# Legal pages

Public, unauthenticated pages linked from the login and register footers:

- `/terms` → Terms and Conditions (`modules/legal/terms`).
- `/privacy` → Privacy Policy (`modules/legal/privacy`).

**The copy is a template**, marked with a visible "Borrador" banner and `[COMPLETAR]`
markers. It is scaffolding, not legal advice: the definitive text must be reviewed
and replaced before launch. `lastUpdated` and `contactEmail` are fields on each
component.

The functional privacy controls live in the app:

- **Export my data** — `GET /api/users/me/export` ("Descargar mis datos" on the profile).
- **Delete my account** — `POST /api/users/me/delete` ("Eliminar mi cuenta"; see
  [ADR 003](../adr/003-account-deletion.md)).
