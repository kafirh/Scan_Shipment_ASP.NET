namespace ShipmentScan.Models
{
    public class SerialNumModel
    {
        public int Id { get; set; }
        public string? SerialNumber { get; set; }
        public string? ShipmentCode { get; set; }
        public string? ModelCode { get; set; }
        public DateTime Date { get; set; }
        public string UserNIK { get; set; }
        public bool ItemCheck_1 { get; set; } 
        public bool ItemCheck_2 { get; set; }
        public bool ItemCheck_3 { get; set; }
        public bool ItemCheck_4 { get; set; }
        public string? Comment { get; set; }
        public UserModel? User { get; set; }
    }
}
