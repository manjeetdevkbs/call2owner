using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace Oversight.Model
{
    public partial class RoleClaim
    {
        public int Id { get; set; }
        public int RoleId { get; set; }

        // Stored in DB as TEXT
        public string ModulePermissionsJson { get; set; } = "[]";

        [NotMapped]
        public List<ModulePermissionData> ModulePermissions
        {
            get => JsonSerializer.Deserialize<List<ModulePermissionData>>(ModulePermissionsJson) ?? new();
            set => ModulePermissionsJson = JsonSerializer.Serialize(value);
        }

        public virtual Role Role { get; set; } = null!;
    }

    public class ModulePermissionData
    {
        public int ModuleId { get; set; }
        public List<PermissionData> Permissions { get; set; } = new();
    }
}
