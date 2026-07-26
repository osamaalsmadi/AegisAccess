using AegisAccess.Domain.Entities;
using Microsoft.EntityFrameworkCore;
namespace AegisAccess.Infrastructure.Persistence;

public static class DataSeeder {
    public static async Task SeedAsync(AegisDbContext ctx) {
        if (await ctx.Departments.AnyAsync()) return; // Idempotent check

        var fin = new Department { Name = "Finance", Description = "Finance Dept" };
        var it = new Department { Name = "IT", Description = "Information Technology" };
        var hr = new Department { Name = "HR", Description = "Human Resources" };
        ctx.Departments.AddRange(fin, it, hr);

        var secAdminRole = new Role { Name = "SecurityAdmin", Description = "Full Admin" };
        var employeeRole = new Role { Name = "Employee", Description = "Standard User" };
        var approverRole = new Role { Name = "Approver", Description = "Approves Access" };
        var auditorRole = new Role { Name = "Auditor", Description = "Read-only Audit" };
        ctx.Roles.AddRange(secAdminRole, employeeRole, approverRole, auditorRole);

        var readPerm = new Permission { Name = "Read", Description = "Read Access" };
        var writePerm = new Permission { Name = "Write", Description = "Write Access" };
        var manageUsersPerm = new Permission { Name = "ManageUsers", Description = "Manage Users" };
        ctx.Permissions.AddRange(readPerm, writePerm, manageUsersPerm);

        ctx.RolePermissions.Add(new RolePermission { Role = secAdminRole, Permission = manageUsersPerm });
        ctx.RolePermissions.Add(new RolePermission { Role = employeeRole, Permission = readPerm });

        var empUser = new User { Username = "employee", Email = "emp@aegis.local", Department = hr, IsActive = true };
        var adminUser = new User { Username = "securityadmin", Email = "admin@aegis.local", Department = it, IsActive = true };
        var apprUser = new User { Username = "approver", Email = "appr@aegis.local", Department = fin, IsActive = true };
        var audUser = new User { Username = "auditor", Email = "aud@aegis.local", Department = fin, IsActive = true };
        ctx.Users.AddRange(empUser, adminUser, apprUser, audUser);

        ctx.UserRoles.Add(new UserRole { User = empUser, Role = employeeRole });
        ctx.UserRoles.Add(new UserRole { User = adminUser, Role = secAdminRole });
        ctx.UserRoles.Add(new UserRole { User = apprUser, Role = approverRole });
        ctx.UserRoles.Add(new UserRole { User = audUser, Role = auditorRole });

        ctx.Resources.Add(new Resource { Name = "Finance Database", Type = "Database", SensitivityLevel = "High" });
        ctx.Resources.Add(new Resource { Name = "HR Portal", Type = "Web", SensitivityLevel = "Medium" });
        ctx.Resources.Add(new Resource { Name = "IT Administration", Type = "System", SensitivityLevel = "Critical" });

        ctx.AuditEvents.Add(new AuditEvent { Action = "SystemInit", Actor = "System", Target = "AegisAccess", Details = "Initial Seed" });

        await ctx.SaveChangesAsync();
    }
}