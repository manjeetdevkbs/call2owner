namespace Oversight.Models
{
    public class BaseModel
    {
        public int? CreatedBy { get; set; }
        public int? ModifiedBy { get; set; }
        public bool? IsVerified { get; set; }
        public bool? IsDeleted { get; set; }
        public bool? IsActive { get; set; }
    }
}
