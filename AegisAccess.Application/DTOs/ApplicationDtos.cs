namespace AegisAccess.Application.DTOs;

public class DashboardDto {
    public int TotalUsers { get; set; }
    public int TotalDepartments { get; set; }
    public int TotalRoles { get; set; }
    public int TotalResources { get; set; }
}
public class UserDto {
    public Guid Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string DepartmentName { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}
public class DepartmentDto {
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
}
public class RoleDto {
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
}
public class PermissionDto {
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
}
public class ResourceDto {
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string SensitivityLevel { get; set; } = string.Empty;
}
public class AuditEventDto {
    public Guid Id { get; set; }
    public string Action { get; set; } = string.Empty;
    public string Actor { get; set; } = string.Empty;
    public string Target { get; set; } = string.Empty;
    public DateTime TimestampUtc { get; set; }
}