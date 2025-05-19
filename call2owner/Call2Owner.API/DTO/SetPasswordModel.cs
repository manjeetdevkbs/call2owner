using System.ComponentModel.DataAnnotations;

namespace Oversight.DTO
{
    public class SetPasswordModel
    {
        [Required]
        public string EncryptedToken { get; set; }

        [Required]
        //[MinLength(8, ErrorMessage = "Password must be at least 8 characters long.")]
        //[RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{8,}$",
        //    ErrorMessage = "Password must contain at least one uppercase letter, one lowercase letter, one number, and one special character.")]
        public string EncryptedPassword { get; set; }
    }
}
