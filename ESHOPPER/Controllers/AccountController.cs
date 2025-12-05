using BCrypt.Net;
using ESHOPPER.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Data.SqlClient;
using System.Linq;
using System.Net;
using System.Web.Mvc;
using EntityState = System.Data.Entity.EntityState;

namespace ESHOPPER.Controllers.Admin
{
    [RoutePrefix("Admin/Account")]
    public class AccountController : Controller
    {
        private QlyFashionShopEntities db = new QlyFashionShopEntities();

        // GET: Admin/Account
        [Route("")]
        public ActionResult Index()
        {
            return View(db.Users.ToList());
        }

        // GET: Admin/Account/Details/5
        [Route("Details/{id:int}")]
        public ActionResult Details(int? id)
        {
            if (id == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            var user = db.Users.Find(id);
            if (user == null)
                return HttpNotFound();

            return View(user);
        }

        // GET: Admin/Account/Create
        [Route("Create")]
        public ActionResult Create()
        {
            return View();
        }

        // POST: Admin/Account/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route("Create")]
        public ActionResult Create([Bind(Include = "Name,Email,Role,PasswordHash")] User user)
        {
            if (ModelState.IsValid)
            {
                // 1. KIỂM TRA TÍNH DUY NHẤT CỦA EMAIL
                if (db.Users.Any(u => u.Email == user.Email))
                {
                    ModelState.AddModelError("Email", "Email này đã tồn tại trong hệ thống.");
                    return View(user);
                }

                // 2. HASH MẬT KHẨU
                string plainTextPassword = user.PasswordHash;
                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(plainTextPassword);

                // 3. GÁN NGÀY TẠO
                user.CreatedAt = DateTime.Now;

                // 4. LƯU VÀ XỬ LÝ NGOẠI LỆ (Nơi Trigger được thực thi)
                db.Users.Add(user);

                try
                {
                    db.SaveChanges(); // Lệnh INSERT được gửi đi, kích hoạt Trigger

                    TempData["SuccessMessage"] = $"Tài khoản {user.Name} ({user.Role}) đã được tạo thành công!";
                    return RedirectToAction("Index");
                }
                // BẮT LỖI TỪ TRIGGER (Nếu là AFTER INSERT ROLLBACK)
                catch (DbUpdateException ex)
                {
                    // Lấy lỗi SQL nội tại
                    var sqlEx = ex.InnerException?.InnerException as SqlException;

                    if (sqlEx != null)
                    {
                        // Mã lỗi 51000 là mã lỗi thường được ném ra từ Trigger THROW
                        if (sqlEx.Number == 51000)
                        {
                            // Lỗi giới hạn Admin do Trigger chặn
                            // Message chính là thông báo: "Không thể tạo Admin mới..."
                            ModelState.AddModelError("Role", sqlEx.Message);
                        }
                        else if (sqlEx.Number == 2627 || sqlEx.Number == 2601) // Lỗi Unique/Primary Key
                        {
                            ModelState.AddModelError("Email", "Email hoặc tên người dùng đã bị trùng lặp.");
                        }
                        else
                        {
                            // Lỗi SQL không xác định khác (Ví dụ: Lỗi Trigger syntax/NOT NULL)
                            ModelState.AddModelError("", $"Lỗi SQL: {sqlEx.Message}");
                        }
                    }
                    else
                    {
                        // Bắt lỗi Concurrency (nếu Trigger là INSTEAD OF và chặn lệnh)
                        ModelState.AddModelError("", "Lỗi đồng thời hệ thống. Thao tác bị chặn.");
                    }

                    return View(user);
                }
            }

            // Nếu Model không hợp lệ
            return View(user);
        }

        // GET: Admin/Account/Edit/5
        [Route("Edit/{id:int}")]
        public ActionResult Edit(int? id)
        {
            if (id == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            var user = db.Users.Find(id);
            if (user == null)
                return HttpNotFound();

            return View(user);
        }

        // POST: Admin/Account/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route("Edit/{id:int}")]
        public ActionResult Edit([Bind(Include = "UserID,Name,Email,Role,PasswordHash,CreatedAt")] User user)
        {
            if (ModelState.IsValid)
            {
                // 1. Đánh dấu toàn bộ đối tượng là đã bị sửa đổi
                db.Entry(user).State = EntityState.Modified;

                // 2. XỬ LÝ MẬT KHẨU (Phần quan trọng nhất)
                // Kiểm tra xem admin có nhập gì vào ô "New Password" không
                if (string.IsNullOrWhiteSpace(user.PasswordHash))
                {
                    // TRƯỜNG HỢP 1: Admin để trống ô mật khẩu (nghĩa là không muốn đổi)
                    // Chúng ta phải báo cho EF: "KHÔNG được cập nhật trường PasswordHash".
                    // Nếu không có dòng này, EF sẽ lưu một chuỗi rỗng vào DB, LÀM MẤT mật khẩu cũ.
                    db.Entry(user).Property(x => x.PasswordHash).IsModified = false;
                }
                else
                {
                    // TRƯỜNG HỢP 2: Admin có nhập mật khẩu mới
                    // Băm (hash) mật khẩu mới đó và gán lại vào model.
                    // EF sẽ tự động lưu giá trị hash mới này.
                    user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(user.PasswordHash);
                }

                // 3. BẢO VỆ NGÀY TẠO (CreatedAt)
                // Báo cho EF: "KHÔNG được cập nhật trường CreatedAt"
                // Dù giá trị của nó là gì, chúng ta cũng không muốn nó bị thay đổi.
                db.Entry(user).Property(x => x.CreatedAt).IsModified = false;

                // 4. LƯU THAY ĐỔI
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            // Nếu model không hợp lệ, trả lại View
            return View(user);
        }

        // GET: Admin/Account/Delete/5
        [Route("Delete/{id:int}")]
        public ActionResult Delete(int? id)
        {
            if (id == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            var user = db.Users.Find(id);
            if (user == null)
                return HttpNotFound();

            return View(user);
        }

        // POST: Admin/Account/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Route("Delete/{id:int}")]
        public ActionResult DeleteConfirmed(int id)
        {
            // 1. TÌM TÀI KHOẢN CẦN XÓA
            var user = db.Users.Find(id);

            if (user == null)
            {
                return HttpNotFound();
            }

            try
            {
                // 2. XỬ LÝ KHÁCH HÀNG (BẢNG CON) TRƯỚC
                // Tìm bản ghi KhachHang liên quan đến UserId này
                var khachHang = db.KhachHangs.FirstOrDefault(k => k.UserId == id);

                if (khachHang != null)
                {
                    // Nếu tìm thấy, XÓA KhachHang trước để gỡ bỏ ràng buộc khóa ngoại
                    db.KhachHangs.Remove(khachHang);
                }

                // 3. XÓA USER (BẢNG CHA)
                db.Users.Remove(user);

                // 4. LƯU THAY ĐỔI
                db.SaveChanges();
            }
            catch (DbUpdateException )
            {
                // Nếu vẫn còn lỗi khóa ngoại (ví dụ: User này còn đơn hàng, v.v.)
                // Bạn nên ghi lại lỗi (logging)

                // Cung cấp thông báo thân thiện hơn cho người dùng
                ModelState.AddModelError("", "Không thể xóa tài khoản này. Có thể tài khoản còn liên kết với Đơn hàng hoặc dữ liệu giao dịch khác.");

                // Tải lại trang Delete với thông báo lỗi
                return View("Delete", user);
            }

            return RedirectToAction("Index");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                db.Dispose();
            base.Dispose(disposing);
        }
    }
}
