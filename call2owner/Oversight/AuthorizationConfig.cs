using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Oversight;
using Utilities;

namespace AuthorizationLibrary
{
    public static class AuthorizationConfig
    {
        public static IServiceCollection ConfigureAuthorizationPolicies(this IServiceCollection services)
        {
            services.AddAuthorization(options =>
            {
                // Policy permissions (Insurance)
                AddPolicy(options, Module.User);
                AddPolicy(options, Module.Quotation);
                AddPolicy(options, Module.Claim);

                // Quotation permissions
                AddPolicy(options, Permission.Add);
                AddPolicy(options, Permission.Update);
                AddPolicy(options, Permission.Delete);
                AddPolicy(options, Permission.GetSingle);
                AddPolicy(options, Permission.GetAll);
            });

            services.AddSingleton<IAuthorizationHandler, PermissionAuthorizationHandler>();
            return services;
        }

        public class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
        {
            protected override Task HandleRequirementAsync(
                AuthorizationHandlerContext context,
                PermissionRequirement requirement)
            {
                var permissionClaim = context.User.FindFirst("Permissions")?.Value;

                if (!string.IsNullOrEmpty(permissionClaim))
                {
                    try
                    {
                        var userPermissions = JsonSerializer.Deserialize<List<Oversight.Model.ModulePermissionData>>(permissionClaim);
                        if (userPermissions != null)
                        {
                            foreach (var module in userPermissions)
                            {
                                if (module.Permissions.Any(p => p.PermissionId.ToString() == requirement.RequiredPermission))
                                {
                                    context.Succeed(requirement);
                                    break;
                                }
                            }
                        }
                    }
                    catch (JsonException)
                    {
                        // Log invalid JSON format error if needed
                    }
                }

                return Task.CompletedTask;
            }
        }

        private static void AddPolicy(AuthorizationOptions options, string permission)
        {
            options.AddPolicy(permission, policy =>
                policy.Requirements.Add(new PermissionRequirement(permission)));
        }
    }
}