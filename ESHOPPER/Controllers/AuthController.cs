using BCrypt.Net;
using ESHOPPER.Helpers;
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

                // 2. Tìm User trong DB (Dùng LINQ thay vì SP để dễ kiểm soát)
                // Tìm theo Tên đăng nhập HOẶC Email
                var user = db.Users.FirstOrDefault(u => u.Name == loginInput || u.Email == loginInput);

                // 3. Kiểm tra tài khoản tồn tại
                if (user == null)
                {
                    ModelState.AddModelError("", "Tài khoản không tồn tại!");
                    return View();
                }

                // 4. Kiểm tra Mật khẩu (Verify Hash)
                bool isPasswordValid = false;
                try
                {
                    if (!string.IsNullOrEmpty(user.PasswordHash))
                    {
                        isPasswordValid = BCrypt.Net.BCrypt.Verify(password, user.PasswordHash);
                    }
                }
                catch
                {
                    isPasswordValid = false;
                }

                if (!isPasswordValid)
                {
                    ModelState.AddModelError("", "Mật khẩu không đúng!");
                    return View();
                }

                // ====================================================
                // 5. ĐĂNG NHẬP THÀNH CÔNG
                // ====================================================

                // A. Xử lý logic nghiệp vụ (Khách hàng & Giỏ hàng)
                int maKH = 0;
                var userKH = db.KhachHangs.FirstOrDefault(k => k.UserId == user.UserID);
                if (userKH != null)
                {
                    maKH = userKH.MaKH;
                }

                // B. Lưu Session (Dùng để hiển thị thông tin nhanh trên Header)
                Session["UserID"] = user.UserID;
                Session["MaKH"] = maKH;
                Session["UserName"] = user.Name;
                Session["UserRole"] = user.Role;

                //// C. Gộp giỏ hàng từ Guest sang User (nếu có)
                //if (Session["Cart"] != null && maKH > 0)
                //{
                //    MergeSessionCartToDb(maKH); // Hàm riêng của bạn
                //}

                // D. [QUAN TRỌNG NHẤT] Ghi nhận đăng nhập với hệ thống (Cookie)
                // Dòng này giúp server nhớ bạn là ai kể cả khi Session bị timeout
                FormsAuthentication.SetAuthCookie(user.Name, false);

                // ====================================================
                // 6. XỬ LÝ CHUYỂN HƯỚNG (REDIRECT)
                // ====================================================
                //string targetUrl = "";

                // Ưu tiên 1: Nếu có returnUrl hợp lệ (VD: đang vào Admin bị đá ra login, giờ vào lại Admin)
                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                {
                    return Redirect(returnUrl);
                }
                else
                {
                    // Ưu tiên 2: Phân quyền theo Role
                    string currentRole = user.Role != null ? user.Role.Trim() : "";

                    if (currentRole.Equals("Admin", StringComparison.OrdinalIgnoreCase) ||
                        currentRole.Equals("Employee", StringComparison.OrdinalIgnoreCase))
                    {
                        // Chuyển sang trang Quản trị
                        // Nếu dùng Area: RedirectToAction("Index", "Home", new { area = "Admin" })
                        // Nếu không dùng Area: RedirectToAction("Dashboard", "Admin")
                        return RedirectToAction("Dashboard", "Admin");
                    }
                    else
                    {
                        // Khách hàng -> Về trang chủ
                        return RedirectToAction("Index", "Home");
                    }
                }
            }
            catch (Exception ex)
            {
                // Ghi log lỗi nếu cần
                ModelState.AddModelError("", "Lỗi hệ thống: " + ex.Message);
                return View();
            }
        }

        //[HttpPost]
        //public JsonResult Login(string loginInput, string password, string returnUrl = "")
        //{
        //    try
        //    {
        //        // 1. Validate đầu vào
        //        if (string.IsNullOrEmpty(loginInput) || string.IsNullOrEmpty(password))
        //        {
        //            return Json(new { success = false, message = "Vui lòng nhập đầy đủ thông tin!" });
        //        }

        //        // 2. Chuẩn bị tham số Stored Procedure
        //        // Lưu ý: Đảm bảo biến 'db' đã được khởi tạo (Entities context)
        //        var resultParam = new ObjectParameter("Result", typeof(int));
        //        var userIdParam = new ObjectParameter("UserID", typeof(int));
        //        var nameParam = new ObjectParameter("Name", typeof(string));
        //        var roleParam = new ObjectParameter("Role", typeof(string));
        //        var passwordHashParam = new ObjectParameter("PasswordHash", typeof(string));

        //        // 3. Gọi Stored Procedure
        //        db.sp_LoginUser(
        //            loginInput,
        //            resultParam,
        //            userIdParam,
        //            nameParam,
        //            roleParam,
        //            passwordHashParam
        //        );

        //        // 4. Kiểm tra kết quả từ DB
        //        int result = resultParam.Value != null ? (int)resultParam.Value : 0;
        //        string storedHash = passwordHashParam.Value != null ? passwordHashParam.Value.ToString() : "";

        //        if (result == 1 || string.IsNullOrEmpty(storedHash))
        //        {
        //            return Json(new { success = false, message = "Tài khoản không tồn tại hoặc sai thông tin!" });
        //        }

        //        // 5. Verify Password
        //        bool isPasswordValid = false;
        //        try
        //        {
        //            isPasswordValid = BCrypt.Net.BCrypt.Verify(password, storedHash);
        //        }
        //        catch
        //        {
        //            isPasswordValid = false;
        //        }

        //        if (!isPasswordValid)
        //        {
        //            return Json(new { success = false, message = "Mật khẩu không đúng!" });
        //        }

        //        // ====================================================
        //        // 6. ĐĂNG NHẬP THÀNH CÔNG -> LƯU SESSION
        //        // ====================================================
        //        int userId = (int)userIdParam.Value;
        //        string userName = nameParam.Value.ToString();
        //        string userRole = roleParam.Value.ToString();

        //        // Lấy MaKH chuẩn từ bảng KhachHang (nếu có)
        //        int maKH = 0; 
        //        var userKH = db.KhachHangs.FirstOrDefault(k => k.User.Name == loginInput || k.Email == loginInput);
        //        if (userKH != null) maKH = userKH.MaKH;

        //        Session["UserID"] = userId;
        //        Session["MaKH"] = maKH;
        //        Session["UserName"] = userName;
        //        Session["UserRole"] = userRole;

        //        // 7. GỘP GIỎ HÀNG
        //        if (Session["Cart"] != null && maKH > 0)
        //        {
        //            MergeSessionCartToDb(maKH); // Hàm riêng của bạn
        //        }

        //        // Tạo Token (Optional - nếu bạn dùng JWT)
        //        var token = JwtHelper.GenerateToken(loginInput, userRole, userName);

        //        // ====================================================
        //        // [QUAN TRỌNG] 8. XỬ LÝ ĐÍCH ĐẾN (TARGET URL)
        //        // ====================================================
        //        string targetUrl = "";

        //        // Ưu tiên: Nếu có returnUrl (khách đang vào link nào đó bị bắt login)
        //        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
        //        {
        //            targetUrl = returnUrl;
        //        }
        //        else
        //        {
        //            // Phân quyền theo Role
        //            // Kiểm tra chính xác chuỗi "Admin" trong database của bạn
        //            if (userRole.Equals("Admin", StringComparison.OrdinalIgnoreCase))
        //            {
        //                // Chuyển sang Area Admin -> Controller Home -> Action Index
        //                targetUrl = Url.Action("Index", "Home", new { area = "Admin" });
        //            }
        //            else
        //            {
        //                // Chuyển sang trang chủ người dùng
        //                targetUrl = Url.Action("Index", "Home", new { area = "" });
        //            }
        //        }

        //        // Trả về JSON chứa URL để JS chuyển hướng
        //        return Json(new
        //        {
        //            success = true,
        //            message = "Đăng nhập thành công!",
        //            token = token,
        //            returnUrl = targetUrl
        //        });
        //    }
        //    catch (Exception ex)
        //    {
        //        return Json(new { success = false, message = "Lỗi hệ thống: " + ex.Message });
        //    }
        //}

        // Helper: Gộp giỏ hàng từ Session vào DB rồi xóa Session
        //private void MergeSessionCartToDb(int maKH)
        //{
        //    try
        //    {
        //        var sessionCart = (GioHang)Session["Cart"];
        //        if (sessionCart != null && sessionCart.ChiTietGioHangs != null && sessionCart.ChiTietGioHangs.Count > 0)
        //        {
        //            // 1. Tìm hoặc tạo giỏ hàng DB
        //            var dbCart = db.GioHangs.FirstOrDefault(g => g.MaKH == maKH);
        //            if (dbCart == null)
        //            {
        //                dbCart = new GioHang { MaKH = maKH, NgayTao = DateTime.Now };
        //                db.GioHangs.Add(dbCart);
        //                db.SaveChanges();
        //            }

        //            // 2. Duyệt từng món session -> chèn vào DB
        //            foreach (var itemSess in sessionCart.ChiTietGioHangs)
        //            {
        //                var dbItem = db.ChiTietGioHangs.FirstOrDefault(c =>
        //                    c.MaGioHang == dbCart.MaGioHang &&
        //                    c.MaSP == itemSess.MaSP &&
        //                    c.MaSize == itemSess.MaSize &&
        //                    c.MaMau == itemSess.MaMau);

        //                if (dbItem != null)
        //                {
        //                    dbItem.SoLuong += itemSess.SoLuong; // Cộng dồn
        //                }
        //                else
        //                {
        //                    var newItem = new ChiTietGioHang
        //                    {
        //                        MaGioHang = dbCart.MaGioHang,
        //                        MaSP = itemSess.MaSP,
        //                        SoLuong = itemSess.SoLuong,
        //                        DonGia = itemSess.DonGia,
        //                        MaSize = itemSess.MaSize,
        //                        MaMau = itemSess.MaMau
        //                    };
        //                    db.ChiTietGioHangs.Add(newItem); // Thêm mới
        //                }
        //            }
        //            db.SaveChanges();

        //            // 3. QUAN TRỌNG: Xóa sạch Session Cart sau khi gộp xong
        //            Session["Cart"] = null;
        //        }
        //    }
        //    catch (Exception) { /* Log error if needed */ }
        //}

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