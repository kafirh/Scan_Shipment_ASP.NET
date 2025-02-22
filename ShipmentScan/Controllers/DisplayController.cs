using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using ShipmentScan.Data;
using ShipmentScan.Hubs;
using ShipmentScan.Models;

namespace ShipmentScan.Controllers
{
    public class DisplayController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _context;
        private readonly IHubContext<ShipmentHub> _shipmentHub;

        public DisplayController(ILogger<HomeController> logger, ApplicationDbContext context, IHubContext<ShipmentHub> shipmentHub)
        {
            _logger = logger;
            _context = context;
            _shipmentHub = shipmentHub;
        }
        private async Task<List<ShipmentModel>> GetShipmentsWithTotalSerialNumbers(DateTime filterDateStart, DateTime filterDateEnd)
        {
            // Query dasar
            var query = _context.Shipments
                .Where(d => d.PlanDate.Date >= filterDateStart.Date && d.PlanDate.Date <= filterDateEnd.Date && d.IsView);

            // Eksekusi query untuk mendapatkan daftar pengiriman
            var shipments = await query
                .OrderBy(d => d.CreatedDate)
                .Include(d => d.Destination)
                .Include(d => d.Model)
                .ToListAsync();

            // ?? **BATCH QUERY untuk mendapatkan semua TotalSerialNumber sekaligus**
            var shipmentCodes = shipments.Select(s => s.Code).ToList();

            var serialNumbers = await _context.ShipmentSerialNums
                .Where(sn => shipmentCodes.Contains(sn.ShipmentCode))
                .GroupBy(sn => sn.ShipmentCode)
                .Select(g => new { Code = g.Key, Total = g.Count() }) // Hitung total serial number per shipment
                .ToDictionaryAsync(g => g.Code, g => g.Total);

            // ?? **Update setiap shipment tanpa harus query satu per satu**
            foreach (var shipment in shipments)
            {
                shipment.TotalSerialNumber = serialNumbers.ContainsKey(shipment.Code) ? serialNumbers[shipment.Code] : 0;
            }

            return shipments;
        }



        // GET: Home/Index
        public async Task<IActionResult> Index()
        {
            var filterDateStart =  DateTime.Now.Date;
            var filterDateEnd =  DateTime.Now.Date;

            var shipments = await GetShipmentsWithTotalSerialNumbers(filterDateStart, filterDateEnd);

            return View("~/Views/Display/Index.cshtml", shipments);
        }
    }
}
