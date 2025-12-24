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
                if (string.IsNullOrEmpty(loginInput) || string.IsNullOrEmpty(password))
                {
                    ModelState.AddModelError("", "Vui lòng nhập đầy đủ thông tin!");
                    return View();
                }

                var user = db.Users.FirstOrDefault(u => u.Name == loginInput || u.Email == loginInput);
                if (user == null)
                {
                    ModelState.AddModelError("", "Tài khoản không tồn tại!");
                    return View();
                }

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
                    user.Name,                 
                    DateTime.Now,
                    DateTime.Now.AddHours(3),  
                    false,
                    user.Role                  
                );

                string encryptedTicket = FormsAuthentication.Encrypt(ticket);

                var cookie = new HttpCookie(
                    FormsAuthentication.FormsCookieName,
                    encryptedTicket
                );

                Response.Cookies.Add(cookie);


                var sessionCart = Session["Cart"] as List<ChiTietGioHang>;

                if (maKH > 0 && sessionCart != null && sessionCart.Count > 0)
                {
                    var dbCart = db.GioHangs.FirstOrDefault(g => g.MaKH == maKH);
                    if (dbCart == null)
                    {
                        dbCart = new GioHang { MaKH = maKH, NgayTao = DateTime.Now };
                        db.GioHangs.Add(dbCart);
                        db.SaveChanges();
                    }

                    foreach (var itemSess in sessionCart)
                    {
                        var dbItem = db.ChiTietGioHangs.FirstOrDefault(c =>
                            c.MaGioHang == dbCart.MaGioHang &&
                            c.MaBienThe == itemSess.MaBienThe);

                        if (dbItem != null)
                        {
                            dbItem.SoLuong += itemSess.SoLuong;
                        }
                        else
                        {
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

                    db.SaveChanges();
                    Session["Cart"] = null;
                }

                var gioHangUser = db.GioHangs.FirstOrDefault(g => g.MaKH == maKH);
                Session["CartCount"] = gioHangUser != null ? (gioHangUser.ChiTietGioHangs.Sum(c => c.SoLuong) ?? 0) : 0;

                return RedirectToLocal(returnUrl, user.Role);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Lỗi đăng nhập: " + ex.Message);
                return View();
            }
        }

        private ActionResult RedirectToLocal(string returnUrl, string role)
        {
            if (string.IsNullOrEmpty(returnUrl))
            {
                return RedirectToRoleHome(role);
            }

            returnUrl = Server.UrlDecode(returnUrl);

            if (Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            Uri uriResult;
            if (Uri.TryCreate(returnUrl, UriKind.Absolute, out uriResult))
            {
                if (string.Equals(uriResult.Authority, Request.Url.Authority, StringComparison.OrdinalIgnoreCase))
                {
                    return Redirect(returnUrl);
                }
            }

            return RedirectToRoleHome(role);
        }

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


        [HttpGet]
        public ActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken] 
        public ActionResult Register(string name, string username, string email, string password, string phone, DateTime? birthDate, string gender)
        {
            try
            {
                if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password) || string.IsNullOrEmpty(username))
                {
                    ModelState.AddModelError("", "Vui lòng điền đầy đủ thông tin bắt buộc.");
                    return View(); 
                }

                string hashedPassword = BCrypt.Net.BCrypt.HashPassword(password);

                var resultParam = new ObjectParameter("Result", typeof(int));
                var userIdParam = new ObjectParameter("UserID", typeof(int));
                var maKHParam = new ObjectParameter("MaKH", typeof(int));

                db.sp_RegisterUserWithSeparateName(
                    name,
                    username,
                    email,
                    hashedPassword,
                    "Customer", 
                    phone,
                    birthDate,
                    gender,
                    resultParam,
                    userIdParam,
                    maKHParam
                );

                int result = resultParam.Value != null ? (int)resultParam.Value : -1;

                if (result == 0) 
                {
                    TempData["SuccessMessage"] = "Đăng ký thành công! Vui lòng đăng nhập.";

                    return RedirectToAction("Login", "Auth");
                }
                else if (result == 1) 
                {
                    ModelState.AddModelError("", "Tên đăng nhập hoặc Email đã tồn tại!");
                    return View(); 
                }
                else 
                {
                    ModelState.AddModelError("", "Lỗi đăng ký không xác định. Mã lỗi: " + result);
                    return View();
                }
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Lỗi hệ thống: " + ex.Message);
                return View();
            }
        }

        public ActionResult Logout()
        {
            Session.Clear();
            Session.Abandon();
            System.Web.Security.FormsAuthentication.SignOut();

            return RedirectToAction("Index", "Home");
        }
        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}