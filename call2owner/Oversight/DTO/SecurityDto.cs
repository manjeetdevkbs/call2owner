using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Oversight.DTO
{
    public class ModuleDto
    {
        public int ModuleId { get; set; }
        public string ModuleName { get; set; } = null!;
    }

    public class PermissionDto
    {
        public int Id { get; set; }
        public string PermissionName { get; set; } = null!;
    }

    public class ModulePermissionDto
    {
        public int ModuleId { get; set; }
        public string ModuleName { get; set; } = null!;
        public List<PermissionDataDto> Permissions { get; set; }
    }

    public class PermissionDataDto
    {
        public int PermissionId { get; set; }
        public string? PermissionName { get; set; }
    }

    // Roles
    public class RoleDto
    {
        public int Id { get; set; }
        public string RoleName { get; set; } = null!;
        public string? DisplayName { get; set; }
        public int? ParentRoleId { get; set; }
        public string? ParentRoleName { get; set; }
    }

    public class RoleDetailDto
    {
        public int Id { get; set; }
        public string RoleName { get; set; } = null!;
        public string? DisplayName { get; set; }
        public int? ParentRoleId { get; set; }
        public string? ParentRoleName { get; set; }
        public List<ModulePermissionDto> RoleClaims { get; set; } = new();
    }

    public class RoleDetailOutputDto
    {
        public int Id { get; set; }
        public string RoleName { get; set; } = null!;
        public string? DisplayName { get; set; }
        public int? ParentRoleId { get; set; }
        public string? ParentRoleName { get; set; }
        public string RoleClaims { get; set; }
    }

    public class RoleClaimDto
    {
        public int Id { get; set; }
        public int? RoleId { get; set; }
        public string? RoleName { get; set; }
        public List<ModulePermissionDto> ModulePermissions { get; set; }
    }
}
