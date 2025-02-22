using System.ComponentModel.DataAnnotations;

namespace ShipmentScan.Models
{
    public class RoleModel
    {
        [Key]
        public string Id { get; set; }
        required public string Name { get; set; }
    }
}
