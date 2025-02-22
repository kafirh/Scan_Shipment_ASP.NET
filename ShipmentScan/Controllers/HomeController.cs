using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ShipmentScan.Data;
using ShipmentScan.Models;
using Rotativa.AspNetCore;
using ShipmentScan.Controllers.Services;
using Microsoft.AspNetCore.SignalR;
using ShipmentScan.Hubs;

namespace ShipmentScan.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _context;
        private readonly IHubContext<ShipmentHub> _shipmentHub;

        public HomeController(ILogger<HomeController> logger, ApplicationDbContext context, IHubContext<ShipmentHub> shipmentHub)
        {
            _logger = logger;
            _context = context;
            _shipmentHub = shipmentHub;
        }

        public IActionResult AccessDenied()
        {
             return View();
        }

       private async Task<List<ShipmentModel>> GetShipmentsWithTotalSerialNumbers(DateTime filterDateStart,DateTime filterDateEnd, bool? filterType, string? filterModel)
{
    // Query dasar
    var query = _context.Shipments
        .Where(d => d.PlanDate.Date >= filterDateStart.Date && d.PlanDate.Date <= filterDateEnd.Date && d.IsView);

            // Tambahkan filter ModelCode jika tersedia
            if (!string.IsNullOrEmpty(filterModel))
    {
        query = query.Where(i => i.ModelCode == filterModel);
    }

    // Tambahkan filter tipe pengiriman jika ada
    if (filterType.HasValue)
    {
        query = query.Where(i => i.IsExport == filterType.Value);
    }

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
        public async Task<IActionResult> Index(DateTime? selectedDateStart, DateTime? selectedDateEnd, bool? selectedType, string? selectedModel)
        {
            // Redirect ke Login jika UserNIK belum diset
            if (HttpContext.Session.GetString("UserNIK") == null)
            {
                return RedirectToAction("Index", "Login");
            }

            // Default ke hari ini jika selectedDate null
            var filterDateStart = selectedDateStart ?? DateTime.Now.Date;
            var filterDateEnd = selectedDateEnd ?? DateTime.Now.Date;

            // Simpan nilai filter ke ViewData untuk dipakai di dropdown
            ViewData["SelectedModel"] = selectedModel;
            ViewData["SelectedType"] = selectedType;
            ViewData["SelectedDateStart"] = filterDateStart.ToString("yyyy-MM-dd");
            ViewData["SelectedDateEnd"] = filterDateEnd.ToString("yyyy-MM-dd");

            // Ambil daftar model dari database, bersihkan karakter "-" pada ModelNumber
            var models = await _context.GlobalModelCodes
                .Select(m => new {
                    ModelCodeId = m.ModelCodeId.ToString(), // Pastikan sebagai string
                    ModelNumber = m.ModelNumber
                })
                .OrderBy(m => m.ModelNumber)
                .ToListAsync();

            // Simpan daftar model ke ViewBag untuk digunakan dalam dropdown
            ViewBag.Models = models.Any()
                ? new SelectList(models, "ModelCodeId", "ModelNumber")
                : new SelectList(Enumerable.Empty<SelectListItem>(), "Value", "Text", "-- Select Model --");

            // Ambil data Shipments dengan TotalSerialNumber
            var shipments = await GetShipmentsWithTotalSerialNumbers(filterDateStart,filterDateEnd, selectedType, selectedModel);

            return View(shipments);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string code)
        {
            if (string.IsNullOrEmpty(code))
            {
                TempData["ErrorMessage"] = "Invalid request. Shipment code is missing.";
                return RedirectToAction("Index");
            }

            var shipment = await _context.Shipments.FirstOrDefaultAsync(s => s.Code == code);
            if (shipment == null)
            {
                TempData["ErrorMessage"] = "Shipment not found.";
                return RedirectToAction("Index");
            }

            // Ubah nilai IsView menjadi false
            shipment.IsView = false;

            try
            {
                await _context.SaveChangesAsync();
                await _shipmentHub.Clients.All.SendAsync("ShipmentDeleted", code);
                TempData["SuccessMessage"] = $"Shipment with code {code} has been successfully marked as deleted.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking shipment as deleted.");
                TempData["ErrorMessage"] = "An error occurred while deleting the shipment.";
            }

            return RedirectToAction("Index");
        }


        // GET: Home/Privacy
        public IActionResult Privacy()
        {
            return View();
        }

        // GET: Home/Error
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        public async Task<IActionResult> DownloadPdf(string code,int no)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                return BadRequest("Shipment code is required.");
            }

            var service = new Service(_context);
            var viewModel = await service.GetDetailsViewModel(code,no);

            // Pastikan View yang digunakan dirancang khusus untuk PDF (gunakan View terpisah jika perlu)
            var pdf = new ViewAsPdf("~/Views/Home/DetailsPdf.cshtml", viewModel)
            {
                FileName = "ShipmentReport.pdf",
                PageSize = Rotativa.AspNetCore.Options.Size.A4,
                PageOrientation = Rotativa.AspNetCore.Options.Orientation.Portrait,
                PageMargins = new Rotativa.AspNetCore.Options.Margins(10, 10, 10, 10), // Margin kiri, kanan, atas, bawah
                CustomSwitches = string.Join(" ", new[]
                {
            "--footer-center \"Page [page] of [toPage]\"",
            "--footer-font-size \"10\"",
            "--footer-line", // Garis di header/footer
            "--header-spacing \"5\"", // Jarak antara header dan konten
            "--footer-spacing \"5\"", // Jarak antara footer dan konten
        })
            };

            return pdf;
        }

    }
}