namespace Oversight.DTO
{
    public class InsurerUserDTO
    {
        public int UserId { get; set; }
        public int InsurerId { get; set; }
        public bool IsActive { get; set; }
        public bool IsDeleted { get; set; }
    }
}
