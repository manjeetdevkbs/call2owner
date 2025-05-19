using Oversight.Model;

namespace Oversight.DTO
{
    public class ChildUsersDTO
    {
        public int UserId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public List<ChildUsersDTO> Children { get; set; } = new();
    }
}
