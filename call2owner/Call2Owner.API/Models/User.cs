using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Newtonsoft.Json;
using Oversight.Models;

namespace Oversight.Model
{
    public partial class User : BaseModel
    {
        public int Id { get; set; }
        public string? FirstName { get; set; } = null!;
        public string? LastName { get; set; } = null!;
        public string? Email { get; set; } = null!;
        public string MobileNumber { get; set; }
        public string? PasswordHash { get; set; }
        public int RoleId { get; set; }
        public string? VerificationCode { get; set; }
        public DateTime? VerificationCodeGenerationTime { get; set; }
        public DateTime? VerificationCodeValidationTime { get; set; }

        [MaxLength(10)]
        public string? OTP { get; set; }
        public DateTime? OtpExpireTime { get; set; }
        public DateTime? ResendOtpTime { get; set; }
        public DateTime? OtpValidatedOn { get; set; }

        [MaxLength(1000)]
        public string? RefreshToken { get; set; }
        public DateTime? RefreshTokenExpireTime { get; set; }
        public bool? IsActive { get; set; }
        public bool? IsVerified { get; set; }
        public bool? IsDeleted { get; set; }
        [JsonProperty("resetLink")]
        [NotMapped]
        public string? resetLink { get; set; }

        [JsonIgnore]
        [NotMapped]
        public int? ParentRoleId { get; set; }

        public virtual Role Role { get; set; } = null!;
    }
}
