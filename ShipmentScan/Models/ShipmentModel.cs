using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ShipmentScan.Models
{
    public class ShipmentModel
    {
        [Key]
        public string? Code { get; set; }
        public DateTime CreatedDate { get; set; }

        [Required(ErrorMessage = "Masukan Nomor Model Product")]
        public string? ModelCode { get; set; } // Dropdown untuk Model
        public  int DestinationId { get; set; } // Dropdown untuk Destination

        [Required(ErrorMessage = "Masukan Qty dibutuhkan")]
        [Range(1, int.MaxValue, ErrorMessage = "Qty tidak boleh kosong")]
        public int Qty { get; set; }

        [Required(ErrorMessage = "Nomor Container dibutuhkan")]
        public string? ContainerNo { get; set; }

        [Required(ErrorMessage = "Nomor Seal dibutuhkan")]
        public string? SealNo { get; set; }

        [Required(ErrorMessage = "Nomor Plat Kendaraan dibutuhkan")]
        public string? TruckPoliceNo { get; set; }
        public bool IsView {  get; set; }
        public bool IsExport { get; set; }

        [Required(ErrorMessage ="Nomor Booking Confirmation dibutuhkan")]
        public string BookingConfirmation { get; set; }
        public int? ContainerSizeId { get; set; }
        public string? ContainerQ { get; set; }
        public string? AWB { get; set; }

        [Required(ErrorMessage = "Nomor SO dibutuhkan")]
        public string SoNum { get; set; }

        [Required(ErrorMessage = "Nomor Invoice dibutuhkan")]
        public string NoInvoice { get; set; }
        public DateTime PlanDate { get; set; }
        [NotMapped]
        public int? TotalSerialNumber { get; set; }

        public DestinationModel? Destination { get; set; }
        public ModelCodeModel? Model { get; set; }
        public ContainerSizeModel? ContainerSizeModel { get; set; }
    }
}
