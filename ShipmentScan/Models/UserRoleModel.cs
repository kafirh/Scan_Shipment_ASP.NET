using System.ComponentModel.DataAnnotations;

namespace ShipmentScan.Models
{
    public class UserRoleModel
    {
        [Key]
        public int Id { get; set; }
        public string UserNik { get; set; }
        public string RoleId { get; set; }

        public RoleModel Role { get; set; }
        public UserModel User { get; set; }
    }
}
