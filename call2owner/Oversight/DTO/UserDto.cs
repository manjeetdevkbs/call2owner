using System.ComponentModel.DataAnnotations;

namespace Oversight.DTO
{
    public class UserDto
    {
        public int Id { get; set; }
        [Required]
        public string FirstName { get; set; }

        public string LastName { get; set; }

        [Required]
        public string Email { get; set; }
        public string? MobileNumber { get; set; }
        public int RoleId { get; set; }
    }
}
