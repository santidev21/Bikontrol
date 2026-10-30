# ADR 005 — Object storage for attachment images and motorcycle photos (private bucket, signed same-origin URLs)

## Context

Maintenance-record attachments and motorcycle photos were stored as base64 data URLs in PostgreSQL
`text` columns (up to ~1 MB decoded each). That inflates the database, its dumps and backups, makes every list payload carry
megabytes, and blocks per-user limits and horizontal scaling.

Options considered:

1. **Public bucket / presigned bucket URLs.** Browsers fetch straight from the bucket. It needs bucket
   CORS and a change to the CSP `img-src` (duplicated five times in `nginx.conf` and per deployment),
   and a presigned URL changes on every request, defeating the browser cache.
2. **Keep everything in PostgreSQL** and only add quotas. No scaling or backup benefit.
3. **Private bucket behind the API.** The API streams the bytes after checking an HMAC-signed,
   expiring URL.

## Decision

Option 3. `IObjectStorage` is a port with an S3 adapter (`AWSSDK.S3`), so Cloudflare R2, Backblaze B2,
MinIO and AWS S3 are all configuration (`Storage__*`). Cloudflare R2 is the recommended default: S3
compatible, no egress fees. `Storage:Enabled=false` keeps the previous behaviour.

- **Signed URLs.** `GET /api/media/attachments/{id}?exp&sig`, anonymous (an `<img>` cannot send a bearer
  token). `sig` is HMAC-SHA256 of `attachments/{id}.{exp}` with a key derived from `Jwt:Key`. The expiry
  is rounded to a 6-hour window plus one, so the URL is identical for a whole window and the browser
  caches it (`Cache-Control: private, max-age=21600`, `ETag` = attachment id). An invalid signature, an
  expired URL and an unknown id all answer 404. The URL carries no user id and grants one image.
- **The DTO keeps its shape.** `MaintenanceAttachmentDTO.DataUrl` is now a signed media path for every
  attachment (also rows still in the database), so list responses stop carrying base64. The frontend
  resolves the path against the API origin.
- **Quota.** `Storage:MaxBytesPerUser` (default 50 MB, 0 = off) is checked on upload against the sum of
  `SizeBytes`; it applies with or without the bucket. Exceeding it is `400 storage_quota_exceeded`.
- **Migration.** `deploy.sh migrate-images` (`Program --migrate-images`; `migrate-attachments` is the old
  name and still works) moves existing attachment images and motorcycle photos in batches, updating a row only after its upload succeeded: idempotent and resumable, and the app
  serves both kinds while it runs.
- **Lifecycle.** Deleting an attachment or an account deletes its objects after the database commit,
  best effort. A failed `SaveChanges` after an upload removes that object. Account export inlines the
  real bytes as data URLs, because signed URLs would expire inside the downloaded file.

## Consequences

- **Backups no longer cover images.** `pg_dump` contains only the object keys. Turn on bucket
  versioning or replication at the provider, or mirror the bucket (`rclone sync`) next to the offsite DB
  copy. A restore of an old dump can reference objects deleted since; those answer 404.
- Orphaned objects are possible (a crash between upload and commit, a failed delete). They cost a few
  KB and are unreachable; a periodic bucket-vs-database sweep is the follow-up if they ever matter.
- Image bytes flow through the API instead of the CDN. At ≤1 MB per image with browser caching this is
  acceptable; if it stops being, the change is confined to `MediaController` (redirect to a short-lived
  presigned URL plus a CSP `img-src` entry).
- **Motorcycle photos use the same port, URLs and quota** (`/api/media/motorcycles/{id}?v&exp&sig`). They
  differ in three ways:
  - One photo per bike that can be replaced, so each upload gets a new `ImageVersion` (and object key).
    `v` is part of the URL only to bust the browser cache, so a changed photo shows at once instead of
    after the 6-hour cache window; it is not signed because it grants nothing. The old object is deleted
    after the new one is saved, and the new one is deleted if saving fails.
  - The edit form reads the bike with `GET` and sends the `image` field back. The server therefore treats
    this bike's own media URL (path or absolute) as "unchanged", an empty value or `default.png` as
    "remove", a data URL as a new photo, and rejects anything else (`image_invalid_format`).
  - Soft-deleting a bike drops its photo (the maintenance history stays), so a deleted bike cannot keep
    eating the user's quota.
- Attachments and photos share one budget (`IStorageQuota`); replacing a photo frees its old size first.
- Rotating `Jwt:Key` invalidates outstanding media URLs; the next API response issues fresh ones.
- Local development and CI run a real S3 server (Adobe S3Mock) so the adapter is exercised end to end.
