using System.ComponentModel.DataAnnotations;

namespace ShipmentScan.Models
{
    public class UserModel
    {
        required public string NIK { get; set; }
        required public string Name { get; set; }
        required public string PasswordHash { get; set; }

    }
}
