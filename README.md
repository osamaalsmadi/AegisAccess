# AegisAccess

AegisAccess is a lightweight Identity Governance and Administration security platform built with ASP.NET Core, Entity Framework Core, SQLite, React, and TypeScript.

## Implemented
- Modular monolith architecture
- Role-based authorization
- Development-only demo identities
- Classified resources and governance roles
- Time-limited access requests
- Deterministic risk scoring
- Critical-risk escalation
- Approval and rejection workflow
- Segregation of duties
- Audit evidence
- Automated unit and integration tests
- Dependency vulnerability scanning

## Local Run

API:
`dotnet run --project AegisAccess.Api --no-launch-profile --urls http://localhost:5091`

Web:
`cd aegisaccess-web`
`npm.cmd run dev`

Open `http://localhost:5173`.

## Security Notice
Demo header authentication is strictly for local development. Production startup is intentionally blocked until OIDC/JWT authentication is implemented.
