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
    public class AuthController : ControllerBase
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

        public AuthController(DataContext context, IConfiguration configuration,
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

        #region Public Methods

        [EmailTrigger(Utilities.Module.Auth, Utilities.Permission.Update)]
        [Authorize(Policy = Utilities.Module.Auth)]
        [Authorize(Policy = Utilities.Permission.Add)]
        [Authorize]
        [HttpPost("register")]

        public async Task<IActionResult> Register([FromBody] UserDto model)
        {
            var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

            if (currentUserId == 0)
                return Unauthorized(new { message = "Invalid user." });

            var currentUser = await _context.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Id == currentUserId);
            if (currentUser == null)
                return Unauthorized(new { message = "User not found or unauthorized." });

            var token = HttpContext.Request.Headers["Authorization"].ToString();
            if (string.IsNullOrEmpty(token))
            {
                Console.WriteLine("Missing Authorization Token");
                return Unauthorized("Missing Authorization Token");
            }

            token = token.Replace("Bearer ", "");
            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(token);
            var roleId = jwtToken.Claims.FirstOrDefault(c => c.Type == "role")?.Value;

            if (currentUser.RoleId == Convert.ToInt32(UserRoles.InsurerAdmin))
            {
                // Forcefully assign InsurerCustomer role, no need for client to send it
                model.RoleId = Convert.ToInt32(UserRoles.InsurerCustomer);
            }

            if (await _context.Users.AnyAsync(u => u.Email == model.Email))
                return BadRequest(new { message = "Email already exists!" });


            // Ensure that the new user cannot have the same role as the current user
            if (currentUser.RoleId == model.RoleId)
                return BadRequest(new { message = "You cannot assign the same role as yours." });
            // Validate whether the current user can assign the requested role
            string role = GetUserRoleFromToken();

            var validChildRole = await _context.Roles.AnyAsync(r => r.Id == model.RoleId && r.ParentRoleId == currentUser.RoleId);
            if (!validChildRole && role != UserRoles.SuperAdmin)
                return Forbid("You do not have permission to assign this role.");

            var verificationCode = Guid.NewGuid().ToString();

            string encryptedEmail = Encrypt(model.Email);
            string encryptedToken = Encrypt(verificationCode);


            string resetLink = $"http://geneinsure.kindlebit.com/set-password?{encryptedToken}&&{encryptedEmail}";
            var user = new User
            {
                FirstName = model.FirstName,
                LastName = model.LastName,
                Email = model.Email,
                MobileNumber = model.MobileNumber,
                RoleId = model.RoleId,
                VerificationCode = verificationCode,
                ParentRoleId = currentUser.RoleId,
                VerificationCodeGenerationTime = DateTime.UtcNow,
                IsActive = true,
                IsDeleted = false,
                IsVerified = false,
                CreatedBy = currentUserId,
                resetLink = resetLink
            };

            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();


            if (user.RoleId == Convert.ToInt32(UserRoles.InsurerCustomer))
            {
                var insurerToSendDTO = new InsurerUserDTO
                {
                    UserId = user.Id,
                    InsurerId = currentUserId,
                    IsActive = user.IsActive ?? false,
                    IsDeleted = user.IsDeleted ?? false
                };


                //var postRequest = new RestRequest("https://localhost:7046/api/Insurer/add-insurer-user", Method.Post);
                var postRequest = new RestRequest("https://outinsurer.kindlebit.com/api/Insurer/add-insurer-user", Method.Post);
                postRequest.AddHeader("accept", "application/json");
                postRequest.AddHeader("Content-Type", "application/json");
                postRequest.AddHeader("Authorization", $"Bearer {token}");

                // Send the updated/confirmed DTO
                postRequest.AddJsonBody(insurerToSendDTO);

                var postResponse = await _client.ExecuteAsync(postRequest);

            }

            string jsonVariables = JsonConvert.SerializeObject(user);
            var recipientEmail = Helper.ExtractMatchingValues(jsonVariables);
            HttpContext.Items["RecipientEmail"] = recipientEmail;
            HttpContext.Items["VariablesRaw"] = jsonVariables;
            //await SendVerificationEmail(user.Email, verificationCode);

            return Ok(new { message = "User registered successfully! Check your email to set a password." });
        }

        [Authorize(Policy = Utilities.Module.Auth)]
        [Authorize(Policy = Utilities.Permission.GetSingle)]
        [Authorize] // Ensure only authenticated users can access this
        [HttpGet("currentUserDetail")]
        public async Task<IActionResult> GetCurrentUser()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier); // Extract user ID from token
            if (userIdClaim == null)
                return Unauthorized(new { message = "Invalid token or user not found." });

            int userId = int.Parse(userIdClaim.Value);

            var user = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
                return NotFound(new { message = "User not found." });

            var userDto = new UserDto
            {
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                MobileNumber = user.MobileNumber,
                RoleId = user.RoleId
            };

            return Ok(new
            {
                UserId = user.Id,
                Token = Request.Headers["Authorization"].ToString().Replace("Bearer ", "") // Send token
            });
        }

        [EmailTrigger(Utilities.Module.Auth, Utilities.Permission.ResendVerificationEmail)]
        [HttpPost("resend-verification-email")]
        public async Task<IActionResult> ResendVerificationEmail([FromBody] DTO.ResendEmailRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Email))
                return BadRequest("Email is required.");

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == request.Email);

            if (user == null)
                return NotFound("User not found.");

            if (user.IsVerified == true)
                return BadRequest("Email is already verified.");

            var verificationCode = Guid.NewGuid().ToString();

            string encryptedEmail = Encrypt(user.Email);
            string encryptedToken = Encrypt(verificationCode);
            var resetLink = $"http://geneinsure.kindlebit.com/set-password?{encryptedToken}&&{encryptedEmail}";
            string emailBody = $@"
                <h2>Set Your Password</h2>
                <p>Click the link below to set your password:</p>
                <a href='{resetLink}' style='padding:10px 20px; background:#28a745; color:white; text-decoration:none; border-radius:5px;'>Set Password</a>
                <p>If you didn't request this, ignore this email.</p>";

            user.resetLink = resetLink;

            user.VerificationCode = verificationCode;
            user.VerificationCodeGenerationTime = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            //await SendVerificationEmail(user.Email, user.VerificationCode);

            string jsonVariables = JsonConvert.SerializeObject(user);
            var recipientEmail = Helper.ExtractMatchingValues(jsonVariables);
            HttpContext.Items["RecipientEmail"] = recipientEmail;
            HttpContext.Items["VariablesRaw"] = jsonVariables;

            return Ok("Verification email has been resent.");
        }

        [AllowAnonymous]
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto model)
        {
            _logger.LogInformation("This is a test log from Application Insights");
            _logger.LogError("This is a test log from Application Insights");

            var user = await _context.Users
                .Include(u => u.Role)
                    .ThenInclude(r => r.RoleClaims) // Include RoleClaims under Role
                .FirstOrDefaultAsync(u => u.Email == model.Email);

            if (user == null || !user.IsActive.GetValueOrDefault() || !user.IsVerified.GetValueOrDefault())
                return Unauthorized(new { message = "Account is not active or verified. Please reset your password." });


            //var hashedPassword = BCrypt.Net.BCrypt.HashPassword(model.Password);

            if (!BCrypt.Net.BCrypt.Verify(model.Password, user.PasswordHash))
                return Unauthorized(new { message = "Invalid Password!" });

            var roleClaimValues = user.Role.RoleClaims
                                .OrderBy(rc => rc.Id)
                                .Select(rc => rc.ModulePermissionsJson.ToString())
                                .FirstOrDefault();
            

            //var fixedJson = JsonSerializer.Deserialize<string>(roleClaimValues);  // First deserialization
            //var modulePermissions = JsonSerializer.Deserialize<List<ModulePermissionData>>(roleClaimValues);  

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

        [Authorize(Policy = Utilities.Module.Auth)]
        [Authorize(Policy = Utilities.Permission.GetSingle)]
        [HttpGet("profile")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public IActionResult GetUserProfile()
        {
            var userId = User.FindFirst("UserId")?.Value;
            var firstName = User.FindFirst("FirstName")?.Value;
            var lastName = User.FindFirst("LastName")?.Value;
            var role = User.FindFirst(ClaimTypes.Role)?.Value;

            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(firstName) || string.IsNullOrEmpty(lastName) || string.IsNullOrEmpty(role))
                return Unauthorized(new { message = "User claims not found!" });

            return Ok(new
            {
                UserId = userId,
                FirstName = firstName,
                LastName = lastName,
                Role = role
            });
        }

        [HttpPost("send-reset-link")]
        public async Task<IActionResult> SendResetLink([FromQuery] string email)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null)
                return BadRequest(new { message = "User not found." });

            var token = Guid.NewGuid().ToString();
            user.VerificationCode = token;
            user.VerificationCodeGenerationTime = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            string encryptedEmail = Encrypt(email);
            string encryptedToken = Encrypt(token);
            var resetLink = $"https://geneinsure.kindlebit.com/api/auth/set-password?{encryptedToken}&&{encryptedEmail}";
            //var resetLink = $"http://outriskappback.kindlebit.net/set-password?{encryptedToken}&&{encryptedEmail}";

            string emailBody = $@"
                <h2>Set Your Password</h2>
                <p>Click the link below to set your password:</p>
                <a href='{resetLink}' style='padding:10px 20px; background:#28a745; color:white; text-decoration:none; border-radius:5px;'>Set Password</a>
                <p>If you didn't request this, ignore this email.</p>";

            await _emailService.SendEmailAsync(email, "Set Your Password - Oversight", emailBody);

            return Ok(new { message = "Reset link sent successfully.", email, emailBody });
        }

        [EmailTrigger(Utilities.Module.Auth, Utilities.Permission.ForgetEmailPwd)]
        [HttpPost("forget-password/{email}")]
        public async Task<IActionResult> ForgetPassword(string email)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null)
                return BadRequest(new { message = "User not found." });

            var token = Guid.NewGuid().ToString();
            user.VerificationCode = token;
            user.VerificationCodeGenerationTime = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            string encryptedEmail = Encrypt(email);
            string encryptedToken = Encrypt(token);
            var resetLink = $"http://geneinsure.kindlebit.com/set-password?{encryptedToken}&&{encryptedEmail}";

            user.resetLink = resetLink;

            //var resetLink = $"http://outriskappback.kindlebit.net/set-password?{encryptedToken}&&{encryptedEmail}";

            string emailBody = $@"
        <h2>Set Your Password</h2>
        <p>Click the link below to set new password:</p>
        <a href='{resetLink}' style='padding:10px 20px; background:#28a745; color:white; text-decoration:none; border-radius:5px;'>Set Password</a>
        <p>If you didn't request this, ignore this email.</p>";

            //await _emailService.SendEmailAsync(email, "Set Your Password - Oversight", emailBody);

            string jsonVariables = JsonConvert.SerializeObject(user);
            var recipientEmail = Helper.ExtractMatchingValues(jsonVariables);
            HttpContext.Items["RecipientEmail"] = recipientEmail;
            HttpContext.Items["VariablesRaw"] = jsonVariables;


            return Ok(new { message = "Reset link sent successfully.", email, emailBody });
        }


        //[HttpPost("reset-password")]
        //public async Task<IActionResult> ResetPassword([FromBody] SetPasswordModel model)
        //{
        //    try
        //    {
        //        // Validate input
        //        if (model == null || string.IsNullOrEmpty(model.EncryptedToken) || string.IsNullOrEmpty(model.EncryptedPassword))
        //            return BadRequest(new { message = "Invalid input data." });

        //        // Split token before and after "&&"
        //        string[] tokenParts = model.EncryptedToken.Split(new string[] { "&&" }, StringSplitOptions.None);
        //        if (tokenParts.Length != 2)
        //            return BadRequest(new { message = "Invalid token format. Ensure correct encoding and structure." });

        //        string encryptedTokenPart1 = tokenParts[0];
        //        string encryptedTokenPart2 = tokenParts[1];

        //        // Ensure valid Base64 format
        //        if (!IsBase64String(encryptedTokenPart1) || !IsBase64String(encryptedTokenPart2))
        //            return BadRequest(new { message = "Invalid token format. Ensure Base64 encoding." });

        //        // Attempt decryption
        //        string token, email, newPassword;
        //        try
        //        {
        //            token = Decrypt(encryptedTokenPart1);
        //            email = Decrypt(encryptedTokenPart2);
        //            newPassword = model.EncryptedPassword;
        //        }
        //        catch (FormatException ex)
        //        {
        //            return BadRequest(new { message = "Decryption failed. Invalid Base64 format.", error = ex.Message });
        //        }

        //        // Find user by token
        //        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
        //        if (user == null)
        //            return NotFound(new { message = "Invalid email.", ResendToken = false });

        //        // Validate stored token
        //        string storedToken = user.VerificationCode;
        //        if (string.IsNullOrEmpty(storedToken) || storedToken != token)
        //            return NotFound(new { message = "Invalid token.", ResendToken = false, email });

        //        // Check token expiration (valid for 1 hour)
        //        bool isTokenValid = user.VerificationCodeGenerationTime > DateTime.UtcNow.AddMinutes(-60);
        //        if (!isTokenValid)
        //            return NotFound(new { message = "Token has expired.", ResendToken = true, Email = email });

        //        // Validate password strength
        //        if (!IsValidPassword(newPassword))
        //            return Unauthorized(new { message = "Password must contain at least one uppercase letter, one lowercase letter, one number, and one special character.", ResendToken = false });

        //        // Activate user
        //        if (user.IsVerified == false || user.IsActive == true || user.IsVerified != null || user.IsActive != null)
        //        {
        //            user.IsVerified = true;
        //        }

        //        // Hash and store new password
        //        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
        //        user.VerificationCode = null; // Remove token after use
        //        user.VerificationCodeValidationTime = DateTime.UtcNow;

        //        await _context.SaveChangesAsync();

        //        return Ok(new { message = "Password successfully set! You can now log in.", status = true, Email = email });
        //    }
        //    catch (Exception ex)
        //    {
        //        return StatusCode(500, new { message = "An error occurred while processing your request.", error = ex.Message, status = false });
        //    }
        //}


        [HttpPost("set-password")]
        public async Task<IActionResult> SetPassword([FromBody] SetPasswordModel model)
        {
            try
            {
                // Validate input
                if (model == null || string.IsNullOrEmpty(model.EncryptedToken) || string.IsNullOrEmpty(model.EncryptedPassword))
                    return BadRequest(new { message = "Invalid input data." });

                // Split token before and after "&&"
                string[] tokenParts = model.EncryptedToken.Split(new string[] { "&&" }, StringSplitOptions.None);
                if (tokenParts.Length != 2)
                    return BadRequest(new { message = "Invalid token format. Ensure correct encoding and structure." });

                string encryptedTokenPart1 = tokenParts[0];
                string encryptedTokenPart2 = tokenParts[1];

                // Ensure valid Base64 format
                if (!IsBase64String(encryptedTokenPart1) || !IsBase64String(encryptedTokenPart2))
                    return BadRequest(new { message = "Invalid token format. Ensure Base64 encoding." });

                // Attempt decryption
                string token, email, newPassword;
                try
                {
                    token = Decrypt(encryptedTokenPart1);
                    email = Decrypt(encryptedTokenPart2);
                    newPassword = model.EncryptedPassword;
                }
                catch (FormatException ex)
                {
                    return BadRequest(new { message = "Decryption failed. Invalid Base64 format.", error = ex.Message });
                }

                // Find user by token
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
                if (user == null)
                    return NotFound(new { message = "Invalid email.", ResendToken = false });

                // Validate stored token
                string storedToken = user.VerificationCode;
                if (string.IsNullOrEmpty(storedToken) || storedToken != token)
                    return NotFound(new { message = "Invalid token.", ResendToken = false, email });

                // Check token expiration (valid for 24 hours)
                bool isTokenValid = user.VerificationCodeGenerationTime > DateTime.UtcNow.AddMinutes(-60);
                if (!isTokenValid)
                    return NotFound(new { message = "Token has expired.", ResendToken = true, Email = email });

                // Validate password strength
                if (!IsValidPassword(newPassword))
                    return Unauthorized(new { message = "Password must contain at least one uppercase letter, one lowercase letter, one number, and one special character.", ResendToken = false });

                // Activate user
                if (user.IsVerified == false || user.IsActive == true || user.IsVerified != null || user.IsActive != null)
                {
                    user.IsVerified = true;
                }

                // Hash and store new password
                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
                user.VerificationCode = null; // Remove token after use
                user.VerificationCodeValidationTime = DateTime.UtcNow;

                // Create UserParent relationship (only if not exists)
                var userParent = new UserParent
                {
                    UserId = user.Id,
                    ParentId = (int)user.CreatedBy,
                    IsActive = true,
                    IsDeleted = false,
                    IsVerified = true
                };
                _context.UserParents.Add(userParent);
                await _context.SaveChangesAsync();

                await _context.SaveChangesAsync();

                return Ok(new { message = "Password successfully set! You can now log in.", ResendToken = false, Email = email });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while processing your request.", error = ex.Message, ResendToken = false });
            }
        }

        [Authorize(Policy = Utilities.Module.Auth)]
        [Authorize(Policy = Utilities.Permission.GetAll)]
        [HttpGet("GetUsersByChildRoles")]
        public async Task<IActionResult> GetUsersByChildRoles()
        {
            string role = GetUserRoleFromToken();
            if (string.IsNullOrEmpty(role))
                return Unauthorized("Role not found in token");

            // Find the Role ID of the Logged-in User
            var userRole = await _context.Roles.FirstOrDefaultAsync(r => r.Id.ToString() == role);
            if (userRole == null)
                return NotFound("Role not found in database");

            // Get all descendant Role IDs recursively
            var childRoleIds = await GetAllChildRoleIds(userRole.Id);

            // Include the current role in the list
            childRoleIds.Add(userRole.Id);

            if (!childRoleIds.Any())
                return NotFound("No child roles found");

            // Fetch Users belonging to those roles
            var users = await _context.Users
                .Where(u => childRoleIds.Contains(u.RoleId))
                .ToListAsync();

            var userList = _mapper.Map<List<UserDto>>(users); // Convert to DTOs

            return Ok(userList);
        }

        //[Authorize(Policy = Utilities.Module.Auth)]
        //[Authorize(Policy = Utilities.Permission.GetAll)]
        [HttpGet("getAllRoles")]
        public async Task<IActionResult> GetAllRoles()
        {
            var roles = await _context.Roles
                .Select(r => new RoleDtoOutput
                {
                    RoleId = r.Id,
                    RoleName = r.RoleName
                })
                .ToListAsync();

            return Ok(roles);
        }

        [AllowAnonymous]
        [HttpGet("getAllUsers")]
        public async Task<IActionResult> GetAllUsers()
        {
            var users = await _context.Users
                .Select(u => new UsersDtoOutput
                {
                    FirstName = u.FirstName,
                    LastName = u.LastName,
                    RoleId = u.RoleId,
                    Email = u.Email,
                    MobileNumber = u.MobileNumber,
                    IsActive = u.IsActive,
                    IsVerified = u.IsVerified
                })
                .ToListAsync();

            return Ok(users);
        }

        [HttpGet("brokerList")]
        public async Task<IActionResult> BrokerList()
        {
            var users = await _context.Users
                .Where(u => u.RoleId == Convert.ToInt32(UserRoles.Broker))
                .Select(u => new UsersDtoOutput
                {
                    Id = u.Id,
                    FirstName = u.FirstName,
                    LastName = u.LastName,
                    RoleId = u.RoleId,
                    Email = u.Email,
                    MobileNumber = u.MobileNumber,
                    IsActive = u.IsActive,
                    IsVerified = u.IsVerified
                })
                .ToListAsync();

            return Ok(users);
        }

        [HttpGet("brokerById/{brokerId}")]
        public async Task<IActionResult> BrokerById(int brokerId)
        {
            var users = await _context.Users
                .Where(u => u.Id == brokerId)
                .Select(u => new UsersDtoOutput
                {
                    Id = u.Id,
                    FirstName = u.FirstName,
                    LastName = u.LastName,
                    RoleId = u.RoleId,
                    Email = u.Email,
                    MobileNumber = u.MobileNumber, 
                    IsActive = u.IsActive,
                    IsVerified = u.IsVerified
                })
                .FirstOrDefaultAsync();

            return Ok(users);
        }

        [HttpGet("user-parent-list")]
        public async Task<IActionResult> UserParentList()
        {
            var usersParentList = await (
                from up in _context.UserParents
                join user in _context.Users on up.UserId equals user.Id
                join parent in _context.Users on up.ParentId equals parent.Id
                select new
                {
                    UserId = up.UserId,
                    UserFirstName = user.FirstName,
                    UserLastName = user.LastName,
                    UserEmail = user.Email,

                    ParentId = up.ParentId,
                    ParentFirstName = parent.FirstName,
                    ParentLastName = parent.LastName,
                    ParentEmail = parent.Email,

                    up.IsActive,
                    up.IsVerified
                }
            ).ToListAsync();

            if (usersParentList == null || !usersParentList.Any())
            {
                return NotFound(new { message = "No user-parent relationships found." });
            }

            return Ok(usersParentList);
        }


        //[HttpPost("assign-parent-to-users")]
        //public async Task<IActionResult> AssignParentToUsers([FromBody] AssignParentRequestDTO model)
        //{
        //    var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

        //    if (currentUserId == 0)
        //        return Unauthorized(new { message = "Invalid user." });

        //    var currentUser = await _context.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Id == currentUserId);
        //    if (currentUser == null)
        //        return Unauthorized(new { message = "User not found or unauthorized." });
        //    // Validate parent
        //    var parentUser = await _context.Users.FindAsync(model.ParentId);
        //    if (parentUser == null)
        //    {
        //        return NotFound(new { message = "Parent user not found." });
        //    }

        //    var validUserIds = await _context.Users
        //        .Where(u => model.UserIds.Contains(u.Id))
        //        .Select(u => u.Id)
        //        .ToListAsync();

        //    var newRelations = new List<UserParent>();

        //    foreach (var userId in validUserIds)
        //    {
        //        var exists = await _context.UserParents.AnyAsync(up => up.UserId == userId);
        //        if (!exists)
        //        {
        //            newRelations.Add(new UserParent
        //            {
        //                UserId = userId,
        //                ParentId = model.ParentId,
        //                CreatedBy = currentUserId,
        //                IsActive = true,
        //                IsDeleted = false,
        //                IsVerified = true
        //            });
        //        }
        //    }

        //    if (!newRelations.Any())
        //    {
        //        return Conflict(new { message = "No new user-parent mappings to assign." });
        //    }

        //    await _context.UserParents.AddRangeAsync(newRelations);
        //    await _context.SaveChangesAsync();

        //    return Ok(new
        //    {
        //        message = "Parent assigned to users successfully.",
        //        assignedCount = newRelations.Count
        //    });
        //}


        [HttpPost("assign-parent-to-users")]
        public async Task<IActionResult> AssignParentToUsers([FromBody] AssignParentRequestDTO model)
        {
            var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

            if (currentUserId == 0)
                return Unauthorized(new { message = "Invalid user." });

            var currentUser = await _context.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Id == currentUserId);
            if (currentUser == null)
                return Unauthorized(new { message = "User not found or unauthorized." });

            // Validate parent
            var parentUser = await _context.Users.FindAsync(model.ParentId);
            if (parentUser == null)
            {
                return NotFound(new { message = "Parent user not found." });
            }

            // Get only valid user IDs from the request
            var validUserIds = await _context.Users
                .Where(u => model.UserIds.Contains(u.Id))
                .Select(u => u.Id)
                .ToListAsync();

            // Remove any existing parent mappings for the users
            var existingRelations = await _context.UserParents
                .Where(up => validUserIds.Contains(up.UserId))
                .ToListAsync();

            if (existingRelations.Any())
            {
                _context.UserParents.RemoveRange(existingRelations);
            }

            // Create new mappings
            var newRelations = validUserIds.Select(userId => new UserParent
            {
                UserId = userId,
                ParentId = model.ParentId,
                CreatedBy = currentUserId,
                IsActive = true,
                IsDeleted = false,
                IsVerified = true
            }).ToList();

            await _context.UserParents.AddRangeAsync(newRelations);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Parent assigned to users successfully.",
                assignedUsers = newRelations.Select(x => x.UserId).ToList()
            });
        }



        [HttpGet("getAllUsersByRole/{roleId}")]
        public async Task<IActionResult> GetAllUsersByRole(int roleId)
        {
            var users = await _context.Users
                .Select(u => new UsersDtoOutput
                {
                    FirstName = u.FirstName,
                    LastName = u.LastName,
                    RoleId = u.RoleId,
                    Email = u.Email,
                    MobileNumber = u.MobileNumber,
                    IsActive = u.IsActive,
                    IsVerified = u.IsVerified,
                    Id = u.Id
                }).Where(x => x.RoleId == roleId)
                .ToListAsync();

            return Ok(users);
        }

        [HttpGet("parent-role-users/{roleId}")]
        public async Task<IActionResult> GetAllParentRoleUsers(int roleId)
        {
            // Step 1: Get the ParentRoleId of the given role
            var parentRoleId = await _context.Roles
                .Where(r => r.Id == roleId)
                .Select(r => r.ParentRoleId)
                .FirstOrDefaultAsync();

            if (parentRoleId == null)
            {
                return Ok(new List<UsersDtoOutput>());
            }

            // Step 2: Get users with the parent role ID
            var users = await _context.Users
                .Where(u => u.RoleId == parentRoleId)
                .Select(u => new UsersDtoOutput
                {
                    FirstName = u.FirstName,
                    LastName = u.LastName,
                    RoleId = u.RoleId,
                    Email = u.Email,
                    MobileNumber = u.MobileNumber,
                    IsActive = u.IsActive,
                    IsVerified = u.IsVerified,
                    Id = u.Id
                })
                .ToListAsync();

            return Ok(users);
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

        public static async Task SeedSuperAdminAsync(DataContext context)
        {
            var roleHierarchy = new List<(int Id, string RoleName, int? ParentRoleId)>
                                {
                                    (301, "SuperAdmin", null),
                                    (302, "OverWriter", 301),
                                    (303, "UnderWriter", 302),
                                    (304, "MasterBroker", 303),
                                    (305, "Broker", 304),
                                    (306, "ClaimUser", 304),
                                    (307, "FinanceUser", 304)
                                };

            var existingRoles = await context.Roles.ToDictionaryAsync(r => r.RoleName, r => r);

            // Begin transaction to ensure consistency
            using var transaction = await context.Database.BeginTransactionAsync();

            try
            {
                await context.Database.ExecuteSqlRawAsync("SET IDENTITY_INSERT Roles ON");

                foreach (var (id, roleName, parentRoleId) in roleHierarchy)
                {
                    if (!existingRoles.ContainsKey(roleName))
                    {
                        var trackedEntity = context.ChangeTracker.Entries<Role>()
                                                   .FirstOrDefault(e => e.Entity.Id == id);

                        if (trackedEntity != null)
                        {
                            trackedEntity.State = EntityState.Detached;
                        }

                        var role = new Role
                        {
                            Id = id,
                            RoleName = roleName,
                            ParentRoleId = parentRoleId
                        };

                        await context.Roles.AddAsync(role);

                        existingRoles[roleName] = role;
                    }
                }

                await context.SaveChangesAsync();

                await context.Database.ExecuteSqlRawAsync("SET IDENTITY_INSERT Roles OFF");


                await context.SaveChangesAsync();

                await context.Database.ExecuteSqlRawAsync("SET IDENTITY_INSERT Roles OFF");

                // Seed SuperAdmin user
                var superAdminRole = existingRoles["SuperAdmin"];
                if (superAdminRole != null && !await context.Users.AnyAsync(u => u.Email == "superadmin@gmail.com"))
                {
                    var superAdmin = new User
                    {
                        FirstName = "Super",
                        LastName = "Admin",
                        Email = "superadmin@gmail.com",
                        MobileNumber = "1122334455",
                        RoleId = superAdminRole.Id,
                        PasswordHash = BCrypt.Net.BCrypt.HashPassword("Super@123"),
                        IsActive = true,
                        IsVerified = true
                    };

                    await context.Users.AddAsync(superAdmin);
                    await context.SaveChangesAsync();
                }

                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
            }
        }


        public static bool IsBase64String(string base64)
        {
            if (string.IsNullOrEmpty(base64) || base64.Length % 4 != 0)
                return false;

            try
            {
                Convert.FromBase64String(base64);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static string DecryptToken(string encryptedText)
        {
            if (string.IsNullOrEmpty(encryptedText))
                throw new ArgumentException("Encrypted text cannot be null or empty.");

            try
            {
                encryptedText = SanitizeBase64(encryptedText); // Fix invalid characters
                byte[] encryptedBytes = Convert.FromBase64String(encryptedText);
                return Encoding.UTF8.GetString(encryptedBytes);
            }
            catch (FormatException)
            {
                throw new FormatException("Invalid Base64 format: The input string is not a valid Base64 encoded string.");
            }
        }


        [Authorize(Policy = Utilities.Module.Auth)]
        [Authorize(Policy = Utilities.Permission.GetAll)]
        [HttpGet("child-roles")]
        public async Task<IActionResult> GetChildRoles()
        {
            string roleName = GetUserRolesFromToken();
            if (string.IsNullOrEmpty(roleName))
                return Unauthorized("Role not found in token");

            var roles = await GetChildRolesAsync(roleName);
            if (!roles.Any())
                return NotFound("No child roles found");

            var rolesList = _mapper.Map<List<RoleDto>>(roles); // Convert to DTOs

            return Ok(rolesList);
        }


        //[HttpGet("hierarchy")]
        //public async Task<IActionResult> GetUserHierarchy()
        //{

        //    var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier); // Extract user ID from token
        //    if (userIdClaim == null)
        //        return Unauthorized(new { message = "Invalid token or user not found." });
        //    int userId = int.Parse(userIdClaim.Value);

        //    var users = await _context.Users.ToListAsync();
        //    var tree = BuildUserTree(users, userId);
        //    return Ok(tree);
        //}

        [Authorize(Policy = Utilities.Module.Auth)]
        [Authorize(Policy = Utilities.Permission.GetAll)]
        [HttpGet("hierarchy")]
        public async Task<IActionResult> GetUserHierarchy()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
                return Unauthorized(new { message = "Invalid token or user not found." });

            int userId = int.Parse(userIdClaim.Value);

            var users = await _context.Users.ToListAsync();

            // Build full tree from root
            var fullTree = BuildUserTree(users, null);

            // Find the subtree under the current user
            var userSubTree = FindUserSubTree(fullTree, userId);

            if (userSubTree == null)
                return NotFound(new { message = "User not found in the hierarchy." });

            return Ok(userSubTree);
        }

        //[Authorize(Policy = Utilities.Module.Auth)]
        //[Authorize(Policy = Utilities.Permission.GetAll)]
        [AllowAnonymous]
        [HttpGet("GetAllClaimUsersByInsurerId")]
        public async Task<IActionResult> GetAllClaimUsersByInsurerId([FromQuery] int insurerid)
        {
            var usersUnderInsurer = await _context.UserParents
                .Where(x => x.ParentId == insurerid)
                .Select(x => x.UserId)
                .ToListAsync();

            var users = await _context.Users
                .Where(x => usersUnderInsurer.Contains(x.Id))
               .Select(u => new UsersDtoOutput
               {
                   
                   FirstName = u.FirstName,
                   LastName = u.LastName,
                   RoleId = u.RoleId,
                   Email = u.Email,
                   MobileNumber = u.MobileNumber,
                   IsActive = u.IsActive,
                   IsVerified = u.IsVerified,
                   userId = u.Id
               })
               .ToListAsync();

            return Ok(users);
        }

        #endregion

        #region Private Method

        private List<ChildUsersDTO> BuildUserTree(List<User> users, int? parentId)
        {
            return users
                .Where(u => u.CreatedBy == parentId)
                .Select(u => new ChildUsersDTO
                {
                    UserId = u.Id,
                    FullName = $"{u.FirstName} {u.LastName}",
                    Email = u.Email,
                    Children = BuildUserTree(users, u.Id) // Recursive
                })
                .ToList();
        }


        private ChildUsersDTO? FindUserSubTree(List<ChildUsersDTO> tree, int userId)
        {
            foreach (var node in tree)
            {
                if (node.UserId == userId)
                    return node;

                var found = FindUserSubTree(node.Children, userId);
                if (found != null)
                    return found;
            }
            return null;
        }



        //private List<ChildUsersDTO> BuildUserTree(List<User> users, int? parentId = null)
        //{
        //    return users
        //        .Where(u => u.CreatedBy == parentId)
        //        .Select(u => new ChildUsersDTO
        //        {
        //            UserId = u.Id,
        //            FullName = $"{u.FirstName} {u.LastName}",
        //            Email = u.Email,
        //            Children = BuildUserTree(users, u.Id)
        //        })
        //        .ToList();
        //}


        private async Task SendVerificationEmail(string email, string token)
        {
            string encryptedEmail = Encrypt(email);
            string encryptedToken = Encrypt(token);

            //var resetLink = $"http://localhost:7260/set-password?{encryptedToken}&&{encryptedEmail}";
            var resetLink = $"http://geneinsure.kindlebit.com/set-password?{encryptedToken}&&{encryptedEmail}";

            string emailBody = $@"
                <h2>Set Your Password</h2>
                <p>Click the link below to set your password:</p>
                <a href='{resetLink}' style='padding:10px 20px; background:#28a745; color:white; text-decoration:none; border-radius:5px;'>Set Password</a>
                <p>If you didn't request this, ignore this email.</p>";

            await _emailService.SendEmailAsync(email, "Set Your Password - Oversight", emailBody);
        }

        private string GetUserRolesFromToken()
        {
            var identity = HttpContext.User.Identity as ClaimsIdentity;
            if (identity == null) return null;

            var roleClaim = identity.FindFirst(ClaimTypes.Role);
            return roleClaim?.Value;
        }

        private async Task<List<int>> GetAllChildRoleIdsAsync(int parentId)
        {
            var childRoles = await _context.Roles
                .Where(r => r.ParentRoleId == parentId)
                .ToListAsync();

            var childRoleIds = childRoles.Select(r => r.Id).ToList();

            foreach (var child in childRoles)
            {
                var subChildIds = await GetAllChildRoleIdsAsync(child.Id);
                childRoleIds.AddRange(subChildIds);
            }

            return childRoleIds;
        }

        private async Task<List<Role>> GetChildRolesAsync(string roleName)
        {
            var userRole = await _context.Roles.FirstOrDefaultAsync(r => r.Id.ToString() == roleName);
            if (userRole == null) return new List<Role>();

            var childRoleIds = await GetAllChildRoleIdsAsync(userRole.Id);
            return await _context.Roles.Where(r => childRoleIds.Contains(r.Id)).ToListAsync();
        }


        // Recursive function to get all child role IDs
        private async Task<List<int>> GetAllChildRoleIds(int parentId)
        {
            var childRoles = await _context.Roles
                .Where(r => r.ParentRoleId == parentId)
                .ToListAsync();

            var childRoleIds = childRoles.Select(r => r.Id).ToList();

            foreach (var child in childRoles)
            {
                var subChildIds = await GetAllChildRoleIds(child.Id);
                childRoleIds.AddRange(subChildIds);
            }

            return childRoleIds;
        }

        private string GetUserRoleFromToken()
        {
            var identity = HttpContext.User.Identity as ClaimsIdentity;
            if (identity == null) return null;

            var roleClaim = identity.FindFirst(ClaimTypes.Role);  // Extract role name from token
            return roleClaim?.Value;
        }


        // Function to check password strength
        private bool IsValidPassword(string password)
        {
            return password.Length >= 8 &&
                   password.Any(char.IsUpper) &&
                   password.Any(char.IsLower) &&
                   password.Any(char.IsDigit) &&
                   password.Any(ch => "!@#$%^&*()-_=+[]{};:'\",.<>?/\\|".Contains(ch));
        }

        private string Encrypt(string plainText)
        {
            byte[] keyBytes = GetAesKey();
            using (Aes aes = Aes.Create())
            {
                aes.Key = keyBytes;
                aes.Mode = CipherMode.ECB;
                aes.Padding = PaddingMode.PKCS7;
                using (ICryptoTransform encryptor = aes.CreateEncryptor())
                {
                    byte[] inputBytes = Encoding.UTF8.GetBytes(plainText);
                    byte[] encryptedBytes = encryptor.TransformFinalBlock(inputBytes, 0, inputBytes.Length);
                    return Convert.ToBase64String(encryptedBytes);
                }
            }
        }

        private string Decrypt(string encryptedText)
        {
            byte[] keyBytes = GetAesKey();
            using (Aes aes = Aes.Create())
            {
                aes.Key = keyBytes;
                aes.Mode = CipherMode.ECB;
                aes.Padding = PaddingMode.PKCS7;
                using (ICryptoTransform decryptor = aes.CreateDecryptor())
                {
                    byte[] inputBytes = Convert.FromBase64String(encryptedText);
                    byte[] decryptedBytes = decryptor.TransformFinalBlock(inputBytes, 0, inputBytes.Length);
                    return Encoding.UTF8.GetString(decryptedBytes);
                }
            }
        }

        #endregion
    }
}