using AegisAccess.Application.DTOs;
using AegisAccess.Application.Interfaces;
using AegisAccess.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AegisAccess.Infrastructure.Services;

public class DashboardService : IDashboardService {
    private readonly AegisDbContext _db;
    public DashboardService(AegisDbContext db) { _db = db; }
    public async Task<DashboardDto> GetSummaryAsync() {
        return new DashboardDto {
            TotalUsers = await _db.Users.CountAsync(),
            TotalDepartments = await _db.Departments.CountAsync(),
            TotalRoles = await _db.Roles.CountAsync(),
            TotalResources = await _db.Resources.CountAsync()
        };
    }
}
public class UserService : IUserService {
    private readonly AegisDbContext _db;
    public UserService(AegisDbContext db) { _db = db; }
    public async Task<IEnumerable<UserDto>> GetAllUsersAsync() {
        return await _db.Users.Include(u => u.Department).Select(u => new UserDto {
            Id = u.Id, Username = u.Username, Email = u.Email, IsActive = u.IsActive, DepartmentName = u.Department!.Name
        }).ToListAsync();
    }
}
public class DepartmentService : IDepartmentService {
    private readonly AegisDbContext _db;
    public DepartmentService(AegisDbContext db) { _db = db; }
    public async Task<IEnumerable<DepartmentDto>> GetAllDepartmentsAsync() {
        return await _db.Departments.Select(d => new DepartmentDto { Id = d.Id, Name = d.Name }).ToListAsync();
    }
}
public class RoleService : IRoleService {
    private readonly AegisDbContext _db;
    public RoleService(AegisDbContext db) { _db = db; }
    public async Task<IEnumerable<RoleDto>> GetAllRolesAsync() {
        return await _db.Roles.Select(r => new RoleDto { Id = r.Id, Name = r.Name }).ToListAsync();
    }
}
public class PermissionService : IPermissionService {
    private readonly AegisDbContext _db;
    public PermissionService(AegisDbContext db) { _db = db; }
    public async Task<IEnumerable<PermissionDto>> GetAllPermissionsAsync() {
        return await _db.Permissions.Select(p => new PermissionDto { Id = p.Id, Name = p.Name }).ToListAsync();
    }
}
public class ResourceService : IResourceService {
    private readonly AegisDbContext _db;
    public ResourceService(AegisDbContext db) { _db = db; }
    public async Task<IEnumerable<ResourceDto>> GetAllResourcesAsync() {
        return await _db.Resources.Select(r => new ResourceDto { Id = r.Id, Name = r.Name, SensitivityLevel = r.SensitivityLevel }).ToListAsync();
    }
}
public class AuditService : IAuditService {
    private readonly AegisDbContext _db;
    public AuditService(AegisDbContext db) { _db = db; }
    public async Task<IEnumerable<AuditEventDto>> GetRecentEventsAsync(int count = 50) {
        return await _db.AuditEvents.OrderByDescending(a => a.TimestampUtc).Take(count).Select(a => new AuditEventDto {
            Id = a.Id, Action = a.Action, Actor = a.Actor, Target = a.Target, TimestampUtc = a.TimestampUtc
        }).ToListAsync();
    }
}