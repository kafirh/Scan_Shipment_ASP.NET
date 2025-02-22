using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using ShipmentScan.Data;
using ShipmentScan.Hubs;
using ShipmentScan.Models;
using System.Diagnostics;

namespace ShipmentScan.Controllers
{
    public class FormController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _context;
        private readonly IHubContext<ShipmentHub> _shipmentHub;

        public FormController(ILogger<HomeController> logger, ApplicationDbContext context, IHubContext<ShipmentHub> shipmentHub)
        {
            _logger = logger;
            _context = context;
            _shipmentHub = shipmentHub;
        }

        // GET: Form
        public async Task<IActionResult> Form(string code)
        {
            // Redirect ke Login jika UserNIK belum diset
            if (HttpContext.Session.GetString("UserNIK") == null)
            {
                return RedirectToAction("Index", "Login");
            }

            // Muat data untuk dropdown
            await LoadDropdownData();

            if (string.IsNullOrEmpty(code))
            {
                return View("~/Views/Home/Form.cshtml", new ShipmentModel());
            }

            // Ambil data Shipment untuk Edit
            var shipment = await _context.Shipments
                .Include(d => d.Destination)
                .Include(d => d.Model)
                .FirstOrDefaultAsync(d => d.Code == code);

            if (shipment == null)
            {
                return NotFound();
            }

            return View("~/Views/Home/Form.cshtml", shipment);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Save(ShipmentModel shipment)
        {
            // Muat ulang data dropdown
            await LoadDropdownData();

            // Validasi ModelState
            if (!ModelState.IsValid)
            {
                return View("~/Views/Home/Form.cshtml", shipment); // Tampilkan form dengan error validasi
            }

            // Validasi DestinationId dan ModelCode
            var isValidDestination = await _context.Destinations.AnyAsync(c => c.DestinationId == shipment.DestinationId);
            if (!isValidDestination)
            {
                ModelState.AddModelError("DestinationId", "Destination yang dipilih tidak valid.");
                return View("~/Views/Home/Form.cshtml", shipment);
            }

            var isValidModel = await _context.GlobalModelCodes.AnyAsync(c => c.ModelCodeId == shipment.ModelCode);
            if (!isValidModel)
            {
                ModelState.AddModelError("ModelCode", "Model yang dipilih tidak valid.");
                return View("~/Views/Home/Form.cshtml", shipment);
            }
            var isValidContainerSize = await _context.ContainerSizes.AnyAsync(c => c.Id == shipment.ContainerSizeId);
            if (!isValidContainerSize) 
            {
                ModelState.AddModelError("ContainerSizeId", "ContainerSize yang dipilih tidak valid.");
                return View("~/Views/Home/Form.cshtml", shipment);
            }

            // Proses Tambah atau Update
            if (shipment.Code == "0")
            {
                // Tambah Data Baru
                shipment.Code = GenerateUniqueCode(shipment.IsExport); // Generate kode unik
                _context.Shipments.Add(shipment);
            }
            else
            {
                // Update Data yang Sudah Ada
                var existingShipment = await _context.Shipments.FirstOrDefaultAsync(s => s.Code == shipment.Code);
                if (existingShipment == null)
                {
                    return NotFound(); // Jika data tidak ditemukan
                }

                try
                {
                    _context.Entry(existingShipment).CurrentValues.SetValues(shipment);
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ShipmentExists(shipment.Code))
                    {
                        return NotFound();
                    }
                    throw;
                }
            }

            // Simpan Perubahan ke Database
            await _context.SaveChangesAsync();
            // 🔥 Kirim notifikasi ke semua user bahwa ada shipment baru
            await _shipmentHub.Clients.All.SendAsync("ReceiveNewShipment");
            return RedirectToAction(nameof(Index), "Home");
        }
        private string GenerateUniqueCode(bool isExport)
        {
            string code;
            do
            {
                // Generate kode baru
                code = GenerateCode(isExport);
            }
            while (IsCodeExists(code)); // Ulangi jika kode sudah ada di database

            return code;
        }

        private bool IsCodeExists(string code)
        {
            // Periksa apakah kode sudah ada di database
            return _context.Shipments.Any(s => s.Code == code);
        }

        // Method untuk generate kode random
        private string GenerateCode(bool isExport)
        {
            // Ambil tanggal saat ini dalam format ddMMyy
            string datePart = DateTime.Now.ToString("ddMMyy");

            // Tentukan karakter ke-7 berdasarkan jenis ekspor/domestik
            char typeChar = isExport ? 'E' : 'D';

            // Generate 5 karakter acak
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var random = new Random();
            string randomPart = new string(Enumerable.Repeat(chars, 5)
                .Select(s => s[random.Next(s.Length)]).ToArray());

            // Gabungkan semuanya
            return $"{datePart}{typeChar}{randomPart}";
        }


        // Method untuk memuat data dropdown
        private async Task LoadDropdownData()
        {
            var containerSizes = await _context.ContainerSizes
                .Select(c => new { c.Id, Name = c.Name.Trim() })
                .ToListAsync();
            var destinations = await _context.Destinations
                .Select(d => new { d.DestinationId, Name = d.Name.Trim() }) // Membersihkan data dari spasi tambahan
                .ToListAsync();

            var models = await _context.GlobalModelCodes
                //.Select(m => new { m.ModelCodeId, ModelNumber = m.ModelNumber.Replace("-", "").Trim() }) // Bersihkan karakter "-"
                .Select(m => new { m.ModelCodeId, ModelNumber = m.ModelNumber }) 
                .OrderBy(m => m.ModelNumber)
                .ToListAsync();

            ViewBag.ContainerSizes = containerSizes.Any()
                ? new SelectList(containerSizes,"Id","Name")
                : new SelectList(Enumerable.Empty<SelectListItem>(),"Value","Text", "-- Pilih Container --");

            ViewBag.Destinations = destinations.Any()
                ? new SelectList(destinations, "DestinationId", "Name")
                : new SelectList(Enumerable.Empty<SelectListItem>(), "Value", "Text", "-- Pilih Destination --");

            ViewBag.Models = models.Any()
                ? new SelectList(models, "ModelCodeId", "ModelNumber")
                : new SelectList(Enumerable.Empty<SelectListItem>(), "Value", "Text", "-- Pilih Model --");
        }

        // Cek apakah Shipment dengan Code tertentu ada
        private bool ShipmentExists(string code)
        {
            return _context.Shipments.Any(e => e.Code == code);
        }
    }
}
