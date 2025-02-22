using System.ComponentModel.DataAnnotations;

namespace ShipmentScan.Models
{
    public class ModelCodeModel
    {
        [Key]
        required public string ModelCodeId { get; set; }
        public required string ModelNumber { get; set; }
        public string? GlobalCodeId { get; set; }
        public string? Register { get; set; }
        public TimeSpan? CycleTime { get; set; }
    }
}
