namespace Oversight.DTO
{
    public class UsersDtoOutput
    {
        public int Id { get; set; }
        public int userId { get; set; }
        public string FirstName { get; set; } = null!;
        public string LastName { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string MobileNumber { get; set; } = null!;
        public int RoleId { get; set; }
        public bool? IsActive { get; set; }
        public bool? IsVerified { get; set; }
    }
}
