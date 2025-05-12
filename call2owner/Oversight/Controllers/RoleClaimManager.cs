using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Oversight.DTO;
using Oversight.Model;

namespace Oversight.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RoleClaimManager : ControllerBase
    {
        private readonly IMapper _mapper;
        private readonly DataContext _context;
        private readonly IConfiguration _configuration;

        public RoleClaimManager(DataContext context, IConfiguration configuration,
            IMapper mapper)
        {
            _mapper = mapper;
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        [Authorize(Policy = Utilities.Module.Roleclaims)]
        [Authorize(Policy = Utilities.Permission.GetAll)]
        [HttpGet("modules")]
        public IActionResult GetModules()
        {
            var modules = _context.Modules.ToList();
            var moduleDtos = _mapper.Map<List<ModuleDto>>(modules);
            return Ok(moduleDtos);
        }

        [Authorize(Policy = Utilities.Module.Roleclaims)]
        [Authorize(Policy = Utilities.Permission.GetAll)]
        [HttpGet("permissions")]
        public IActionResult GetPermissions()
        {
            var permissions = _context.Permissions.ToList();
            var permissionDtos = _mapper.Map<List<PermissionDto>>(permissions);
            return Ok(permissionDtos);
        }

        [Authorize(Policy = Utilities.Module.Roleclaims)]
        [Authorize(Policy = Utilities.Permission.GetOwn)]
        [HttpGet("modules/{moduleId}/permissions")]
        public IActionResult GetModulePermission(int moduleId)
        {
            var modules = _context.Modules.ToList();
            var permissions = _context.Permissions.ToList();

            var modulePermission = _context.ModulePermissions
                .Where(mp => mp.ModuleId == moduleId)
                .ToList();

            if (!modulePermission.Any())
            {
                return NotFound("Module permissions not found.");
            }

            var modulePermissionDtos = _mapper.Map<List<ModulePermissionDto>>(modulePermission);

            // Process each modulePermissionDto
            foreach (var dto in modulePermissionDtos)
            {
                // Set module name
                var module = modules.FirstOrDefault(m => m.ModuleId == dto.ModuleId);
                if (module != null)
                {
                    dto.ModuleName = module.ModuleName;
                }

                // Set permission names for each permission in the module
                foreach (var permissionDto in dto.Permissions)
                {
                    var permission = permissions.FirstOrDefault(p => p.Id == permissionDto.PermissionId);
                    if (permission != null)
                    {
                        permissionDto.PermissionName = permission.PermissionName;
                    }
                }
            }

            return Ok(modulePermissionDtos);
        }


        [Authorize(Policy = Utilities.Module.Roleclaims)]
        [Authorize(Policy = Utilities.Permission.GetOwn)]
        [HttpGet("modules/permissions")]
        public IActionResult GetModulePermissions()
        {
            var modules = _context.Modules.ToList();
            var permissions = _context.Permissions.ToList();

            var modulePermission = _context.ModulePermissions
                .ToList();

            if (!modulePermission.Any())
            {
                return NotFound("Module permissions not found.");
            }

            var modulePermissionDtos = _mapper.Map<List<ModulePermissionDto>>(modulePermission);

            foreach (var dto in modulePermissionDtos)
            {
                // Assign module name
                var module = modules.FirstOrDefault(m => m.ModuleId == dto.ModuleId);
                if (module != null)
                {
                    dto.ModuleName = module.ModuleName;
                }

                // Assign permission names
                foreach (var perm in dto.Permissions)
                {
                    var permission = permissions.FirstOrDefault(p => p.Id == perm.PermissionId);
                    if (permission != null)
                    {
                        perm.PermissionName = permission.PermissionName;
                    }
                }
            }

            return Ok(modulePermissionDtos);
        }

        // Roles
        [Authorize(Policy = Utilities.Module.Roleclaims)]
        [Authorize(Policy = Utilities.Permission.GetAll)]
        [HttpGet("roles")]
        public async Task<ActionResult<IEnumerable<RoleDto>>> GetRoles()
        {
            var roles = await _context.Roles.ToListAsync();
            return Ok(_mapper.Map<List<RoleDto>>(roles));
        }

        // 2. Get role by ID with role claims
        [Authorize(Policy = Utilities.Module.Roleclaims)]
        [Authorize(Policy = Utilities.Permission.GetSingle)]
        [HttpGet("role/{id}")]
        public async Task<ActionResult<RoleDetailDto>> GetRoleById(int id)
        {
            var role = await _context.Roles
                .Include(r => r.RoleClaims)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (role == null)
            {
                return NotFound();
            }

            return Ok(_mapper.Map<RoleDetailDto>(role));
        }

        [Authorize(Policy = Utilities.Module.Roleclaims)]
        [Authorize(Policy = Utilities.Permission.Add)]
        [Authorize(Policy = Utilities.Permission.Update)]
        [HttpPost("role-claim/add-update")]
        public async Task<IActionResult> AddOrUpdateRoleClaim([FromBody] RoleClaimDto roleClaimDto)
        {
            // Validate if Role exists
            var role = await _context.Roles.FirstOrDefaultAsync(r => r.Id == roleClaimDto.Id);
            if (role == null)
                return BadRequest("Invalid RoleId.");

            // Update Role Name if changed
            if (!string.IsNullOrEmpty(roleClaimDto.RoleName) && role.RoleName != roleClaimDto.RoleName)
            {
                role.RoleName = roleClaimDto.RoleName;
                _context.Roles.Update(role);
            }

            // Extract distinct ModuleIds
            var moduleIds = roleClaimDto.ModulePermissions.Select(mp => mp.ModuleId).Distinct().ToList();

            // Validate ModuleIds
            var existingModules = await _context.Modules
                .Where(m => moduleIds.Contains(m.ModuleId))
                .Select(m => m.ModuleId)
                .ToListAsync();

            if (existingModules.Count != moduleIds.Count)
                return BadRequest("One or more ModuleIds are invalid.");

            // Extract distinct PermissionIds
            var permissionIds = roleClaimDto.ModulePermissions
                .SelectMany(mp => mp.Permissions)
                .Select(p => p.PermissionId)
                .Distinct()
                .ToList();

            // Validate PermissionIds
            var existingPermissions = await _context.Permissions
                .Where(p => permissionIds.Contains(p.Id))
                .Select(p => p.Id)
                .ToListAsync();

            if (existingPermissions.Count != permissionIds.Count)
                return BadRequest("One or more PermissionIds are invalid.");

            // Check if RoleClaim already exists
            var existingRoleClaim = await _context.RoleClaims
                .FirstOrDefaultAsync(rc => rc.RoleId == roleClaimDto.Id);

            if (existingRoleClaim == null)
            {
                // Create a new RoleClaim
                var newRoleClaim = new RoleClaim
                {
                    RoleId = (int)roleClaimDto.RoleId,
                    ModulePermissions = roleClaimDto.ModulePermissions.Select(mp => new ModulePermissionData
                    {
                        ModuleId = mp.ModuleId,
                        Permissions = mp.Permissions.Select(p => new PermissionData { PermissionId = p.PermissionId }).ToList()
                    }).ToList()
                };
                await _context.RoleClaims.AddAsync(newRoleClaim);
            }
            else
            {
                // Update existing RoleClaim
                existingRoleClaim.ModulePermissions = roleClaimDto.ModulePermissions.Select(mp => new ModulePermissionData
                {
                    ModuleId = mp.ModuleId,
                    Permissions = mp.Permissions.Select(p => new PermissionData { PermissionId = p.PermissionId }).ToList()
                }).ToList();
            }

            await _context.SaveChangesAsync();

            return Ok(new { status = true, message = "RoleClaim and Role Name updated successfully." });
        }


        [Authorize(Policy = Utilities.Module.Roleclaims)]
        [Authorize(Policy = Utilities.Permission.GetAll)]
        [HttpGet("role-claim/{roleId}")]
        public async Task<IActionResult> GetRoleWithClaims(int roleId)
        {
            var role = await _context.Roles
                .Include(r => r.ParentRole)
                .Include(r => r.RoleClaims)
                .FirstOrDefaultAsync(r => r.Id == roleId);

            if (role == null)
                return NotFound("Role not found.");

            //var roleDetailDto = new RoleDetailDto
            //{
            //    Id = role.Id,
            //    RoleName = role.RoleName,
            //    DisplayName = role.DisplayName,
            //    ParentRoleId = role.ParentRoleId,
            //    ParentRoleName = role.ParentRole?.RoleName,
            //    RoleClaims = role.RoleClaims.Select(rc => new ModulePermissionDto
            //    {
            //        ModuleId = rc.ModulePermissions.Select(mp => mp.ModuleId).FirstOrDefault(),
            //        ModuleName = _context.Modules.Where(m => m.ModuleId == rc.ModulePermissions.Select(mp => mp.ModuleId).FirstOrDefault())
            //                                     .Select(m => m.ModuleName).FirstOrDefault(),
            //        Permissions = rc.ModulePermissions.SelectMany(mp => mp.Permissions)
            //                                          .Select(p => new PermissionDataDto { PermissionId = p.PermissionId })
            //                                          .ToList()
            //    }).ToList()
            //};

            var roleDetailDto = new RoleDetailOutputDto
            {
                Id = role.Id,
                RoleName = role.RoleName,
                DisplayName = role.DisplayName,
                ParentRoleId = role.ParentRoleId,
                ParentRoleName = role.ParentRole?.RoleName,
                RoleClaims = role.RoleClaims.FirstOrDefault()?.ModulePermissionsJson
            };

            return Ok(roleDetailDto);
        }

    }
}
