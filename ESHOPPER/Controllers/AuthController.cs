using BCrypt.Net;
//using ESHOPPER.Helpers;
using ESHOPPER.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity.Core.Objects;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;

namespace ESHOPPER.Controllers.Auth
{
    public class AuthController : Controller
    {
        private QlyFashionShopEntities db = new QlyFashionShopEntities();

        // ====================================================
        // 1. ĐĂNG NHẬP
        // ====================================================
        public ActionResult Login()
        {
            // Nếu đã đăng nhập, chuyển hướng về trang chủ
            if (Session["MaKH"] != null || Session["UserID"] != null)
            {
                return RedirectToAction("Index", "Home");
            }
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Login(string loginInput, string password, string returnUrl = "")
        {
            try
            {
                // 1. Validate đầu vào
                if (string.IsNullOrEmpty(loginInput) || string.IsNullOrEmpty(password))
                {
                    ModelState.AddModelError("", "Vui lòng nhập đầy đủ thông tin!");
                    return View();
                }

                // 2. Tìm User
                var user = db.Users.FirstOrDefault(u => u.Name == loginInput || u.Email == loginInput);
                if (user == null)
                {
                    ModelState.AddModelError("", "Tài khoản không tồn tại!");
                    return View();
                }

                // 3. Kiểm tra Password (BCrypt)
                bool isPasswordValid = false;
                try
                {
                    isPasswordValid = BCrypt.Net.BCrypt.Verify(password, user.PasswordHash);
                }
                catch { isPasswordValid = false; }

                if (!isPasswordValid)
                {
                    ModelState.AddModelError("", "Mật khẩu không đúng!");
                    return View();
                }

                // ====================================================
                // 4. ĐĂNG NHẬP THÀNH CÔNG -> SETUP SESSION
                // ====================================================
                int maKH = 0;
                var khachHang = db.KhachHangs.FirstOrDefault(k => k.UserId == user.UserID);
                if (khachHang != null) maKH = khachHang.MaKH;

                Session["UserID"] = user.UserID;
                Session["UserName"] = user.Name;
                Session["UserRole"] = user.Role;
                Session["MaKH"] = maKH;

                var ticket = new FormsAuthenticationTicket(
                    1,
                    user.Name,                 // username
                    DateTime.Now,
                    DateTime.Now.AddHours(3),  // thời hạn login
                    false,
                    user.Role                  // 👈 ROLE ĐƯA VÀO USERDATA
                );

                string encryptedTicket = FormsAuthentication.Encrypt(ticket);

                var cookie = new HttpCookie(
                    FormsAuthentication.FormsCookieName,
                    encryptedTicket
                );

                Response.Cookies.Add(cookie);


                // ====================================================
                // D. [QUAN TRỌNG] ĐỒNG BỘ GIỎ HÀNG (SESSION -> DB)
                // ====================================================
                // Logic: Nếu là Khách Hàng và trong Session đang có hàng -> Đổ vào DB
                var sessionCart = Session["Cart"] as List<ChiTietGioHang>;

                if (maKH > 0 && sessionCart != null && sessionCart.Count > 0)
                {
                    // B1: Tìm hoặc Tạo giỏ hàng trong DB
                    var dbCart = db.GioHangs.FirstOrDefault(g => g.MaKH == maKH);
                    if (dbCart == null)
                    {
                        dbCart = new GioHang { MaKH = maKH, NgayTao = DateTime.Now };
                        db.GioHangs.Add(dbCart);
                        db.SaveChanges(); // Lưu ngay để lấy MaGioHang
                    }

                    // B2: Duyệt từng món trong Session để đưa vào DB
                    foreach (var itemSess in sessionCart)
                    {
                        // Kiểm tra xem món này đã có trong DB chưa
                        var dbItem = db.ChiTietGioHangs.FirstOrDefault(c =>
                            c.MaGioHang == dbCart.MaGioHang &&
                            c.MaBienThe == itemSess.MaBienThe);

                        if (dbItem != null)
                        {
                            // Đã có -> Cộng dồn số lượng
                            dbItem.SoLuong += itemSess.SoLuong;
                            // dbItem.DonGia = itemSess.DonGia; // (Tuỳ chọn: cập nhật giá mới nhất)
                        }
                        else
                        {
                            // Chưa có -> Tạo mới (Lưu ý: Phải new object mới, không dùng lại object session)
                            var newItem = new ChiTietGioHang
                            {
                                MaGioHang = dbCart.MaGioHang,
                                MaBienThe = itemSess.MaBienThe,
                                SoLuong = itemSess.SoLuong,
                                DonGia = itemSess.DonGia
                            };
                            db.ChiTietGioHangs.Add(newItem);
                        }
                    }

                    // B3: Lưu DB và Xóa Session
                    db.SaveChanges();
                    Session["Cart"] = null;
                }

                // Cập nhật lại số lượng hiển thị trên menu (Badge)
                // Gọi hàm GetCartTotalItems() mà bạn đã viết ở bước trước
                // Tính trực tiếp từ DB luôn vì lúc này đã là User rồi
                var gioHangUser = db.GioHangs.FirstOrDefault(g => g.MaKH == maKH);
                Session["CartCount"] = gioHangUser != null ? (gioHangUser.ChiTietGioHangs.Sum(c => c.SoLuong) ?? 0) : 0;

                // ====================================================
                // 5. ĐIỀU HƯỚNG
                // ====================================================
                return RedirectToLocal(returnUrl, user.Role);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Lỗi đăng nhập: " + ex.Message);
                return View();
            }
        }

        // ====================================================
        // HELPER: Hàm điều hướng an toàn
        // ====================================================
        private ActionResult RedirectToLocal(string returnUrl, string role)
        {
            // 1. Kiểm tra nếu returnUrl rỗng thì thôi
            if (string.IsNullOrEmpty(returnUrl))
            {
                return RedirectToRoleHome(role);
            }

            // 2. Giải mã URL (đề phòng trường hợp nó vẫn còn encode ký tự %)
            // Ví dụ: https%3A%2F%2F -> https://
            returnUrl = Server.UrlDecode(returnUrl);

            // 3. TRƯỜNG HỢP A: URL Tương đối (Ví dụ: /Home/Cart)
            // Hàm IsLocalUrl của MVC chỉ chấp nhận loại này
            if (Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            // Ta phải tự kiểm tra xem nó có cùng Domain với web mình không
            Uri uriResult;
            // Thử tạo đối tượng Uri từ chuỗi returnUrl
            if (Uri.TryCreate(returnUrl, UriKind.Absolute, out uriResult))
            {
                // So sánh Authority (Domain + Port) của returnUrl với Request hiện tại
                if (string.Equals(uriResult.Authority, Request.Url.Authority, StringComparison.OrdinalIgnoreCase))
                {
                    return Redirect(returnUrl);
                }
            }

            // 5. Nếu không thỏa mãn cả 2 -> Về trang chủ mặc định
            return RedirectToRoleHome(role);
        }

        // Hàm phụ để code gọn hơn
        private ActionResult RedirectToRoleHome(string role)
        {
            string r = role != null ? role.Trim() : "";
            if (r.Equals("Admin", StringComparison.OrdinalIgnoreCase) ||
                r.Equals("Employee", StringComparison.OrdinalIgnoreCase))
            {
                return RedirectToAction("Dashboard", "Admin");
            }
            return RedirectToAction("Index", "Home");
        }


        // ====================================================
        // 2. ĐĂNG KÝ
        // ====================================================
        [HttpGet]
        public ActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken] // Bảo mật form
        public ActionResult Register(string name, string username, string email, string password, string phone, DateTime? birthDate, string gender)
        {
            try
            {
                // 1. Validate cơ bản (Server-side check)
                if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password) || string.IsNullOrEmpty(username))
                {
                    ModelState.AddModelError("", "Vui lòng điền đầy đủ thông tin bắt buộc.");
                    return View(); // Trả lại View với lỗi
                }

                // 2. Hash mật khẩu
                string hashedPassword = BCrypt.Net.BCrypt.HashPassword(password);

                // 3. Chuẩn bị tham số SP
                var resultParam = new ObjectParameter("Result", typeof(int));
                var userIdParam = new ObjectParameter("UserID", typeof(int));
                var maKHParam = new ObjectParameter("MaKH", typeof(int));

                // 4. Gọi Stored Procedure
                db.sp_RegisterUserWithSeparateName(
                    name,
                    username,
                    email,
                    hashedPassword,
                    "Customer", // Mặc định role là Customer
                    phone,
                    birthDate,
                    gender,
                    resultParam,
                    userIdParam,
                    maKHParam
                );

                // 5. Xử lý kết quả trả về từ DB
                int result = resultParam.Value != null ? (int)resultParam.Value : -1;

                if (result == 0) // Thành công
                {
                    // Lưu thông báo vào TempData để hiển thị bên trang Login
                    TempData["SuccessMessage"] = "Đăng ký thành công! Vui lòng đăng nhập.";

                    // Chuyển hướng sang trang Login
                    return RedirectToAction("Login", "Auth");
                }
                else if (result == 1) // Trùng tài khoản/email
                {
                    ModelState.AddModelError("", "Tên đăng nhập hoặc Email đã tồn tại!");
                    return View(); // Ở lại trang đăng ký để sửa
                }
                else // Lỗi khác
                {
                    ModelState.AddModelError("", "Lỗi đăng ký không xác định. Mã lỗi: " + result);
                    return View();
                }
            }
            catch (Exception ex)
            {
                // Bắt lỗi hệ thống
                ModelState.AddModelError("", "Lỗi hệ thống: " + ex.Message);
                return View();
            }
        }

