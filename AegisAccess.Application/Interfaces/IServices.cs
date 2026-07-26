using AegisAccess.Application.DTOs;
namespace AegisAccess.Application.Interfaces;

public interface IDashboardService { Task<DashboardDto> GetSummaryAsync(); }
public interface IUserService { Task<IEnumerable<UserDto>> GetAllUsersAsync(); }
public interface IDepartmentService { Task<IEnumerable<DepartmentDto>> GetAllDepartmentsAsync(); }
public interface IRoleService { Task<IEnumerable<RoleDto>> GetAllRolesAsync(); }
public interface IPermissionService { Task<IEnumerable<PermissionDto>> GetAllPermissionsAsync(); }
public interface IResourceService { Task<IEnumerable<ResourceDto>> GetAllResourcesAsync(); }
public interface IAuditService { Task<IEnumerable<AuditEventDto>> GetRecentEventsAsync(int count = 50); }

public interface IIdentityProvider { Task<bool> VerifyUserAsync(string userId); }
public interface IPolicyDecisionPoint { Task<bool> EvaluateAccessAsync(string user, string resource); }
public interface IPrivilegedCredentialProvider { Task<string> GetTemporaryCredentialsAsync(string resource); }