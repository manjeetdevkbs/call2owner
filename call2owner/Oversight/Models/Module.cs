using System;
using System.Collections.Generic;

namespace Oversight.Model
{
    public partial class Module
    {
        public Module()
        {
            ModulePermissions = new HashSet<ModulePermission>();
        }

        public int ModuleId { get; set; }
        public string ModuleName { get; set; } = null!;

        public virtual ICollection<ModulePermission> ModulePermissions { get; set; }
    }
}
