using System;
using System.Collections.Generic;

namespace Oversight.Model
{
    public partial class Permission
    {
        public int Id { get; set; }
        public string PermissionName { get; set; } = null!;
    }
}
