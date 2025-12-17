using ESHOPPER.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;

namespace ESHOPPER.Controllers
{
    public class Product_VariantsController : Controller
    {
        private QlyFashionShopEntities db = new QlyFashionShopEntities();

        public ActionResult Index(int? id)
        {
            var bienThes = db.BienTheSanPhams
                             .Include(b => b.SanPham)
                             .Include(b => b.MauSac)
                             .Include(b => b.KichThuoc)
                             .AsQueryable(); 

            if (id.HasValue)
            {
                bienThes = bienThes.Where(x => x.MaSP == id);

                var sanPham = db.SanPhams.Find(id);
                if (sanPham != null)
                {
                    ViewBag.TenSanPham = sanPham.TenSanPham;
                    ViewBag.MaSanPham = id;     
                }
            }

            return View(bienThes.ToList());
        }

        // GET: Product_Variants/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            BienTheSanPham bienTheSanPham = db.BienTheSanPhams.Find(id);
            if (bienTheSanPham == null)
            {
                return HttpNotFound();
            }

            // Lấy tên sản phẩm cha để hiển thị đẹp hơn
            var parentProduct = db.SanPhams.Find(bienTheSanPham.MaSP);
            ViewBag.TenSanPham = parentProduct != null ? parentProduct.TenSanPham : "Sản phẩm";

            return View(bienTheSanPham);
        }

        // ==========================================
        // GET: BienTheSanPham/Create?productId=5
        // ==========================================
        public ActionResult Create(int? productId)
        {
            // 1. Kiểm tra ID. Nếu không có, đẩy về trang danh sách sản phẩm
            if (productId == null)
            {
                return RedirectToAction("Index", "Product_Variants");
            }

            // 2. Tìm sản phẩm cha để lấy tên hiển thị
            var parentProduct = db.SanPhams.Find(productId);
            if (parentProduct == null) return HttpNotFound();

            // 3. Gửi thông tin sang View để hiển thị (Read-only)
            ViewBag.TenSanPham = parentProduct.TenSanPham;

            // 4. Load Dropdown cho Màu và Size (KHÔNG load dropdown Sản phẩm)
            ViewBag.MaMau = new SelectList(db.MauSacs, "MaMau", "TenMau");
            ViewBag.MaSize = new SelectList(db.KichThuocs, "MaSize", "TenSize");

            // 5. Gán ID vào model để form tự nhận
            var model = new BienTheSanPham();
            model.MaSP = productId.Value; // Tự điền ID
            model.MaTrangThai = 1;        // Mặc định Active

            return View(model);
        }

        // ==========================================
        // POST: BienTheSanPham/Create
        // ==========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(BienTheSanPham bienThe, HttpPostedFileBase ImageUpload)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // --- XỬ LÝ UPLOAD ẢNH (Giữ nguyên như cũ) ---
                    if (ImageUpload != null && ImageUpload.ContentLength > 0)
                    {
                        string fileName = Path.GetFileNameWithoutExtension(ImageUpload.FileName);
                        string extension = Path.GetExtension(ImageUpload.FileName);
                        fileName = fileName + "_" + DateTime.Now.ToString("yyyyMMddHHmmss") + extension;
                        string path = Path.Combine(Server.MapPath("~/Content/Images/"), fileName);
                        ImageUpload.SaveAs(path);
                        bienThe.AnhBienThe = fileName;
                    }

                    // --- XỬ LÝ DỮ LIỆU ---
                    if (bienThe.MaTrangThai == null) bienThe.MaTrangThai = 1;

                    db.BienTheSanPhams.Add(bienThe);
                    db.SaveChanges();

