namespace ShipmentScan.Models
{
    public class DetailsViewModel
    {
        public int No { get; set; }
        public List<UserModel>? Users { get; set; }
        public ShipmentModel? Shipmentmodel { get; set; }
        public List<SerialNumModel>? Serialnummodel { get; set; }
        public List<string>? ModelNumbers { get; set; }

    }
}
