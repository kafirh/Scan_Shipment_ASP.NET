using System.ComponentModel.DataAnnotations;

namespace ShipmentScan.Models
{
    public class DestinationModel
    {
        [Key]
        public int DestinationId { get; set; }
        public required string Name { get; set; }
    }
}
