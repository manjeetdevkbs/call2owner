using Microsoft.AspNetCore.Http;
using Newtonsoft.Json.Linq;
using RestSharp;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Utilities
{
    public class JwtHelper
    {
        private readonly RestClient _client;

        public JwtHelper(RestClient client)
        {
            _client = client;
        }

        public async Task<(string roleId, List<string> userIds, string errorMessage)> ProcessJwtTokenAsync(HttpContext httpContext)
        {
            var token = httpContext.Request.Headers["Authorization"].ToString();
            if (string.IsNullOrEmpty(token) || !token.StartsWith("Bearer "))
            {
                return (null, null, "Missing Authorization Token");
            }

            token = token.Replace("Bearer ", "");

            // Decode JWT Token to get the Role ID
            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(token);
            var roleId = jwtToken.Claims.FirstOrDefault(c => c.Type == "role")?.Value;

            if (string.IsNullOrEmpty(roleId))
            {
                return (null, null, "Role ID not found in token");
            }

            // Call UserManagerClient API to get users belonging to child roles
            // Need to replace
            //var request = new RestRequest($"UserManagerClient/GetUsersByChildRoles/{roleId}", Method.Get);
            //var request = new RestRequest($"https://localhost:7054/UserManagerClient/GetUsersByChildRoles/{roleId}", Method.Get);
            var request = new RestRequest($"https://outmicro.kindlebit.com/UserManagerClient/GetUsersByChildRoles/{roleId}", Method.Get);
            request.AddHeader("accept", "*/*");
            request.AddHeader("Authorization", $"Bearer {token}");

            var response = await _client.ExecuteAsync(request);

            if (!response.IsSuccessful)
            {
                return (null, null, $"Error: {response.StatusCode} - {response.ErrorMessage}");
            }

            // Parse JSON response dynamically to extract user IDs
            var userIds = new List<string>();
            var jsonArray = JArray.Parse(response.Content);

            foreach (var item in jsonArray)
            {
                var userId = item["id"]?.ToString();
                if (!string.IsNullOrEmpty(userId))
                {
                    userIds.Add(userId);
                }
            }

            return (roleId, userIds, null);
        }
    }
}

