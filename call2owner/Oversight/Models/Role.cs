using System;
using System.Collections.Generic;

namespace Oversight.Model
{
    public partial class Role
    {
        public Role()
        {
            InverseParentRole = new HashSet<Role>();
            RoleClaims = new HashSet<RoleClaim>();
            Users = new HashSet<User>();
        }

        public int Id { get; set; }
        public string RoleName { get; set; } = null!;
        public int? ParentRoleId { get; set; }
        public string? DisplayName { get; set; }

        public virtual Role? ParentRole { get; set; }
        public virtual ICollection<Role> InverseParentRole { get; set; }
        public virtual ICollection<RoleClaim> RoleClaims { get; set; }
        public virtual ICollection<User> Users { get; set; }
    }
}
