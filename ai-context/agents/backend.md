# Backend agent

Playbook for .NET 8 backend work (`Bikontrol/`).

- Keep the Clean Architecture layering: controllers stay thin, logic goes in `Application`/`Infrastructure`, EF Core stays in `Persistence`.
- When you change an API contract, update frontend types and tests together.
- Add or update xUnit tests in `Bikontrol/Bikontrol.Tests` when adding service or controller behavior.