        // ====================================================
        // 3. ĐĂNG XUẤT (LOGIC MỚI: XÓA SẠCH BÁCH)
        // ====================================================
        // File: Controllers/Auth/AuthController.cs

        // Bỏ [HttpPost] để thẻ <a> bình thường cũng gọi được
        //[HttpGet]
        //public ActionResult Logout()
        //{
        //    // 1. Xóa sạch Session
        //    Session["Cart"] = null;
        //    Session["MaKH"] = null;
        //    Session["UserID"] = null;
        //    Session["UserName"] = null;
        //    Session["UserRole"] = null;

        //    Session.Clear();   // Xóa dữ liệu trong RAM
        //    Session.Abandon(); // Hủy ID phiên làm việc hiện tại

        //    // 2. Xóa Cookie Authentication (nếu có dùng)
        //    if (Request.Cookies["AuthToken"] != null)
        //    {
        //        var c = new HttpCookie("AuthToken");
        //        c.Expires = DateTime.Now.AddDays(-1);
        //        Response.Cookies.Add(c);
        //    }

        //    // 3. Xóa Cookie Session của ASP.NET (Để trình duyệt quên phiên cũ đi)
        //    if (Request.Cookies["ASP.NET_SessionId"] != null)
        //    {
        //        var c = new HttpCookie("ASP.NET_SessionId");
        //        c.Expires = DateTime.Now.AddDays(-1);
        //        Response.Cookies.Add(c);
        //    }

        //    // 4. Chuyển hướng về trang chủ
        //    return RedirectToAction("Index", "Home");
        //}
        public ActionResult Logout()
        {
            // Xóa toàn bộ session
            Session.Clear();
            Session.Abandon();
            System.Web.Security.FormsAuthentication.SignOut();

            // Chuyển hướng về trang chủ
            return RedirectToAction("Index", "Home");
        }
        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}