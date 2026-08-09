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


## Screenshots

### Dashboard
![AegisAccess dashboard](screenshots/dashboard-overview.jpeg)

### Risk & Governance Activity
![Risk distribution and governance activity](screenshots/dashboard-risk-activity.jpeg)

### Adaptive Access Requests
![Create an adaptive access request](screenshots/access-request-form.jpeg)

### Access Request Evidence
![Risk-aware access request decision queue](screenshots/access-request-evidence.jpeg)

### Access Review Campaigns
![Privileged access certification campaign](screenshots/review-campaign-decision.jpeg)

### Policy Center
![Segregation of duties policy center](screenshots/policy-center.jpeg)
