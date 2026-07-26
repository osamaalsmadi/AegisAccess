# Security Decisions
- No secrets hardcoded.
- CORS strictly bound to `http://localhost:5173`.
- DemoAuth enforces strict environment checks (fails if IsProduction).