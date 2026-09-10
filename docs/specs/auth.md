# Auth

- Authentication uses JWT and password hashing.
- Login and register store the JWT token in `localStorage`.
- `auth.interceptor.ts` adds `Authorization: Bearer <token>` automatically.
- UI errors should be surfaced to the user, not logged with `console.error`.
- Per-clone signing secret: generate with `openssl rand -base64 48` and put it in `Jwt:Key` inside `Bikontrol/Bikontrol.API/appsettings.Development.json` (never committed); production uses `Jwt__Key` env.
- Password hashing with a per-user salt.
