using Microsoft.EntityFrameworkCore;
using System.Data;
using Oversight.Model;
using Oversight.Models;

namespace Oversight
{
    public partial class DataContext : DbContext
    {
        public DataContext(DbContextOptions<DataContext> options) : base(options) { }

        public virtual DbSet<Module> Modules { get; set; } = null!;
        public virtual DbSet<ModulePermission> ModulePermissions { get; set; } = null!;
        public virtual DbSet<Permission> Permissions { get; set; } = null!;
        public virtual DbSet<Role> Roles { get; set; } = null!;
        public virtual DbSet<RoleClaim> RoleClaims { get; set; } = null!;
        public virtual DbSet<User> Users { get; set; } = null!;
        public virtual DbSet<UserParent> UserParents { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ModulePermission>(entity =>
            {
                entity.HasIndex(e => e.ModuleId, "IX_ModulePermissions_ModuleId");

                entity.HasOne(d => d.Module)
                    .WithMany(p => p.ModulePermissions)
                    .HasForeignKey(d => d.ModuleId);

                // JSON stored as TEXT
                entity.Property(e => e.PermissionsJson)
                    .HasColumnType("TEXT")
                    .IsRequired();
            });

            modelBuilder.Entity<Role>(entity =>
            {
                entity.HasIndex(e => e.ParentRoleId, "IX_Roles_ParentRoleId");

                entity.HasOne(d => d.ParentRole)
                    .WithMany(p => p.InverseParentRole)
                    .HasForeignKey(d => d.ParentRoleId)
                    .OnDelete(DeleteBehavior.SetNull)
                    .HasConstraintName("FK_Roles_ParentRoleId");
            });

            modelBuilder.Entity<RoleClaim>(entity =>
            {
                entity.HasIndex(e => e.RoleId, "IX_RoleClaims_RoleId");

                entity.HasOne(d => d.Role)
                    .WithMany(p => p.RoleClaims)
                    .HasForeignKey(d => d.RoleId)
                    .HasConstraintName("FK_RoleClaims_Roles");

                // JSON stored as TEXT
                entity.Property(e => e.ModulePermissionsJson)
                    .HasColumnType("TEXT")
                    .IsRequired();
            });

            modelBuilder.Entity<User>(entity =>
            {
                entity.HasIndex(e => e.RoleId, "IX_Users_RoleId");

                entity.HasIndex(e => e.Email, "Users_Email_key")
                    .IsUnique();

                //entity.HasIndex(e => e.MobileNumber, "Users_MobileNumber_key")
                //    .IsUnique();

                entity.Property(e => e.IsActive).HasDefaultValueSql("true");
                entity.Property(e => e.IsVerified).HasDefaultValueSql("false");

                entity.HasOne(d => d.Role)
                    .WithMany(p => p.Users)
                    .HasForeignKey(d => d.RoleId)
                    .HasConstraintName("FK_Users_Roles");
            });

            OnModelCreatingPartial(modelBuilder);
        }

        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
    }
}
