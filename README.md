# AegisAccess

AegisAccess is a full-stack Identity Governance and Administration portfolio platform.

## Technology
- ASP.NET Core 10
- Entity Framework Core
- SQLite
- React
- TypeScript
- Vite
- xUnit

## Security Modules
- RBAC and persona-based authorization
- Risk-aware access requests
- SoD toxic-combination detection
- Critical escalation
- Self-approval prevention
- Access review campaigns
- Certification and revocation
- Dynamic posture analytics
- Audit evidence
- CSV exports
- Rate limiting and strict CORS
- Dependency vulnerability scanning

## Run API
`dotnet run --project AegisAccess.Api --no-launch-profile --urls http://localhost:5091`

## Run Web
`cd aegisaccess-web`
`npm.cmd run dev`

Open `http://localhost:5173`.

## Security Boundary
Demo header authentication is local-only. The API intentionally refuses Production startup until OIDC/JWT is implemented.
