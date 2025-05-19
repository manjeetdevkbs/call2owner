using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace Oversight.Model
{
    public partial class ModulePermission
    {
        public int Id { get; set; }
        public int ModuleId { get; set; }

        public string PermissionsJson { get; set; } = "{}";

        [NotMapped]
        public List<PermissionData> Permissions
        {
            get
            {
                var wrapper = JsonSerializer.Deserialize<PermissionWrapper>(PermissionsJson);
                return wrapper?.Permissions ?? new List<PermissionData>();
            }
            set
            {
                PermissionsJson = JsonSerializer.Serialize(new PermissionWrapper { Permissions = value });
            }
        }

        public virtual Module Module { get; set; } = null!;
    }

    public class PermissionWrapper
    {
        public List<PermissionData> Permissions { get; set; } = new();
    }

    public class PermissionData
    {
        public int PermissionId { get; set; }
    }
}
