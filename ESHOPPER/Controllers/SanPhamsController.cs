using ESHOPPER.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using EntityState = System.Data.Entity.EntityState;

namespace ESHOPPER.Controllers.Admin
{
    [RoutePrefix("Admin/SanPhams")] // URL gốc cho controller
    public class SanPhamsController : Controller
    {
        private QlyFashionShopEntities db = new QlyFashionShopEntities();

        // GET: Admin/SanPhams
        [Route("")] // Index
        public ActionResult Index()
        {
            var sanPhams = db.SanPhams.Include(s => s.DanhMucSanPham).Include(s => s.NhaCungCap);
            return View(sanPhams.ToList());
        }

        // GET: Admin/SanPhams/Details/5
        [Route("Details/{id}")] // id bắt buộc
        public ActionResult Details(string id)
        {
            if (id == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            SanPham sanPham = db.SanPhams.Find(id);
            if (sanPham == null)
                return HttpNotFound();

            return View(sanPham);
        }

        // GET: Admin/SanPhams/Create
        [Route("Create")]
        public ActionResult Create()
        {
            ViewBag.MaDM = new SelectList(db.DanhMucSanPhams, "MaDM", "TenDanhMuc");
            ViewBag.MaNCC = new SelectList(db.NhaCungCaps, "MaNCC", "TenNCC");
            return View();
        }

        // POST: Admin/SanPhams/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route("Create")]
        public ActionResult Create([Bind(Include = "MaSP,MaDM,TenSanPham,MoTa,GiaBanLe,TrangThai,MaNCC")] SanPham sanPham, HttpPostedFileBase ImageFile)
        {
            // --- BẮT ĐẦU XÁC THỰC LẦN 1 (Tự động và Khóa chính) ---
            if (ModelState.IsValid)
            {
                // Kiểm tra MaSP (nvarchar NOT NULL, không Identity)
                if (string.IsNullOrEmpty(sanPham.MaSP))
                {
                    ModelState.AddModelError("MaSP", "Mã sản phẩm là bắt buộc.");
                }

                // Kiểm tra Khóa ngoại (nvarchar NOT NULL)
                if (string.IsNullOrEmpty(sanPham.MaDM))
                {
                    ModelState.AddModelError("MaDM", "Vui lòng chọn một Danh mục sản phẩm.");
                }
                if (string.IsNullOrEmpty(sanPham.MaNCC))
                {
                    ModelState.AddModelError("MaNCC", "Vui lòng chọn một Nhà cung cấp.");
                }

                // Kiểm tra MaSP có bị trùng không (Logic nghiệp vụ)
                if (db.SanPhams.Any(s => s.MaSP == sanPham.MaSP))
                {
                    ModelState.AddModelError("MaSP", "Mã sản phẩm đã tồn tại. Vui lòng nhập mã khác.");
                }
            }

            // --- BẮT ĐẦU XỬ LÝ DỮ LIỆU VÀ LƯU (Chỉ khi ModelState.IsValid) ---
            if (ModelState.IsValid)
            {
                // 1. Gán giá trị mặc định cho TRẠNG THÁI (Vì đã ẩn khỏi View)
                if (string.IsNullOrEmpty(sanPham.TrangThai))
                {
                    sanPham.TrangThai = "Hoạt động";
                }

                // 2. Xử lý ẢNH TẢI LÊN (AnhSP)
                if (ImageFile != null && ImageFile.ContentLength > 0)
                {
                    // Lấy tên file
                    var fileName = Path.GetFileName(ImageFile.FileName);
                    // Tạo đường dẫn lưu file (Dùng Server.MapPath để tìm thư mục vật lý)
                    var path = Path.Combine(Server.MapPath("~/Images/Products"), fileName);

                    // Lưu file vào thư mục
                    ImageFile.SaveAs(path);

                    // CHỈ LƯU TÊN FILE vào database
                    sanPham.AnhSP = fileName;
                }
                else
                {
                    // Gán ảnh mặc định nếu không tải lên (Giả sử file tồn tại)
                    sanPham.AnhSP = "placeholder.jpg";
                }

                // 3. Xử lý GiaBanLe nếu là NOT NULL (Và form gửi null)
                if (sanPham.GiaBanLe == null)
                {
                    sanPham.GiaBanLe = 0;
                }

                // 4. LƯU VÀO DATABASE
                try
                {
                    db.SanPhams.Add(sanPham);
                    db.SaveChanges();
                    return RedirectToAction("Index");
                }
                catch (System.Data.Entity.Validation.DbEntityValidationException ex)
                {
                    // Nếu lỗi vẫn xảy ra (ràng buộc không lường trước), bắt và thêm lỗi vào ModelState
                    var fullErrorMessage = string.Join("; ", ex.EntityValidationErrors.SelectMany(x => x.ValidationErrors).Select(x => x.PropertyName + ": " + x.ErrorMessage));
                    ModelState.AddModelError("", "Lưu thất bại do lỗi ràng buộc dữ liệu: " + fullErrorMessage);
                }
            }
            // --- KẾT THÚC LƯU ---

            // Nếu model không hợp lệ (lỗi validation), tải lại các DropDownList và trả về View
            ViewBag.MaDM = new SelectList(db.DanhMucSanPhams, "MaDM", "TenDanhMuc", sanPham.MaDM);
            ViewBag.MaNCC = new SelectList(db.NhaCungCaps, "MaNCC", "TenNCC", sanPham.MaNCC);
            return View(sanPham);
        }

        // GET: Admin/SanPhams/Edit/5
        [Route("Edit/{id}")]
        public ActionResult Edit(string id)
        {
            if (id == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            SanPham sanPham = db.SanPhams.Find(id);
            if (sanPham == null)
                return HttpNotFound();

            ViewBag.MaDM = new SelectList(db.DanhMucSanPhams, "MaDM", "TenDanhMuc", sanPham.MaDM);
            ViewBag.MaNCC = new SelectList(db.NhaCungCaps, "MaNCC", "TenNCC", sanPham.MaNCC);
            return View(sanPham);
        }

        // POST: Admin/SanPhams/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route("Edit/{id}")]
        // 1. Thêm "HttpPostedFileBase ImageFile"
        // 2. Xóa "AnhSP" khỏi [Bind] vì chúng ta sẽ xử lý nó thủ công
        public ActionResult Edit([Bind(Include = "MaSP,MaDM,TenSanPham,MoTa,GiaBanLe,TrangThai,MaNCC")] SanPham sanPham, HttpPostedFileBase ImageFile)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // 3. Tải sản phẩm GỐC từ Database lên
                    var sanPhamFromDb = db.SanPhams.Find(sanPham.MaSP);
                    if (sanPhamFromDb == null)
                    {
                        return HttpNotFound();
                    }

                    // 4. Cập nhật các thuộc tính từ "sanPham" (form)
                    //    sang "sanPhamFromDb" (đối tượng DB)
                    sanPhamFromDb.TenSanPham = sanPham.TenSanPham;
                    sanPhamFromDb.MoTa = sanPham.MoTa;
                    sanPhamFromDb.GiaBanLe = sanPham.GiaBanLe;
                    sanPhamFromDb.TrangThai = sanPham.TrangThai;
                    sanPhamFromDb.MaDM = sanPham.MaDM;
                    sanPhamFromDb.MaNCC = sanPham.MaNCC;

                    // 5. XỬ LÝ ẢNH MỚI (NẾU CÓ)
                    if (ImageFile != null && ImageFile.ContentLength > 0)
                    {
                        var fileName = Path.GetFileName(ImageFile.FileName);
                        var path = Path.Combine(Server.MapPath("~/Images/Products"), fileName);
                        ImageFile.SaveAs(path);

                        // Cập nhật đường dẫn ảnh mới vào model GỐC
                        sanPhamFromDb.AnhSP =  fileName;
                    }
                    // 6. NẾU KHÔNG CÓ FILE MỚI (ImageFile == null)
                    //    -> Không làm gì cả. "sanPhamFromDb.AnhSP" sẽ giữ nguyên giá trị cũ.

                    // 7. LƯU THAY ĐỔI
                    // Không cần db.Entry(...).State nữa vì EF đang theo dõi sanPhamFromDb
                    db.SaveChanges();
                    return RedirectToAction("Index");
                }
                catch (Exception ex)
                {
                    // (Thêm log lỗi tại đây)
                    ModelState.AddModelError("", "Đã xảy ra lỗi khi lưu: " + ex.Message);
                }
            }

            // Nếu model không hợp lệ, tải lại các DropDownList
            ViewBag.MaDM = new SelectList(db.DanhMucSanPhams, "MaDM", "TenDanhMuc", sanPham.MaDM);
            ViewBag.MaNCC = new SelectList(db.NhaCungCaps, "MaNCC", "TenNCC", sanPham.MaNCC);
            return View(sanPham);
        }

        // GET: Admin/SanPhams/Delete/5
        [Route("Delete/{id}")]
        public ActionResult Delete(string id)
        {
            if (id == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            SanPham sanPham = db.SanPhams.Find(id);
            if (sanPham == null)
                return HttpNotFound();

            return View(sanPham);
        }

        // POST: Admin/SanPhams/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Route("Delete/{id}")]
        public ActionResult DeleteConfirmed(string id)
        {
            SanPham sanPham = db.SanPhams.Find(id);
            db.SanPhams.Remove(sanPham);
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
