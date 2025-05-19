using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using System;
using System.Linq;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;

namespace Oversight
{
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
                    string jsonString = permissionClaim;

                    // Fix incorrectly escaped JSON
                    jsonString = jsonString.Trim(); // Remove any extra spaces or newlines
                    if (jsonString.StartsWith("\"") && jsonString.EndsWith("\""))
                    {
                        jsonString = jsonString.Substring(1, jsonString.Length - 2); // Remove surrounding quotes
                    }
                    jsonString = jsonString.Replace("\\\"", "\""); // Replace escaped quotes

                    var userPermissions = JsonConvert.DeserializeObject<List<Utilities.ModulePermissionData>>(jsonString);

                    if (userPermissions != null)
                    {
                        // Check if the required permission exists in any module
                        if (userPermissions.Any(mp => mp.Permissions.Any(p => p.PermissionId.ToString() == requirement.RequiredPermission))
                            || userPermissions.Any(mp => mp.ModuleId.ToString() == requirement.RequiredPermission))
                        {
                            context.Succeed(requirement);
                        }
                    }
                }
                catch (Exception exx)
                {
                    // Handle JSON deserialization error (optional logging)
                }
            }

            return Task.CompletedTask;
        }
    }
    public class PermissionRequirement : IAuthorizationRequirement
    {
        public string RequiredPermission { get; }

        public PermissionRequirement(string requiredPermission)
        {
            RequiredPermission = requiredPermission;
        }
    }
}
