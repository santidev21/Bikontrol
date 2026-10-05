# Landing

Public marketing page at the **root path** (`modules/marketing/landing`). The
`rootRedirectGuard` sends authenticated users straight to `/dashboard` and lets
guests see the landing; CTAs link to `/register`, `/login` (and a demo button
when `environment.demoEnabled` is on).

Sections: hero + value proposition, value props, "cómo funciona", trust, final
CTA and footer (Ayuda / Términos / Privacidad).

**Screenshots are pending.** The plan asks for real captures (see
[docs/screenshots/](../screenshots/)); add them to
`bikontrol-web/public/screenshots/` and render them in the marked spot in the
template. Do not ship invented/mocked screenshots.
