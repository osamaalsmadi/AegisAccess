# Access Governance Workflow

## Objective
AegisAccess provides a risk-aware Identity Governance and Administration workflow for time-limited access.

## Security Controls
- Authenticated users can submit requests for classified resources.
- Risk is calculated from sensitivity, requested privilege, duration, justification quality, and existing privileged access.
- Critical requests are escalated and require SecurityAdmin approval.
- Segregation of duties prevents self-approval.
- Every creation and decision writes an audit event.
- Employee identities see only their own requests.
- Approver, Auditor, and SecurityAdmin identities can review the decision queue.

## Demo Personas
- `employee`: creates and views personal requests.
- `approver`: reviews standard requests.
- `auditor`: reviews requests and audit evidence without decision rights.
- `securityadmin`: decides all requests, including Critical escalations.

## Production Boundary
Phase 2 remains a local development lab. Demo header authentication is blocked in Production.
OIDC/JWT integration is planned before any production deployment.
