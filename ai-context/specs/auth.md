# Auth

- Authentication uses JWT and password hashing.
- Login and register store the JWT token in `localStorage`.
- `auth.interceptor.ts` adds `Authorization: Bearer <token>` automatically.
- UI errors should be surfaced to the user, not logged with `console.error`.
