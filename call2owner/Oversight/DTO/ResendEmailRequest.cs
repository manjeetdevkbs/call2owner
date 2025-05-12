using System.ComponentModel.DataAnnotations;

namespace Oversight.DTO
{
    public class ResendEmailRequest
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }
    }
}
