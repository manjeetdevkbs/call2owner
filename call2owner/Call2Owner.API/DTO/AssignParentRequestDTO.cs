namespace Oversight.DTO
{
    public class AssignParentRequestDTO
    {
        public List<int>? UserIds { get; set; }
        public int ParentId { get; set; }
    }
}
