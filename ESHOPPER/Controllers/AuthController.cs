using BCrypt.Net;
using ESHOPPER.Helpers;
using ESHOPPER.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity.Core.Objects;
using System.Linq;
using System.Web;
using System.Web.Mvc;

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
        [ValidateAntiForgeryToken] // Nên có khi submit form truyền thống để bảo mật
        public ActionResult Login(string loginInput, string password, string returnUrl = "")
        {
            try
            {
                // 1. Validate đầu vào
                if (string.IsNullOrEmpty(loginInput) || string.IsNullOrEmpty(password))
                {
                    ModelState.AddModelError("", "Vui lòng nhập đầy đủ thông tin!");
                    return View(); // Trả về View đăng nhập kèm lỗi
                }

                // 2. Tìm User
                var user = db.Users.FirstOrDefault(u => u.Name == loginInput || u.Email == loginInput);

                // 3. Kiểm tra tồn tại
                if (user == null)
                {
                    ModelState.AddModelError("", "Tài khoản không tồn tại!");
                    return View();
                }

                // 4. Verify Password
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
                // 5. ĐĂNG NHẬP THÀNH CÔNG -> LƯU SESSION
                // ====================================================

                // Lấy MaKH
                int maKH = 0;
                var userKH = db.KhachHangs.FirstOrDefault(k => k.UserId == user.UserID);
                if (userKH != null)
                {
                    maKH = userKH.MaKH;
                }

                // Lưu Session
                Session["UserID"] = user.UserID;
                Session["MaKH"] = maKH;
                Session["UserName"] = user.Name;
                Session["UserRole"] = user.Role;

                // 6. Gộp giỏ hàng
                if (Session["Cart"] != null && maKH > 0)
                {
                    MergeSessionCartToDb(maKH);
                }

                // Tạo Token (Nếu cần dùng cho mục đích khác, ko thì có thể bỏ qua)
                // var token = JwtHelper.GenerateToken(loginInput, user.Role, user.Name); 

                // ====================================================
                // 7. XỬ LÝ CHUYỂN HƯỚNG (REDIRECT)
                // ====================================================
                //string targetUrl = "";

                // Ưu tiên: Nếu có returnUrl hợp lệ
                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                {
                    return Redirect(returnUrl);
                }
                else
                {
                    // Phân quyền
                    string currentRole = user.Role != null ? user.Role.Trim() : "";

                    if (currentRole.Equals("Admin", StringComparison.OrdinalIgnoreCase) ||
                        currentRole.Equals("Employee", StringComparison.OrdinalIgnoreCase))
                    {
                        // Chuyển về Dashboard Admin
                        // Lưu ý: Đảm bảo Controller "Admin" và Action "Dashboard" tồn tại
                        return RedirectToAction("Dashboard", "Admin",new {area="Admin"});

                        // Nếu dùng Area thì dùng dòng dưới:
                        // return RedirectToAction("Index", "Home", new { area = "Admin" });
                    }
                    else
                    {
                        // Về trang chủ
                        return RedirectToAction("Index", "Home", new {area=""});
                    }
                }
            }
            catch (Exception ex)
            {
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
        private void MergeSessionCartToDb(int maKH)
        {
            try
            {
                var sessionCart = (GioHang)Session["Cart"];
                if (sessionCart != null && sessionCart.ChiTietGioHangs != null && sessionCart.ChiTietGioHangs.Count > 0)
                {
                    // 1. Tìm hoặc tạo giỏ hàng DB
                    var dbCart = db.GioHangs.FirstOrDefault(g => g.MaKH == maKH);
                    if (dbCart == null)
                    {
                        dbCart = new GioHang { MaKH = maKH, NgayTao = DateTime.Now };
                        db.GioHangs.Add(dbCart);
                        db.SaveChanges();
                    }

                    // 2. Duyệt từng món session -> chèn vào DB
                    foreach (var itemSess in sessionCart.ChiTietGioHangs)
                    {
                        var dbItem = db.ChiTietGioHangs.FirstOrDefault(c =>
                            c.MaGioHang == dbCart.MaGioHang &&
                            c.MaSP == itemSess.MaSP &&
                            c.MaSize == itemSess.MaSize &&
                            c.MaMau == itemSess.MaMau);

                        if (dbItem != null)
                        {
                            dbItem.SoLuong += itemSess.SoLuong; // Cộng dồn
                        }
                        else
                        {
                            var newItem = new ChiTietGioHang
                            {
                                MaGioHang = dbCart.MaGioHang,
                                MaSP = itemSess.MaSP,
                                SoLuong = itemSess.SoLuong,
                                DonGia = itemSess.DonGia,
                                MaSize = itemSess.MaSize,
                                MaMau = itemSess.MaMau
                            };
                            db.ChiTietGioHangs.Add(newItem); // Thêm mới
                        }
                    }
                    db.SaveChanges();

                    // 3. QUAN TRỌNG: Xóa sạch Session Cart sau khi gộp xong
                    Session["Cart"] = null;
                }
            }
            catch (Exception) { /* Log error if needed */ }
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
        public JsonResult Register(string name, string username, string email, string password, string phone, DateTime? birthDate, string gender)
        {
            try
            {
                if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password) || string.IsNullOrEmpty(username))
                    return Json(new { success = false, message = "Vui lòng điền đầy đủ thông tin." });

                string hashedPassword = BCrypt.Net.BCrypt.HashPassword(password);

                var resultParam = new ObjectParameter("Result", typeof(int));
                var userIdParam = new ObjectParameter("UserID", typeof(int));
                var maKHParam = new ObjectParameter("MaKH", typeof(int));

                db.sp_RegisterUserWithSeparateName(
                    name, username, email, hashedPassword, "Customer", phone, birthDate, gender,
                    resultParam, userIdParam, maKHParam
                );

                int result = resultParam.Value != null ? (int)resultParam.Value : -1;

                if (result == 0)
                    return Json(new { success = true, message = "Đăng ký thành công! Vui lòng đăng nhập." });
                else if (result == 1)
                    return Json(new { success = false, message = "Tài khoản đã tồn tại!" });
                else
                    return Json(new { success = false, message = "Lỗi đăng ký. Mã lỗi: " + result });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi hệ thống: " + ex.Message });
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