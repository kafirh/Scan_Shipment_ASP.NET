using Microsoft.EntityFrameworkCore;
using ShipmentScan.Data;
using ShipmentScan.Models;

namespace ShipmentScan.Controllers.Services
{
    public class Service
    {
        private readonly ApplicationDbContext _context;

        public Service(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<int> GetTotallSerialNumberByCode(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                return 0;
            }

            return await _context.ShipmentSerialNums
                .Where(d => d.ShipmentCode == code)
                .CountAsync();
        }
        public async Task<List<SerialNumModel>> GetAllSerialNumberByCode(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                return new List<SerialNumModel>(); // Kembalikan list kosong jika kode kosong
            }

            return await _context.ShipmentSerialNums
                .Where(d => d.ShipmentCode == code)
                .OrderBy(d => d.Date)
                .ToListAsync();
        }
        public async Task<List<string>> GetAllModelNumbers()
        {
            return await _context.GlobalModelCodes
                .AsNoTracking()
                .Select(m => m.ModelNumber.Replace("\r\n", ""))
                .ToListAsync();
        }

        public async Task<DetailsViewModel> GetDetailsViewModel(string code, int no)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                throw new ArgumentException("Shipment code cannot be null or empty.", nameof(code));
            }

            var shipment = await _context.Shipments
                .Include(s => s.Destination)
                .Include(s => s.Model)
                .FirstOrDefaultAsync(s => s.Code == code);

            if (shipment == null)
            {
                return null; // Tidak ada data, kembalikan null
            }

            shipment.TotalSerialNumber = await GetTotallSerialNumberByCode(shipment.Code);
            var serialNumbers = await GetAllSerialNumberByCode(code);
            var users = await ListUser(code);
            var modelNumbers = await GetAllModelNumbers();

            return new DetailsViewModel
            {
                Users = users,
                No = no,
                Shipmentmodel = shipment,
                Serialnummodel = serialNumbers,
                ModelNumbers = modelNumbers
            };
        }

        public async Task<List<UserModel>> ListUser(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                return new List<UserModel>(); // Kembalikan list kosong jika kode kosong
            }

            var users = await _context.ShipmentSerialNums
                .Where(d => d.ShipmentCode == code)
                .Include(d => d.User)
                .Select(d => d.User)
                .Distinct()
                .ToListAsync();

            return users;
        }
    }
}
