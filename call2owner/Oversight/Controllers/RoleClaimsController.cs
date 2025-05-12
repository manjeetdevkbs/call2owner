using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Oversight.DTO;
using Oversight.Model;
using System;
using System.Text.Json;

namespace Oversight.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RoleClaimsController : ControllerBase
    {
        private readonly DataContext _context;
        private readonly IMapper _mapper;

        public RoleClaimsController(DataContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        // Get all RoleClaims
        [Authorize]
        [HttpGet]
        //public async Task<ActionResult<IEnumerable<RoleClaim>>> GetRoleClaims()
        //{
        //    var roleClaims = await _context.RoleClaims
        //        .Include(rc => rc.ModulePermission)
        //        .Include(rc => rc.Role)
        //        .ToListAsync();

        //    return Ok(roleClaims);
        //}



        [HttpGet("GetRolesModulesPermissions")]
        public async Task<ActionResult<IEnumerable<object>>> GetRolesModulesPermissions()
        {
            var roles = await _context.Roles
                .Select(rc => new
                {
                    rc.Id,
                    rc.RoleName
                })
                .Distinct()
                .ToListAsync();

            var modules = await _context.Modules
                .Include(x => x.ModulePermissions)
                .Select(x => new
                {
                    ModuleId = x.ModuleId,
                    ModuleName = x.ModuleName,
                    Permissions = x.ModulePermissions.ToList()
                })
                .Distinct()
                .ToListAsync();

            return Ok(new { roles  = roles , module = modules });
        }

        [HttpGet("GetRoleClaims")]
        public async Task<ActionResult<IEnumerable<object>>> GetRoleClaims()
        {
            var roles = await _context.RoleClaims
                .Include(rc => rc.Role)
                .Select(rc => new
                {
                    rc.Role.Id,
                    rc.Role.RoleName
                })
                .Distinct()
                .ToListAsync();

            return Ok(roles);
        }



        //// Get RoleClaim by ID
        //[HttpGet("{id}")]
        //public async Task<ActionResult<RoleClaim>> GetRoleClaim(int id)
        //{
        //    var roleClaim = await _context.RoleClaims
        //        .Include(rc => rc.ModulePermission)
        //        .Include(rc => rc.Role)
        //        .FirstOrDefaultAsync(rc => rc.Id == id);

        //    if (roleClaim == null)
        //        return NotFound("RoleClaim not found.");

        //    return Ok(roleClaim);
        //}

        //[Authorize]
        //// Insert new RoleClaim
        //[HttpPost]       
        //public async Task<ActionResult> CreateOrUpdateRoleClaim(RoleClaimInputDto roleClaimDto)
        //{
        //    var role = await _context.Roles.FirstOrDefaultAsync(r => r.RoleName == roleClaimDto.RoleName);

        //    if (role == null)
        //    {
        //        role = new Role
        //        {
        //            RoleName = roleClaimDto.RoleName,
        //        };

        //        _context.Roles.Add(role);
        //        await _context.SaveChangesAsync();
        //    }

        //    var existingClaims = await _context.RoleClaims
        //        .Where(rc => rc.RoleId == role.Id)
        //        .Select(rc => rc.ModulePermissionId)
        //        .ToListAsync();

        //    var newClaims = roleClaimDto.ModulePermission
        //        .Where(moduleId => !existingClaims.Contains(moduleId))
        //        .Select(moduleId => new RoleClaim
        //        {
        //            RoleId = role.Id,
        //            ModulePermissionId = moduleId
        //        })
        //        .ToList();

        //    if (newClaims.Any())
        //    {
        //        await _context.RoleClaims.AddRangeAsync(newClaims);
        //        await _context.SaveChangesAsync();
        //    }

        //    return Ok(new { roleId = role.Id, roleClaims = newClaims });
        //}

        [HttpPost("create-or-update-role-claim")]
        //    public async Task<ActionResult> CreateOrUpdateRoleClaim(RoleClaimNewDto roleClaimDto)
        //    {
        //        var role = await _context.Roles.FirstOrDefaultAsync(r => r.RoleName == roleClaimDto.RoleName);

        //        if (role == null)
        //        {
        //            role = new Role
        //            {
        //                RoleName = roleClaimDto.RoleName
        //            };

        //            _context.Roles.Add(role);
        //            await _context.SaveChangesAsync();
        //        }

        //        // Fetch existing role claims for this role
        //        var existingClaims = await _context.RoleClaims
        //            .Where(rc => rc.RoleId == role.Id)
        //            .Select(rc => rc.ModulePermissionId)
        //            .ToListAsync();

        //        var newClaims = new List<RoleClaim>();

        //        // ✅ Iterate correctly over ModulePermissions
        //        foreach (var modulePermission in roleClaimDto.ModulePermissions)
        //        {
        //            foreach (var permissionId in modulePermission.Permissions)
        //            {
        //                // ✅ Ensure module-permission exists before inserting
        //                //var modulePermissionEntity = await _context.ModulePermissions
        //                //    .FirstOrDefaultAsync(mp => mp.ModuleId == modulePermission.ModuleId && mp.PermissionId.FirstOrDefault() == permissionId);

        //                var modulePermissionEntity = (await _context.ModulePermissions
        //.Where(mp => mp.ModuleId == modulePermission.ModuleId)
        //.ToListAsync()) // Force client-side evaluation
        //.FirstOrDefault(mp => mp.PermissionId.Contains(permissionId));


        //                if (modulePermissionEntity != null && !existingClaims.Contains(modulePermissionEntity.Id))
        //                {
        //                    newClaims.Add(new RoleClaim
        //                    {
        //                        RoleId = role.Id,
        //                        ModulePermissionId = modulePermissionEntity.Id
        //                    });
        //                }
        //            }
        //        }

        //        if (newClaims.Any())
        //        {
        //            await _context.RoleClaims.AddRangeAsync(newClaims);
        //            await _context.SaveChangesAsync();
        //        }

        //        return Ok(new { roleId = role.Id, roleClaims = newClaims });
        //    }








        //public async Task<ActionResult> CreateOrUpdateRoleClaim(RoleClaimNewDto roleClaimDto)
        //{
        //    var role = await _context.Roles.FirstOrDefaultAsync(r => r.RoleName == roleClaimDto.RoleName);

        //    if (role == null)
        //    {
        //        role = new Role
        //        {
        //            RoleName = roleClaimDto.RoleName
        //        };

        //        _context.Roles.Add(role);
        //        await _context.SaveChangesAsync();
        //    }

        //    // Fetch existing role claims for this role
        //    var existingClaims = await _context.RoleClaims
        //        .Where(rc => rc.RoleId == role.Id)
        //        .Select(rc => rc.ModulePermissionIdsJson)
        //        .ToListAsync();

        //    var newClaims = new List<RoleClaim>();

        //    // Iterate correctly over ModulePermissions
        //    foreach (var modulePermission in roleClaimDto.ModulePermissions)
        //    {
        //        foreach (var permissionId in modulePermission.Permissions)
        //        {
        //            // Fetch modulePermission entity based on ModuleId and PermissionId
        //            var modulePermissionEntity = (await _context.ModulePermissions
        //                .Where(mp => mp.ModuleId == modulePermission.ModuleId)
        //                .ToListAsync()) // Force client-side evaluation
        //                .FirstOrDefault(mp => mp.PermissionId.Contains(permissionId));

        //            if (modulePermissionEntity != null && !existingClaims.Contains(modulePermissionEntity.Id))
        //            {
        //                newClaims.Add(new RoleClaim
        //                {
        //                    RoleId = role.Id,
        //                    ModulePermissionId = modulePermissionEntity.Id
        //                });
        //            }
        //        }
        //    }

        //    // Additional entry where ModuleId and ModulePermissionIdJson are saved
        //    foreach (var modulePermission in roleClaimDto.ModulePermissions)
        //    {
        //        var modulePermissionJson = await _context.ModulePermissions
        //            .Where(mp => mp.ModuleId == modulePermission.ModuleId)
        //            .Select(mp => mp.PermissionIdJson)
        //            .FirstOrDefaultAsync();

        //        if (!string.IsNullOrEmpty(modulePermissionJson))
        //        {
        //            var modulePermissionEntity = await _context.ModulePermissions
        //                .FirstOrDefaultAsync(mp => mp.ModuleId == modulePermission.ModuleId);

        //            if (modulePermissionEntity != null)
        //            {
        //                newClaims.Add(new RoleClaim
        //                {
        //                    RoleId = role.Id,
        //                    ModulePermissionId = modulePermissionEntity.Id
        //                });
        //            }
        //        }
        //    }

        //    if (newClaims.Any())
        //    {
        //        await _context.RoleClaims.AddRangeAsync(newClaims);
        //        await _context.SaveChangesAsync();
        //    }

        //    return Ok(new { roleId = role.Id, roleClaims = newClaims });
        //}







        //public async Task<ActionResult> CreateOrUpdateRoleClaim(RoleClaimNewDto roleClaimDto)
        //{
        //    var role = await _context.Roles.FirstOrDefaultAsync(r => r.RoleName == roleClaimDto.RoleName);

        //    if (role == null)
        //    {
        //        role = new Role { RoleName = roleClaimDto.RoleName };
        //        _context.Roles.Add(role);
        //        await _context.SaveChangesAsync();
        //    }

        //    // Fetch existing role claims for this role
        //    var existingClaimsJson = await _context.RoleClaims
        //        .Where(rc => rc.RoleId == role.Id)
        //        .Select(rc => rc.ModulePermissionsJson)
        //        .FirstOrDefaultAsync();

        //    var existingClaims = string.IsNullOrEmpty(existingClaimsJson)
        //        ? new HashSet<int>()
        //        : JsonSerializer.Deserialize<HashSet<int>>(existingClaimsJson) ?? new HashSet<int>();

        //    var newPermissions = new HashSet<int>(existingClaims);

        //    // Collect all ModulePermissionIds for the role
        //    foreach (var modulePermission in roleClaimDto.ModulePermissions)
        //    {
        //        var permissionIds = await _context.ModulePermissions
        //            .Where(mp => mp.ModuleId == modulePermission.ModuleId)
        //            .Select(mp => new { mp.Id, mp.Permissions.PermissionId })
        //            .ToListAsync();

        //        foreach (var permissionId in modulePermission.Permissions)
        //        {
        //            var matchingEntity = permissionIds.FirstOrDefault(mp => mp.PermissionId.Contains(permissionId));
        //            if (matchingEntity != null)
        //            {
        //                newPermissions.Add(matchingEntity.Id);
        //            }
        //        }
        //    }

        //    //If there are new permissions, update the RoleClaim entry
        //    if (!existingClaims.SetEquals(newPermissions))
        //    {
        //        var permissionsJson = JsonSerializer.Serialize(newPermissions);

        //        var roleClaim = await _context.RoleClaims.FirstOrDefaultAsync(rc => rc.RoleId == role.Id);

        //        if (roleClaim == null)
        //        {
        //            roleClaim = new RoleClaim
        //            {
        //                RoleId = role.Id,
        //                ModulePermissionsJson = permissionsJson
        //            };
        //            _context.RoleClaims.Add(roleClaim);
        //        }
        //        else
        //        {
        //            roleClaim.ModulePermissionIdsJson = permissionsJson;
        //            _context.RoleClaims.Update(roleClaim);
        //        }

        //        await _context.SaveChangesAsync();
        //    }

        //    return Ok(new { roleId = role.Id, modulePermissions = newPermissions });
        //}







        // Update RoleClaims
        //[HttpPut("{roleId}")]
        //public async Task<IActionResult> UpdateRoleClaim(int roleId, RoleClaimEditDto roleClaimDto)
        //{
        //    var role = await _context.Roles.FirstOrDefaultAsync(r => r.Id == roleId);
        //    if (role == null)
        //    {
        //        return BadRequest("Invalid Role");
        //    }

        //    // Update role name if provided
        //    if (!string.IsNullOrWhiteSpace(roleClaimDto.RoleName))
        //    {
        //        role.RoleName = roleClaimDto.RoleName;
        //    }

        //    var existingClaims = _context.RoleClaims.Where(rc => rc.RoleId == roleId);
        //    _context.RoleClaims.RemoveRange(existingClaims);

        //    var newRoleClaims = roleClaimDto.ModulePermission.Select(moduleId => new RoleClaim
        //    {
        //        RoleId = roleId,
        //        ModulePermissionId = moduleId
        //    }).ToList();

        //    await _context.RoleClaims.AddRangeAsync(newRoleClaims);
        //    await _context.SaveChangesAsync();

        //    return Ok(new { roleClaimDto.RoleName, roleClaimDto.ModulePermission });
        //}

        //[HttpPost("{roleId}")]
        //public async Task<IActionResult> UpdateRoleClaim(int roleId, RoleClaimEditDto roleClaimDto)
        //{
        //    var role = await _context.Roles.FirstOrDefaultAsync(r => r.Id == roleId);
        //    if (role == null)
        //    {
        //        return BadRequest("Invalid Role");
        //    }

        //    // Check if the new role name already exists
        //    if (!string.IsNullOrWhiteSpace(roleClaimDto.RoleName) &&
        //        await _context.Roles.AnyAsync(r => r.RoleName == roleClaimDto.RoleName && r.Id != roleId))
        //    {
        //        return BadRequest("Role name already exists.");
        //    }

        //    // Update role name if provided
        //    if (!string.IsNullOrWhiteSpace(roleClaimDto.RoleName))
        //    {
        //        role.RoleName = roleClaimDto.RoleName;
        //    }

        //    var existingClaims = _context.RoleClaims.Where(rc => rc.RoleId == roleId);
        //    _context.RoleClaims.RemoveRange(existingClaims);

        //    var newRoleClaims = roleClaimDto.ModulePermission.Select(moduleId => new RoleClaim
        //    {
        //        RoleId = roleId,
        //        ModulePermissionId = moduleId
        //    }).ToList();

        //    await _context.RoleClaims.AddRangeAsync(newRoleClaims);
        //    await _context.SaveChangesAsync();

        //    return Ok(new { roleClaimDto.RoleName, roleClaimDto.ModulePermission });
        //}




        [HttpGet("{roleId}")]
        public async Task<ActionResult<IEnumerable<RoleClaim>>> GetRoleClaimsByRoleId(int roleId)
        {
            var roleClaims = await _context.RoleClaims.Where(rc => rc.RoleId == roleId).ToListAsync();
            if (!roleClaims.Any())
            {
                return NotFound("No claims found for this role");
            }
            return Ok(roleClaims);
        }



        //[Authorize]
        //[HttpGet("roles-permissions")]
        //public async Task<IActionResult> GetRolesAndPermissions()
        //{
        //    // Fetch roles
        //    var roles = await _context.Roles
        //        .Select(r => new
        //        {
        //            RoleName = r.RoleName,
        //            DisplayName = r.DisplayName
        //        })
        //        .ToListAsync();

        //    // Fetch module permissions
        //    var modulePermissions = await _context.ModulePermissions
        //        .Include(mp => mp.Module)
        //        .Include(mp => mp.Permission)
        //        .ToListAsync();

        //    // Group permissions by module
        //    var groupedPermissions = modulePermissions
        //        .GroupBy(mp => mp.Module.ModuleName)
        //        .Select(g => new
        //        {
        //            Module = g.Key,
        //            Permissions = g.Select(mp => new
        //            {
        //                PermissionId = mp.PermissionId,
        //                PermissionName = mp.Permission.PermissionName,
        //                PermissionValue = "false"
        //            }).ToList()
        //        })
        //        .ToList();

        //    return Ok(new
        //    {
        //        Roles = roles,
        //        ModulePermissions = groupedPermissions
        //    });
        //}

        //[Authorize]
        //[HttpGet("roles-permissions")]
        //public async Task<IActionResult> GetRolesAndPermissions()
        //{
        //    // Fetch roles
        //    var roles = await _context.Roles
        //        .Select(r => new
        //        {
        //            RoleName = r.RoleName,
        //            DisplayName = r.DisplayName
        //        })
        //        .ToListAsync();

        //    // Fetch module permissions with necessary joins
        //    var modulePermissions = await _context.ModulePermissions
        //        .Include(mp => mp.Module)
        //        .Include(mp => mp.Permission)
        //        .Select(mp => new
        //        {
        //            ModuleName = mp.Module.ModuleName,
        //            ModulePermissionId = mp.Id, // Ensure this is the primary key of ModulePermissions table
        //            PermissionId = mp.PermissionId,
        //            PermissionName = mp.Permission.PermissionName
        //        })
        //        .ToListAsync();

        //    // Group permissions by module
        //    var groupedPermissions = modulePermissions
        //        .GroupBy(mp => mp.ModuleName)
        //        .Select(g => new
        //        {
        //            Module = g.Key,
        //            Permissions = g.Select(mp => new
        //            {
        //                ModulePermissionId = mp.ModulePermissionId, // Now correctly included
        //                PermissionId = mp.PermissionId,
        //                PermissionName = mp.PermissionName,
        //                PermissionValue = "false"
        //            }).ToList()
        //        })
        //        .ToList();

        //    return Ok(new
        //    {
        //        Roles = roles,
        //        ModulePermissions = groupedPermissions
        //    });
        //}

        //[Authorize]
        //[HttpGet("getRolePermissionByRoleId/{roleId}")]
        //public async Task<IActionResult> GetRolePermissionsById(int roleId)
        //{
        //    // Fetch the role by ID
        //    var role = await _context.Roles
        //        .Where(r => r.Id == roleId)
        //        .Select(r => new
        //        {
        //            RoleId = r.Id,
        //            RoleName = r.RoleName,
        //            DisplayName = r.DisplayName
        //        })
        //        .FirstOrDefaultAsync();

        //    if (role == null)
        //    {
        //        return NotFound(new { Message = "Role not found." });
        //    }

        //    // Fetch permissions assigned to the role
        //    var rolePermissions = await _context.RoleClaims
        //        .Where(rp => rp.RoleId == roleId)
        //        .Include(rp => rp.ModulePermission)
        //        .ThenInclude(mp => mp.Module)
        //        .Include(rp => rp.ModulePermission)
        //        .ThenInclude(mp => mp.Permission)
        //        .Select(rp => new
        //        {
        //            ModuleName = rp.ModulePermission.Module.ModuleName,
        //            ModulePermissionId = rp.ModulePermission.Id,
        //            PermissionId = rp.ModulePermission.PermissionId,
        //            PermissionName = rp.ModulePermission.Permission.PermissionName
        //        })
        //        .ToListAsync();

        //    // Group permissions by module
        //    var groupedPermissions = rolePermissions
        //        .GroupBy(rp => rp.ModuleName)
        //        .Select(g => new
        //        {
        //            Module = g.Key,
        //            Permissions = g.Select(p => new
        //            {
        //                ModulePermissionId = p.ModulePermissionId,
        //                PermissionId = p.PermissionId,
        //                PermissionName = p.PermissionName,
        //                PermissionValue = "false" // Default value, can be updated as needed
        //            }).ToList()
        //        })
        //        .ToList();

        //    return Ok(new
        //    {
        //        Role = role,
        //        ModulePermissions = groupedPermissions
        //    });
        //}



    }
}