                    // Thành công -> Về lại danh sách biến thể của sản phẩm này
                    return RedirectToAction("Index", new { id= bienThe.MaSP });
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Lỗi hệ thống: " + ex.Message);
                }
            }

            // --- NẾU CÓ LỖI: PHẢI LOAD LẠI THÔNG TIN SẢN PHẨM CHA ---
            var parentProduct = db.SanPhams.Find(bienThe.MaSP);
            ViewBag.TenSanPham = parentProduct != null ? parentProduct.TenSanPham : "Sản phẩm";

            // Load lại Dropdown Màu/Size
            ViewBag.MaMau = new SelectList(db.MauSacs, "MaMau", "TenMau", bienThe.MaMau);
            ViewBag.MaSize = new SelectList(db.KichThuocs, "MaSize", "TenSize", bienThe.MaSize);

            return View(bienThe);
        }
        // GET: Product_Variants/Edit/5
        // ==========================================
        // GET: Product_Variants/Edit/5
        // ==========================================
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            // Tìm biến thể theo ID
            BienTheSanPham bienTheSanPham = db.BienTheSanPhams.Find(id);
            if (bienTheSanPham == null)
            {
                return HttpNotFound();
            }

            // Load Dropdown
            ViewBag.MaMau = new SelectList(db.MauSacs, "MaMau", "TenMau", bienTheSanPham.MaMau);
            ViewBag.MaSize = new SelectList(db.KichThuocs, "MaSize", "TenSize", bienTheSanPham.MaSize);

            // Lấy tên sản phẩm cha để hiển thị trên View (Read-only)
            var parentProduct = db.SanPhams.Find(bienTheSanPham.MaSP);
            ViewBag.TenSanPham = parentProduct != null ? parentProduct.TenSanPham : "Sản phẩm";

            return View(bienTheSanPham);
        }

        // ==========================================
        // POST: Product_Variants/Edit/5
        // ==========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(BienTheSanPham bienTheSanPham, HttpPostedFileBase ImageUpload)
        {
            // 1. Bỏ qua validate các bảng liên kết (Tránh lỗi ModelState.IsValid = false oan)
            ModelState.Remove("SanPham");
            ModelState.Remove("MauSac");
            ModelState.Remove("KichThuoc");

            if (ModelState.IsValid)
            {
                try
                {
                    // 2. Xử lý Upload ảnh (Nếu có chọn ảnh mới)
                    if (ImageUpload != null && ImageUpload.ContentLength > 0)
                    {
                        // Tạo tên file mới
                        string fileName = Path.GetFileNameWithoutExtension(ImageUpload.FileName);
                        string extension = Path.GetExtension(ImageUpload.FileName);
                        fileName = fileName + "_" + DateTime.Now.ToString("yyyyMMddHHmmss") + extension;

                        // Lưu file
                        string path = Path.Combine(Server.MapPath("~/Content/Images/"), fileName);
                        ImageUpload.SaveAs(path);

                        // Cập nhật tên ảnh mới vào Model
                        bienTheSanPham.AnhBienThe = fileName;
                    }
                    // Lưu ý: Nếu không upload ảnh mới, bienTheSanPham.AnhBienThe sẽ giữ giá trị cũ 
                    // nhờ dòng @Html.HiddenFor(model => model.AnhBienThe) bên View.

                    // 3. Cập nhật vào Database
                    db.Entry(bienTheSanPham).State = EntityState.Modified;
                    db.SaveChanges();

                    // 4. Chuyển hướng về trang danh sách biến thể của sản phẩm đó
                    return RedirectToAction("Index", new { id = bienTheSanPham.MaSP });
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Lỗi cập nhật: " + ex.Message);
                }
            }


            // Load lại tên sản phẩm cha
            var parentProduct = db.SanPhams.Find(bienTheSanPham.MaSP);
            ViewBag.TenSanPham = parentProduct != null ? parentProduct.TenSanPham : "Sản phẩm";

            // Load lại Dropdown
            ViewBag.MaMau = new SelectList(db.MauSacs, "MaMau", "TenMau", bienTheSanPham.MaMau);
            ViewBag.MaSize = new SelectList(db.KichThuocs, "MaSize", "TenSize", bienTheSanPham.MaSize);

            return View(bienTheSanPham);
        }

        // GET: Product_Variants/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            BienTheSanPham bienTheSanPham = db.BienTheSanPhams.Find(id);
            if (bienTheSanPham == null)
            {
                return HttpNotFound();
            }
            return View(bienTheSanPham);
        }

        // POST: Product_Variants/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            // 1. Tìm đối tượng cần xóa
            BienTheSanPham bienTheSanPham = db.BienTheSanPhams.Find(id);

            // Kiểm tra null cho an toàn (tránh lỗi nếu ID không tồn tại)
            if (bienTheSanPham == null)
            {
                return HttpNotFound();
            }

            // 2. [QUAN TRỌNG] Lưu lại ID sản phẩm cha trước khi xóa
            // Vì sau khi xóa dòng dưới, bienTheSanPham có thể bị mất context hoặc null
            int parentId = bienTheSanPham.MaSP;

            // 3. Thực hiện xóa
            db.BienTheSanPhams.Remove(bienTheSanPham);
            db.SaveChanges();

            // 4. Quay về đúng trang danh sách của sản phẩm cha đó
            // Lưu ý: Action Index của bạn nhận tham số là 'id'
            return RedirectToAction("Index", new { id = parentId });
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
