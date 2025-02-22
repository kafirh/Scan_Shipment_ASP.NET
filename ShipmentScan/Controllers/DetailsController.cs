using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShipmentScan.Data;
using ShipmentScan.Models;
using Rotativa.AspNetCore;
using System.Threading.Tasks;
using ShipmentScan.Controllers.Services;
using ShipmentScan.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace ShipmentScan.Controllers
{
    public class DetailsController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _context;
        private readonly IHubContext<ShipmentHub> _shipmentHub;

        public DetailsController(ILogger<HomeController> logger, ApplicationDbContext context, IHubContext<ShipmentHub> shipmentHub)
        {
            _logger = logger;
            _context = context;
            _shipmentHub = shipmentHub;
        }

        // GET: Detail
        public async Task<IActionResult> Details(string code, int no)
        {
            if (HttpContext.Session.GetString("UserNIK") == null)
            {
                return RedirectToAction("Index", "Login");
            }
            if (string.IsNullOrWhiteSpace(code))
            {
                return BadRequest("Shipment code is required.");
            }
            var service = new Service(_context);
            var viewModel = await service.GetDetailsViewModel(code, no);

            return View("~/Views/Home/details.cshtml", viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> DeleteSerial(int id, string code, int no)
        {
            if (id == 0)
            {
                TempData["ErrorMessage"] = "Invalid request. Serial ID is missing.";
                return RedirectToAction("Details", new { code, no });
            }

            var serialNum = await _context.ShipmentSerialNums.FirstOrDefaultAsync(s => s.Id == id);
            if (serialNum == null)
            {
                TempData["ErrorMessage"] = "Serial not found.";
                return RedirectToAction("Details", new { code, no });
            }

            _context.ShipmentSerialNums.Remove(serialNum);
            try
            {
                await _context.SaveChangesAsync();
                await _shipmentHub.Clients.All.SendAsync("SerialDeleted", id);
                TempData["SuccessMessage"] = $"Serial with ID {id} has been successfully deleted.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting serial.");
                TempData["ErrorMessage"] = "An error occurred while deleting the serial.";
            }

            // Mengarahkan kembali ke halaman Details dengan parameter yang sesuai
            return RedirectToAction("Details", new { code, no });
        }
    }
}
