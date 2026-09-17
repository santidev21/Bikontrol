---
description: Review Bikontrol changes — diff review or full-repo quality audit, with security and tests.
---

Review scope: `$ARGUMENTS` (default: current uncommitted changes).

Workflow:
1. Determine the scope with read-only git: `git status`, `git diff`, `git diff --staged`. If `$ARGUMENTS` mentions `repo` / `all` / `full`, the scope is the **entire repository**.
2. Pick the reviewer:
   - Whole repo, or a broad quality/security question → delegate to the **repo-auditor** agent (whole-repo, graded report).
   - A diff/PR/finished change → delegate to the **reviewer** agent.
3. Load the skills that match the touched areas before judging: `api-contract` (DTOs/endpoints), `angular-best-practices` (frontend), `dotnet-best-practices` / `aspnet-core` / `csharp-async` (backend), `security-review` (always for auth, input, secrets, data access), `db-migrations` (entities/schema).
4. Confirm with tests: `npm run test` (or `npm run test:api` / `npm run test:ui`). Report the actual result — do not claim green without running it.
5. Report, in this order: blocking issues (`path:line`), then suggestions, then test results. Never edit code unless explicitly asked.
