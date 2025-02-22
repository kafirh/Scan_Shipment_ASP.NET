using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace ShipmentScan.Hubs
{
    public class ShipmentHub : Hub
    {
        // Notifikasi ketika ada shipment baru
        public async Task SendNewShipment()
        {
            await Clients.All.SendAsync("ReceiveNewShipment");
        }
        public async Task SendNewSerial()
        {
            await Clients.All.SendAsync("ReceiveNewSerial");
        }
        // Notifikasi ketika ada shipment yang dihapus
        public async Task SendShipmentDeleted(string code)
        {
            await Clients.All.SendAsync("ShipmentDeleted", code);
        }
        public async Task SendSerialDeleted(int id)
        {
            await Clients.All.SendAsync("SerialDeleted", id);
        }
    }
}
