using AegisAccess.Application.DTOs;
using AegisAccess.Application.Interfaces;
// Interfaces depend on DbContext, but we inject IGenericRepository or inject DbContext directly for speed in this phase.
// To avoid cyclic reference cleanly, we'll implement these directly in Infrastructure, or use a context interface.
// For Lightweight phase, we will place implementations in Infrastructure layer to access AegisDbContext directly.