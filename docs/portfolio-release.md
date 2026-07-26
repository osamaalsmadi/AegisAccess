# AegisAccess Portfolio Release v1.0

## Implemented Security Capabilities
- Identity inventory and role-based access control.
- Time-limited access request workflow.
- Deterministic risk scoring from resource sensitivity, role privilege, duration, justification, and existing privilege.
- Segregation of Duties policy engine and toxic role-combination detection.
- Critical request escalation and SecurityAdmin exception review.
- Self-approval prevention.
- Access review and recertification campaigns.
- Certify, Revoke, and Escalate review decisions.
- CSV evidence export.
- Dynamic governance posture score.
- Audit events for every security decision.
- API rate limiting, strict CORS, development-only authentication, and Production startup blocking.
- Unit tests, integration tests, migrations, frontend production builds, and dependency vulnerability scans.

## Portfolio Boundary
This is a complete local portfolio release, not a production identity provider.
Production deployment requires replacing demo authentication with OIDC/JWT, centralized secrets, PostgreSQL, TLS, and enterprise operations controls.

## Suggested CV Description
Built AegisAccess, a full-stack Identity Governance and Administration platform using ASP.NET Core, EF Core, SQLite, React, and TypeScript. Implemented risk-aware access requests, RBAC, SoD conflict detection, access certification campaigns, revocation workflows, audit evidence, CSV export, automated tests, and dependency vulnerability scanning.
