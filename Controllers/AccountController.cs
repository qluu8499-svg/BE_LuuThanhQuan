using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using BE4_LUUTHANHQUAN.Models;
using BE4_LUUTHANHQUAN.Models.ViewModel;

namespace BE4_LUUTHANHQUAN.Controllers
{
    public class AccountController : Controller
    {
        private readonly MyStoreEntities db = new MyStoreEntities();

        public ActionResult Register() { return View("~/Views/Home/Register.cshtml"); }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Register(RegisterVM model)
        {
            if (!ModelState.IsValid) return View("~/Views/Home/Register.cshtml", model);
            if (db.Users.Any(u => u.Username == model.Username))
            {
                ModelState.AddModelError("Username", "Tên đăng nhập này đã tồn tại!");
                return View("~/Views/Home/Register.cshtml", model);
            }
            using (var tx = db.Database.BeginTransaction())
            {
                try
                {
                    db.Users.Add(new User { Username = model.Username.Trim(), Password = HashPassword(model.Password), UserRole = "C" });
                    db.Customers.Add(new Customer { CustomerName = model.CustomerName, CustomerEmail = model.CustomerEmail, CustomerPhone = model.CustomerPhone, CustomerAddress = model.CustomerAddress, Username = model.Username.Trim() });
                    db.SaveChanges(); tx.Commit();
                }
                catch { tx.Rollback(); ModelState.AddModelError("", "Không thể tạo tài khoản. Vui lòng thử lại."); return View("~/Views/Home/Register.cshtml", model); }
            }
            TempData["Message"] = "Đăng ký thành công. Vui lòng đăng nhập.";
            return RedirectToAction("Login");
        }

        public ActionResult Login(string returnUrl) { return View(new LoginVM { ReturnUrl = returnUrl }); }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Login(LoginVM model)
        {
            if (!ModelState.IsValid) return View(model);
            var user = db.Users.SingleOrDefault(u => u.Username == model.Username);
            if (user == null || !VerifyPassword(model.Password, user.Password))
            {
                ModelState.AddModelError("", "Tên đăng nhập hoặc mật khẩu không đúng."); return View(model);
            }
            // Nâng cấp mật khẩu cũ đang lưu dạng văn bản thuần sau lần đăng nhập hợp lệ.
            if (!user.Password.Trim().StartsWith("P$")) { user.Password = HashPassword(model.Password); db.SaveChanges(); }
            var ticket = new FormsAuthenticationTicket(1, user.Username, DateTime.Now, DateTime.Now.AddHours(8), model.RememberMe, user.UserRole ?? "Customer");
            var cookie = new HttpCookie(FormsAuthentication.FormsCookieName, FormsAuthentication.Encrypt(ticket)) { HttpOnly = true };
            if (model.RememberMe) cookie.Expires = ticket.Expiration;
            Response.Cookies.Add(cookie);
            if (!String.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl)) return Redirect(model.ReturnUrl);
            return RedirectToAction("Index", "Home");
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Logout() { FormsAuthentication.SignOut(); Session.Clear(); return RedirectToAction("Index", "Home"); }

        private static string HashPassword(string password)
        {
            var salt = new byte[8]; using (var rng = RandomNumberGenerator.Create()) rng.GetBytes(salt);
            using (var derive = new Rfc2898DeriveBytes(password, salt, 100000))
                return "P$" + Convert.ToBase64String(salt) + "$" + Convert.ToBase64String(derive.GetBytes(24));
        }
        private static bool VerifyPassword(string password, string stored)
        {
            if (String.IsNullOrEmpty(stored)) return false;
            stored = stored.Trim();
            if (!stored.StartsWith("P$")) return String.Equals(password, stored, StringComparison.Ordinal);
            var parts = stored.Split('$'); if (parts.Length != 3) return false;
            try { using (var derive = new Rfc2898DeriveBytes(password, Convert.FromBase64String(parts[1]), 100000)) return FixedEquals(derive.GetBytes(24), Convert.FromBase64String(parts[2])); }
            catch { return false; }
        }
        private static bool FixedEquals(byte[] a, byte[] b) { if (a == null || b == null || a.Length != b.Length) return false; int diff = 0; for (int i = 0; i < a.Length; i++) diff |= a[i] ^ b[i]; return diff == 0; }
        protected override void Dispose(bool disposing) { if (disposing) db.Dispose(); base.Dispose(disposing); }
    }
}
