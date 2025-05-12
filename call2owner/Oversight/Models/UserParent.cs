using Oversight.Model;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Oversight.Models
{
    [Table("UserParent")]
    public class UserParent : BaseModel
    {
        [Key]
        public int Id { get; set; }
        public int UserId { get; set; }
        public int ParentId { get; set; }
        [JsonIgnore]
        public virtual User Users { get; set; } = null!;
    }
}
