# Changelog

## [0.8.0](https://github.com/santidev21/Bikontrol/compare/v0.7.0...v0.8.0) (2026-10-30)


### Features

* dedicated worker for daily jobs; migrations off on startup by default ([#118](https://github.com/santidev21/Bikontrol/issues/118)) ([2e92de8](https://github.com/santidev21/Bikontrol/commit/2e92de8689ca5312d8bcd8596bbaaf631a673e70))
* HTML emails with one-click unsubscribe and DNS deliverability check ([#121](https://github.com/santidev21/Bikontrol/issues/121)) ([7d3d44f](https://github.com/santidev21/Bikontrol/commit/7d3d44f26e9180435ad9f7ac5a61d9ae59f10acf))
* JSON logs, job metrics, tracing and a monitoring stack with alerting ([#120](https://github.com/santidev21/Bikontrol/issues/120)) ([b3588ed](https://github.com/santidev21/Bikontrol/commit/b3588edbd5d0500a0f5f7d1ebda5c30c84863ff9))
* store attachment images in a private S3-compatible bucket ([#123](https://github.com/santidev21/Bikontrol/issues/123)) ([0ddc475](https://github.com/santidev21/Bikontrol/commit/0ddc475967cb72f5809189bc8106a5e25432d6f3))
* store motorcycle photos in object storage ([#128](https://github.com/santidev21/Bikontrol/issues/128)) ([8cb0da1](https://github.com/santidev21/Bikontrol/commit/8cb0da1c181b1f4a61a4c042f4924c51cd7653a3))
* **web:** report unhandled browser errors to Sentry ([#129](https://github.com/santidev21/Bikontrol/issues/129)) ([e2c47b1](https://github.com/santidev21/Bikontrol/commit/e2c47b113e74c414e9176bc358e50f7bbd79d546))


### Bug Fixes

* **deploy:** let the backup cron run without root and find per-user rclone ([#130](https://github.com/santidev21/Bikontrol/issues/130)) ([e7379d2](https://github.com/santidev21/Bikontrol/commit/e7379d26d1bb5c230eca67598d6aef05081a7655))
* **deploy:** let the backup cron run without root and find per-user rclone ([#131](https://github.com/santidev21/Bikontrol/issues/131)) ([2819083](https://github.com/santidev21/Bikontrol/commit/2819083d6332d132b2fc96d39ca43443d655eb3c))
* **deploy:** record the real previous tag on the first tagged deploy ([#124](https://github.com/santidev21/Bikontrol/issues/124)) ([60e7efa](https://github.com/santidev21/Bikontrol/commit/60e7efa8fd21332561066f13de2921c311ba0710))
* **deps:** patch vulnerable packages and fix the memory alert metric ([#127](https://github.com/santidev21/Bikontrol/issues/127)) ([e6d1672](https://github.com/santidev21/Bikontrol/commit/e6d1672c68e805e1a4f711d7f2e05900fbffc7a0))
* **docker:** apply Alpine security updates in the web image ([#122](https://github.com/santidev21/Bikontrol/issues/122)) ([615d5be](https://github.com/santidev21/Bikontrol/commit/615d5bee07e46d16edf3e6fa6ae9acf97d5686c6))

## [0.7.0](https://github.com/santidev21/Bikontrol/compare/v0.6.1...v0.7.0) (2026-10-29)


### Features

* phase 1 hardening (auth, limits, resilience, typed options, query shaping) ([#115](https://github.com/santidev21/Bikontrol/issues/115)) ([ec9b335](https://github.com/santidev21/Bikontrol/commit/ec9b335666f734ea8c3fc609059eeca4d6e159f4))

## [0.6.1](https://github.com/santidev21/Bikontrol/compare/v0.6.0...v0.6.1) (2026-10-29)


### Bug Fixes

* shared ownership check, xmin tokens and atomic odometer writes ([#110](https://github.com/santidev21/Bikontrol/issues/110)) ([9bd3be8](https://github.com/santidev21/Bikontrol/commit/9bd3be85d6602e4f9f876e88c6c31014bc0eb30b))

## [0.6.0](https://github.com/santidev21/Bikontrol/compare/v0.5.0...v0.6.0) (2026-10-28)


### Features

* security, performance and architecture hardening pass ([#103](https://github.com/santidev21/Bikontrol/issues/103)) ([bfc64d8](https://github.com/santidev21/Bikontrol/commit/bfc64d8fc9c9501f159ea83fb66b3da36492a9c2))


### Bug Fixes

* **fuel:** guard integrity checks pre-migration and add flow coverage ([#101](https://github.com/santidev21/Bikontrol/issues/101)) ([342ce06](https://github.com/santidev21/Bikontrol/commit/342ce065fdb0f9a086a3e7b66bb207c84d95b8dc))
* reminder delivery, job locking, audit size, refresh interceptor and backup/restore ([#105](https://github.com/santidev21/Bikontrol/issues/105)) ([01da107](https://github.com/santidev21/Bikontrol/commit/01da107e1c905ee762094a7bd9c7cf30506bade1))

## [0.5.0](https://github.com/santidev21/Bikontrol/compare/v0.4.0...v0.5.0) (2026-10-15)


### Features

* **fuel:** fuel-ups, consumption, cost of ownership and maintenance budgets ([#92](https://github.com/santidev21/Bikontrol/issues/92)) ([5c200f0](https://github.com/santidev21/Bikontrol/commit/5c200f03118a562b741acb921b5ec00e70f52209))


### Performance Improvements

* **auth:** generate JWTs with JsonWebTokenHandler ([#97](https://github.com/santidev21/Bikontrol/issues/97)) ([174d93d](https://github.com/santidev21/Bikontrol/commit/174d93d79467e78532cc383cce742bff4f9f979d))
* **logging:** adopt source-generated LoggerMessage in the API layer ([#100](https://github.com/santidev21/Bikontrol/issues/100)) ([6a9a0f5](https://github.com/santidev21/Bikontrol/commit/6a9a0f56d16de3c3bea8ae0b3770e9e2139bb4da))

## [0.4.0](https://github.com/santidev21/Bikontrol/compare/v0.3.1...v0.4.0) (2026-10-14)


### Features

* **web:** loading feedback for every request ([#89](https://github.com/santidev21/Bikontrol/issues/89)) ([1d3c105](https://github.com/santidev21/Bikontrol/commit/1d3c1051deedb0ae162006fac466223de275ed72))

## [0.3.1](https://github.com/santidev21/Bikontrol/compare/v0.3.0...v0.3.1) (2026-10-07)


### Bug Fixes

* **maintenance:** cascade soft-delete and enforce monotonic km ([#86](https://github.com/santidev21/Bikontrol/issues/86)) ([9ae00e3](https://github.com/santidev21/Bikontrol/commit/9ae00e38f7b51c4a2728cfd28fddd103ace26ae1))

## [0.3.0](https://github.com/santidev21/Bikontrol/compare/v0.2.1...v0.3.0) (2026-10-07)


### Features

* **maintenance:** multi-select registration and quick register from summary ([#81](https://github.com/santidev21/Bikontrol/issues/81)) ([a1e8db7](https://github.com/santidev21/Bikontrol/commit/a1e8db72a378088a5a73c86fab8f85f27b9fec1f))


### Bug Fixes

* **web:** use Bikontrol icons and keep navigation clear of safe areas ([#85](https://github.com/santidev21/Bikontrol/issues/85)) ([3b58f6d](https://github.com/santidev21/Bikontrol/commit/3b58f6ddf1fa81a5790e7451e42dc5d7892679fa))

## [0.2.1](https://github.com/santidev21/Bikontrol/compare/v0.2.0...v0.2.1) (2026-10-07)


### Bug Fixes

* **home:** hide empty state while motorcycles are loading ([#79](https://github.com/santidev21/Bikontrol/issues/79)) ([98f15bb](https://github.com/santidev21/Bikontrol/commit/98f15bb1d7a617efba8f50c2ba836ed043e7008e))

## [0.2.0](https://github.com/santidev21/Bikontrol/compare/v0.1.0...v0.2.0) (2026-10-06)


### Features

* **help:** public Help/FAQ page for customers (5.4) ([#70](https://github.com/santidev21/Bikontrol/issues/70)) ([77d70f2](https://github.com/santidev21/Bikontrol/commit/77d70f258c267f34d073a788ae4d9f8c4deb9bff))
* **i18n:** runtime ES/EN i18n and translate the landing (3.6) ([#74](https://github.com/santidev21/Bikontrol/issues/74)) ([9a00084](https://github.com/santidev21/Bikontrol/commit/9a00084079cbc888083d5630bb757a7d913b5345))
* **i18n:** translate auth pages + app shell (3.6) ([#75](https://github.com/santidev21/Bikontrol/issues/75)) ([7ffe8b7](https://github.com/santidev21/Bikontrol/commit/7ffe8b768b15b55f2b27362b5ae9c954bb52eb60))
* **i18n:** translate maintenance, statistics and profile (3.6) ([#78](https://github.com/santidev21/Bikontrol/issues/78)) ([86e4961](https://github.com/santidev21/Bikontrol/commit/86e496130f835274aef2fb79576419dd2e6facc1))
* **i18n:** translate the motorcycle flow (3.6) ([#76](https://github.com/santidev21/Bikontrol/issues/76)) ([d364bea](https://github.com/santidev21/Bikontrol/commit/d364bead44fd65b99b2b1bff87238ccad18ebfda))
* **landing:** public marketing page at the root (5.1) ([#73](https://github.com/santidev21/Bikontrol/issues/73)) ([9e3f21a](https://github.com/santidev21/Bikontrol/commit/9e3f21a8657bad74119bf76f9132551483696dda))
* **legal:** public Terms and Privacy pages (5.2) ([#69](https://github.com/santidev21/Bikontrol/issues/69)) ([0426901](https://github.com/santidev21/Bikontrol/commit/042690198ebe6b3e2fcfd330ec6a009ba1560a35))

## Changelog

Notable changes to Bikontrol. Maintained by
[release-please](https://github.com/googleapis/release-please) from
[Conventional Commits](https://www.conventionalcommits.org/): it opens a release
PR that updates this file and `version.txt` (mirrored into
`bikontrol-web/package.json`), and merging it tags the release.
