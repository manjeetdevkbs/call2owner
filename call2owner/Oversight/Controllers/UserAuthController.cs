using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OfficeOpenXml.FormulaParsing.LexicalAnalysis;
using Oversight.DTO;
using Oversight.Model;
using Oversight.Models;
using Oversight.Services;
using RestSharp;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Utilities;

namespace Oversight.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserAuthController : ControllerBase
    {
        private readonly IMapper _mapper;
        private readonly DataContext _context;
        private readonly IConfiguration _configuration;
        private readonly EmailService _emailService;
        private readonly string EncryptionKey = "ABCabc123!@#hdgRHF1245KDnjkjfdsfdkv";
        private readonly ILogger<AuthController> _logger;
        private readonly RestClient _client;

        public static string SanitizeBase64(string base64)
        {
            return base64.Replace(" ", "").Replace("-", "+").Replace("_", "/");
        }

        public UserAuthController(DataContext context, IConfiguration configuration,
            IMapper mapper, EmailService emailService, ILogger<AuthController> logger, RestClient client)
        {
            _mapper = mapper;
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _emailService = emailService ?? throw new ArgumentNullException(nameof(emailService));
            _logger = logger;
            _client = client;
        }

        private byte[] GetAesKey()
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                return sha256.ComputeHash(Encoding.UTF8.GetBytes(EncryptionKey));
            }
        }

        [AllowAnonymous]
        [HttpPost("register-self-resident")]

        public async Task<IActionResult> SelfRegisterResident([FromBody] UserResidentDto dto)
        {
            OTPGenerator otpGenerator = new OTPGenerator();
            string otp = otpGenerator.GenerateOTP();

            var newGuid = Guid.NewGuid();

            // Get user using phone mumber
            var user = new User
            {
                MobileNumber = dto.MobileNumber,
                OTP = otp,
                OtpExpireTime = DateTime.UtcNow.AddMinutes(5),
                ResendOtpTime = DateTime.UtcNow.AddMinutes(2),
                RoleId = 1,
                OtpValidatedOn = null,
            };

            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();

            return Ok(new { message = "User registered successfully! Check your email to set a password." });
        }

        [AllowAnonymous]
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginSelfDto model)
        {
            _logger.LogInformation("This is a test log from Application Insights");
            _logger.LogError("This is a test log from Application Insights");

            var user = await _context.Users
                .Include(u => u.Role)
                    .ThenInclude(r => r.RoleClaims) // Include RoleClaims under Role
                .FirstOrDefaultAsync(u => u.Email == model.UserName || u.MobileNumber == model.UserName);

            if (user == null || !user.IsActive.GetValueOrDefault() || !user.IsVerified.GetValueOrDefault())
                return Unauthorized(new { message = "Account is not active or verified. Please reset your password." });


            //var hashedPassword = BCrypt.Net.BCrypt.HashPassword(model.Password);

            // Check OTP

            var roleClaimValues = user.Role.RoleClaims
                                .OrderBy(rc => rc.Id)
                                .Select(rc => rc.ModulePermissionsJson.ToString())
                                .FirstOrDefault();


            var token = GenerateJwtToken(user, roleClaimValues);

            UserDto userDto = _mapper.Map<UserDto>(user); // Convert to DTOs

            object insurerData = null;

            // Check if user is Insurer
            if (user.Role?.Id == Convert.ToInt32(UserRoles.InsurerAdmin))
            {
                var request = new RestRequest("https://outinsurer.kindlebit.com/api/Insurer/getAllInsurers", Method.Get);
                //var request = new RestRequest("https://localhost:7046/api/Insurer/getAllInsurers", Method.Get);
                request.AddHeader("accept", "*/*");
                request.AddHeader("Authorization", $"Bearer {token}");

                var response = await _client.ExecuteAsync(request);

                if (response.IsSuccessful && !string.IsNullOrEmpty(response.Content))
                {
                    var insurers = System.Text.Json.JsonSerializer.Deserialize<List<InsurerDto>>(response.Content, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    // Match insurer based on Email or UserID
                    insurerData = insurers?.FirstOrDefault(i => i.Email == user.Email);
                }
            }

            return Ok(new { token, role = user.Role?.RoleName, User = userDto, InsurerId = insurerData });
        }

        private string GenerateJwtToken(User user, string modulePermissions)
        {
            var jwtKey = _configuration["JwtSettings:Key"];
            var jwtIssuer = _configuration["JwtSettings:Issuer"];
            var jwtAudience = _configuration["JwtSettings:Audience"];
            var expiryMinutes = _configuration["JwtSettings:ExpiryMinutes"];

            if (string.IsNullOrEmpty(jwtKey) || string.IsNullOrEmpty(jwtIssuer) || string.IsNullOrEmpty(jwtAudience) || string.IsNullOrEmpty(expiryMinutes))
                throw new InvalidOperationException("JWT settings are not configured properly.");

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
            var tokenHandler = new JwtSecurityTokenHandler();

            var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                    new Claim(ClaimTypes.Email, user.Email),
                    new Claim(ClaimTypes.Role, user.Role?.Id.ToString() ?? "0"),
                    new Claim("FirstName", user.FirstName ?? ""),
                    new Claim("LastName", user.LastName ?? ""),
                    new Claim("MobileNumber", user.MobileNumber ?? ""),
                    new Claim("UserId", user.Id.ToString()),
                    new Claim("RoleName", user.Role?.RoleName ?? "User")
                };

            claims.Add(new Claim("Permissions", string.IsNullOrWhiteSpace(modulePermissions) ? "" : modulePermissions));

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddMinutes(int.Parse(expiryMinutes)),
                Issuer = jwtIssuer,
                Audience = jwtAudience,
                SigningCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }
    }

    public class OTPGenerator
    {
        private Random random = new Random();
        private List<string> last10OTPs = new List<string>();

        public string GenerateOTP()
        {
            string otp;

            do
            {
                // Generate a random 6-digit number
                int otpNumber = random.Next(100000, 999999);

                // Convert the number to a string
                otp = otpNumber.ToString();
            } while (last10OTPs.Contains(otp)); // Ensure the OTP is not in the last 10 generated OTPs

            // Add the new OTP to the list and remove the oldest OTP if the list exceeds 10 items
            last10OTPs.Add(otp);
            if (last10OTPs.Count > 10)
            {
                last10OTPs.RemoveAt(0);
            }

            return otp;
        }
    }

    }