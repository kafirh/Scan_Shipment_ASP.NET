using System.Diagnostics;
using ShipmentScan.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BCrypt.Net;
using ShipmentScan.Data;
using System.Collections.Generic;

namespace ShipmentScan.Controllers
{
    public class LoginController : Controller
    {
        private readonly ILogger<LoginController> _logger;
        private readonly ApplicationDbContext _context;

        public LoginController(ILogger<LoginController> logger, ApplicationDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        // GET: Halaman Login
        [HttpGet]
        public IActionResult Index()
        {
            // Cek apakah user sudah login
            if (HttpContext.Session.GetString("UserNIK") != null)
            {
                return RedirectToAction("Index", "Home"); // Redirect ke halaman utama jika sudah login
            }
            return View();
        }

        [HttpPost]
        public IActionResult Login(string nik, string password)
        {
            // Validasi input nik dan password
            if (IsInputInvalid(nik, password))
            {
                ViewData["ErrorMessage"] = "Username dan password harus diisi.";
                return View("Index");
            }
            try
            {
                // Mendapatkan user berdasarkan nik
                var user = GetUserByUsername(nik);
                if (user == null)
                {
                    ViewData["ErrorMessage"] = "User tidak ditemukan.";
                    return View("Index");
                }

                // Verifikasi password
                if (!VerifyPassword(password, user.PasswordHash))
                {
                    ViewData["ErrorMessage"] = "Password Salah.";
                    return View("Index");
                }

                // Mendapatkan dan memvalidasi role user
                var roleModel = GetUserRoleAndValidate(user);
                if (roleModel == null)
                {
                    return RedirectToAction("Error", "Home"); // Misalnya menampilkan halaman error
                }

                // Set session data
                SetUserSessionData(user, roleModel);

                // Example: Assuming you have verified the user login.
                HttpContext.Session.SetString("UserName", user.Name); // Store the username in session

                // 🔹 Redirect berdasarkan UserRole
                return roleModel.Name switch
                {
                    "InspectorShipmentFG" => RedirectToAction("Index", "Scan"),
                    _ => RedirectToAction("Index", "Home") // Default jika role tidak dikenali
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Terjadi kesalahan saat login.");
                ViewData["ErrorMessage"] = "Terjadi kesalahan pada sistem. Silakan coba lagi.";
                return View("Index");
            }
        }

        //[HttpPost]
        //public IActionResult Login(string nik, string password)
        //{
        //    // Validasi input nik dan password
        //    if (IsInputInvalid(nik, password))
        //    {
        //        ViewData["ErrorMessage"] = "Username dan password harus diisi.";
        //        return View("Index");
        //    }
        //    try
        //    {
        //        // Mendapatkan user berdasarkan nik
        //        var user = GetUserByUsername(nik);
        //        if (user == null)
        //        {
        //            ViewData["ErrorMessage"] = "User tidak ditemukan.";
        //            return View("Index");
        //        }

        //        // Verifikasi password
        //        if (!VerifyPassword(password, user.PasswordHash))
        //        {
        //            ViewData["ErrorMessage"] = "Password salah.";
        //            return View("Index");
        //        }

        //        // Mendapatkan dan memvalidasi role user
        //        var role = GetUserRoleAndValidate(user);
        //        if (role == null)
        //        {
        //            return RedirectToAction("Error", "Home"); // Misalnya menampilkan halaman error
        //        }

        //        // Set session data
        //        SetUserSessionData(user, role);

        //        return RedirectToAction("Index", "Home");
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Terjadi kesalahan saat login.");
        //        ViewData["ErrorMessage"] = "Terjadi kesalahan pada sistem. Silakan coba lagi.";
        //        return View("Index");
        //    }
        //}

        // Memeriksa apakah input nik atau password kosong
        private bool IsInputInvalid(string nik, string password)
        {
            return string.IsNullOrEmpty(nik) || string.IsNullOrEmpty(password);
        }

        // Memverifikasi password dengan hash yang disimpan
        private bool VerifyPassword(string inputPassword, string storedPasswordHash)
        {
            return BCrypt.Net.BCrypt.Verify(inputPassword, storedPasswordHash);
        }

        // Mendapatkan role user dan memvalidasi keberadaannya
        private RoleModel GetUserRoleAndValidate(UserModel user)
        {
            var userrole = GetUserRole(user.NIK);
            if (userrole == null)
            {
                return null;
            }

            var role = GetRoleById(userrole.RoleId);
            return role;
        }

        // Menyimpan data user dan role ke session
        private void SetUserSessionData(UserModel user, RoleModel role)
        {
            HttpContext.Session.SetString("UserRole", role.Name);
            HttpContext.Session.SetString("UserNIK", user.NIK);
            HttpContext.Session.SetString("UserName", user.Name);

            bool isAdmin = role.Name.Equals("Admin", StringComparison.OrdinalIgnoreCase);
            HttpContext.Session.SetString("IsAdmin", isAdmin.ToString());
        }


        // Fungsi untuk mendapatkan user berdasarkan Username (NIK)
        public UserModel GetUserByUsername(string nik)
        {
            try
            {
                return _context.AspNetUsers
                    .Where(u => u.NIK == nik)
                    .Select(u => new UserModel
                    {
                        NIK = u.NIK,
                        Name = u.Name,
                        PasswordHash = u.PasswordHash
                    })
                    .FirstOrDefault();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error saat mencari user dengan username: {nik}");
                throw; // Opsional: lempar ulang error jika diperlukan
            }
        }

        public RoleModel GetRoleById(string Id)
        {
            try
            {
                return _context.AspNetRoles
                    .Where(u => u.Id == Id)
                    .Select(u => new RoleModel
                    {
                        Id = u.Id,
                        Name = u.Name
                    })
                    .FirstOrDefault();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error saat mencari user dengan username: {Id}");
                throw; // Opsional: lempar ulang error jika diperlukan
            }
        }

        public UserRoleModel GetUserRole(string nik) 
        {
            try
            {
                return _context.AspNetUserRole
                    .Where(u => u.UserNik == nik)
                    .Select(u => new UserRoleModel
                    {
                        UserNik = u.UserNik,
                        RoleId = u.RoleId,
                    })
                    .FirstOrDefault();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error saat mencari user dengan username: {nik}");
                throw; // Opsional: lempar ulang error jika diperlukan
            }
        }

        // Logout Logic
        [HttpPost]
        public IActionResult Logout()
        {
            // Hapus semua data session
            HttpContext.Session.Clear();

            return RedirectToAction("Index");
        }

        // Error Handling
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
