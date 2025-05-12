using AutoMapper;
using Oversight.DTO;
using Oversight.Model;

namespace Oversight
{
    public class MappingProfile:Profile
    {
        public MappingProfile()
        {
            CreateMap<User, UserDto>();

            CreateMap<Module, ModuleDto>();
            CreateMap<Permission, PermissionDto>();
            CreateMap<ModulePermission, ModulePermissionDto>()
                .ForMember(dest => dest.ModuleName, opt => opt.MapFrom(src => src.Module.ModuleName))
                .ForMember(dest => dest.Permissions, opt => opt.MapFrom(src => src.Permissions));

            // Mapping from PermissionData to PermissionDataDto
            CreateMap<PermissionData, PermissionDataDto>();

            CreateMap<Role, RoleDto>()
            .ForMember(dest => dest.ParentRoleId, opt => opt.MapFrom(src => src.ParentRoleId))
            .ForMember(dest => dest.ParentRoleName, opt => opt.MapFrom(src => src.ParentRole != null ? src.ParentRole.RoleName : null));

            
            CreateMap<Role, RoleDetailDto>().ReverseMap();
            CreateMap<RoleClaim, RoleClaimDto>();

            CreateMap<Role, RoleDetailDto>()
                .ForMember(dest => dest.ParentRoleId, opt => opt.MapFrom(src => src.ParentRoleId))
                .ForMember(dest => dest.ParentRoleName, opt => opt.MapFrom(src => src.ParentRole != null ? src.ParentRole.RoleName : null))
                .ForMember(dest => dest.RoleClaims, opt => opt.MapFrom(src => src.RoleClaims));
        }
    }
}
