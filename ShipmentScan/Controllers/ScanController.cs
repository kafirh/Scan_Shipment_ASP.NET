using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using ShipmentScan.Controllers.Services;
using ShipmentScan.Data;
using ShipmentScan.Hubs;
using ShipmentScan.Models;
using System;
using System.Threading.Tasks;

namespace ShipmentScan.Controllers
{
    public class ScanController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _context;
        private readonly IHubContext<ShipmentHub> _shipmentHub;

        public ScanController(ILogger<HomeController> logger, ApplicationDbContext context, IHubContext<ShipmentHub> shipmentHub)
        {
            _logger = logger;
            _context = context;
            _shipmentHub = shipmentHub;
        }

        public async Task<IActionResult> Index(string searchCode)
        {
            if (HttpContext.Session.GetString("UserNIK") == null)
            {
                return RedirectToAction("Index", "Login");
            }
            var serialnums = new List<SerialNumModel>
{
};

            ViewData["SearchCode"] = searchCode;

            if (string.IsNullOrWhiteSpace(searchCode))
            {
                return View("~/Views/Scanner/Scan.cshtml", new DetailsViewModel { No = 0,Shipmentmodel= new ShipmentModel(),Serialnummodel = serialnums, Users = null }); // Halaman awal kosong
            }

            try
            {
                var service = new Service(_context);
                var viewModel = await service.GetDetailsViewModel(searchCode, 0);

                if (viewModel == null)
                {
                    TempData["ErrorMessage"] = "No shipment details found for the provided code.";
                    return View("~/Views/Scanner/Scan.cshtml", null);
                }

                return View("~/Views/Scanner/Scan.cshtml", viewModel);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"An unexpected error occurred: {ex.Message}";
                return View("~/Views/Scanner/Scan.cshtml", null);
            }
        }

        [HttpPost]
        public async Task<IActionResult> PostSerialNumber(string serialNum, string shipmentCode, string modelCode)
        {
            if (string.IsNullOrWhiteSpace(serialNum) || string.IsNullOrWhiteSpace(shipmentCode) || string.IsNullOrWhiteSpace(modelCode))
            {
                TempData["ErrorMessage"] = "Serial number, shipment code, and model code are required.";
                return RedirectToAction("Index"); // Redirect tanpa searchCode
            }

            try
            {
                bool serialExists = await _context.ShipmentSerialNums
                    .AnyAsync(s => s.SerialNumber == serialNum && s.ModelCode == modelCode);

                if (serialExists)
                {
                    TempData["ErrorMessage"] = "Serial number already exists for this model.";
                    //ModelState.AddModelError("serialNum", "Serial number already exists for this model");
                    return RedirectToAction("Index", new { searchCode = shipmentCode });
                }

                var modelserial = new SerialNumModel
                {
                    SerialNumber = serialNum,
                    ShipmentCode = shipmentCode,
                    ModelCode = modelCode,
                    Date = DateTime.Now,
                    UserNIK = HttpContext.Session.GetString("UserNIK"),
                    ItemCheck_1 = true,
                    ItemCheck_2 = true,
                    ItemCheck_3 = true,
                    ItemCheck_4 = true,
                    Comment = null,
                };

                _context.ShipmentSerialNums.Add(modelserial);
                await _context.SaveChangesAsync();
                // 🔥 Kirim notifikasi ke semua user bahwa ada serial baru
                await _shipmentHub.Clients.All.SendAsync("ReceiveNewSerial");

                TempData["SuccessMessage"] = "Serial number successfully added.";
                return RedirectToAction("Index", new { searchCode = shipmentCode });
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"An error occurred: {ex.Message}";
                return RedirectToAction("Index", new { searchCode = shipmentCode });
            }
        }

        [HttpPost]
        public async Task<IActionResult> UpdateSerialNumber(int id, bool ItemCheck_1, bool ItemCheck_2, bool ItemCheck_3, bool ItemCheck_4, string Comment)
        {
            try
            {
                var existingSerial = await _context.ShipmentSerialNums.FindAsync(id);
                if (existingSerial == null)
                {
                    TempData["ErrorMessage"] = "Serial number not found.";
                    return RedirectToAction("Index");
                }

                // Update data dari form
                existingSerial.ItemCheck_1 = ItemCheck_1;
                existingSerial.ItemCheck_2 = ItemCheck_2;
                existingSerial.ItemCheck_3 = ItemCheck_3;
                existingSerial.ItemCheck_4 = ItemCheck_4;
                existingSerial.Comment = Comment;

                await _context.SaveChangesAsync();
                // 🔥 Kirim notifikasi ke semua user bahwa ada serial baru
                await _shipmentHub.Clients.All.SendAsync("ReceiveNewSerial");

                TempData["SuccessMessage"] = "Serial number updated successfully.";
                return RedirectToAction("Index", new { searchCode = existingSerial.ShipmentCode });
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"An error occurred: {ex.Message}";
                return RedirectToAction("Index");
            }
        }

    }
}
