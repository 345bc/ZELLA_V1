using ESHOPPER.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using System.Data.Entity;

namespace ESHOPPER.Controllers.Admin
{

    public class AdminController : Controller
    {
        QlyFashionShopEntities db = new QlyFashionShopEntities();

        public ActionResult Dashboard()
        {
            var model = new AdminViewModel();

            model.TongDoanhThu = db.DonHangs
                .Where(d => d.TrangThai == 1)
                .Sum(d => (decimal?)d.TongTien) ?? 0;

            var thisMonth = DateTime.Now.Month;
            var thisYear = DateTime.Now.Year;
            model.DonHangMoi = db.DonHangs
                .Count(d => d.NgayDat.HasValue && d.NgayDat.Value.Month == thisMonth && d.NgayDat.Value.Year == thisYear);

            model.TongKhachHang = db.KhachHangs.Count();
            model.TongSanPham = db.SanPhams.Count();

            model.ChartDoanhThu = new List<decimal>();
            model.ChartLabelThang = new List<string>();

            for (int i = 1; i <= 12; i++)
            {
                model.ChartLabelThang.Add("T" + i);
                var revenue = db.DonHangs
                    .Where(d => d.NgayDat.HasValue && d.NgayDat.Value.Month == i && d.NgayDat.Value.Year == thisYear && d.TrangThai == 1)
                    .Sum(d => (decimal?)d.TongTien) ?? 0;

                model.ChartDoanhThu.Add(revenue / 1000000m);
            }

            var categoryStats = db.SanPhams
                .GroupBy(p => p.DanhMucSanPham.TenDanhMuc)
                .Select(g => new { Name = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .Take(5)
                .ToList();

            model.ChartLabelDanhMuc = categoryStats.Select(x => x.Name).ToList();
            model.ChartDataDanhMuc = categoryStats.Select(x => x.Count).ToList();

            model.ListDonHangMoi = db.DonHangs
                .OrderByDescending(d => d.NgayDat)
                .Take(5)
                .Include(d => d.KhachHang) // Join bảng khách hàng để lấy tên
                .ToList();

            return View(model);
        }

        public ActionResult ProductList()
        {
            var sanPhams = db.SanPhams
                .Include(s => s.DanhMucSanPham)
                .Include(s => s.NhaCungCap);

            return View(sanPhams.ToList());
        }


        // GET: SanPhams/Details/5
        public ActionResult ProductDetails(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            SanPham sanPham = db.SanPhams.Find(id);
            if (sanPham == null)
            {
                return HttpNotFound();
            }
            return View(sanPham);
        }

        // GET: SanPhams/Create
        public ActionResult ProductCreate()
        {
            ViewBag.MaDM = new SelectList(db.DanhMucSanPhams, "MaDM", "TenDanhMuc");
            ViewBag.MaNCC = new SelectList(db.NhaCungCaps, "MaNCC", "TenNCC");
            ViewBag.MaSP = new SelectList(db.SanPhams, "MaSP", "MaDM");
            ViewBag.MaSP = new SelectList(db.SanPhams, "MaSP", "MaDM");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ProductCreate(SanPham sanPham, HttpPostedFileBase ImageUpload)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    if (ImageUpload != null && ImageUpload.ContentLength > 0)
                    {
                        // 1. Tạo tên file duy nhất để tránh trùng lặp
                        string fileName = System.IO.Path.GetFileNameWithoutExtension(ImageUpload.FileName);
                        string extension = System.IO.Path.GetExtension(ImageUpload.FileName);
                        fileName = fileName + "_" + DateTime.Now.ToString("yyyyMMddHHmmss") + extension;

                        // 2. Xác định thư mục lưu trữ
                        string folderPath = Server.MapPath("~/Images/Products/");

                        // 3. Kiểm tra và tạo thư mục nếu chưa tồn tại
                        if (!System.IO.Directory.Exists(folderPath))
                        {
                            System.IO.Directory.CreateDirectory(folderPath);
                        }

                        // 4. Lưu file
                        string path = System.IO.Path.Combine(folderPath, fileName);
                        ImageUpload.SaveAs(path);

                        // 5. Gán tên file vào model
                        sanPham.AnhSP = fileName;
                    }
                    else
                    {
                        sanPham.AnhSP = "default.png";
                    }

                    db.SanPhams.Add(sanPham);
                    db.SaveChanges();

                    return RedirectToAction("ProductList");
                }
                catch (Exception ex)
                {
                    // Ghi log lỗi nếu cần thiết
                    ModelState.AddModelError("", "Đã xảy ra lỗi khi lưu sản phẩm: " + ex.Message);
                }
            }


            return View(sanPham);
        }

        // GET: SanPhams/Edit/5
        public ActionResult ProductEdit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            SanPham sanPham = db.SanPhams.Find(id);
            if (sanPham == null)
            {
                return HttpNotFound();
            }

            // Tạo danh sách chọn cho Dropdown
            ViewBag.MaDM = new SelectList(db.DanhMucSanPhams, "MaDM", "TenDanhMuc", sanPham.MaDM);
            ViewBag.MaNCC = new SelectList(db.NhaCungCaps, "MaNCC", "TenNCC", sanPham.MaNCC);
            // (Đã xóa dòng ViewBag.MaSP bị trùng lặp thừa)

            return View(sanPham);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ProductEdit(SanPham sanPham, HttpPostedFileBase ImageUpload)
        {
            if (ModelState.IsValid)
            {
                // --- XỬ LÝ ẢNH ---
                if (ImageUpload != null && ImageUpload.ContentLength > 0)
                {
                    string finalFileName = "";
                    string uploadFolder = Server.MapPath("~/Images/Products/");

                    // Đảm bảo thư mục tồn tại
                    if (!Directory.Exists(uploadFolder))
                    {
                        Directory.CreateDirectory(uploadFolder);
                    }

                    // TRƯỜNG HỢP 1: Sản phẩm ĐÃ CÓ ảnh cũ
                    if (!string.IsNullOrEmpty(sanPham.AnhSP))
                    {
                        string oldBaseName = Path.GetFileNameWithoutExtension(sanPham.AnhSP);
                        string newExtension = Path.GetExtension(ImageUpload.FileName);
                        finalFileName = oldBaseName + newExtension;

                        string oldFullPath = Path.Combine(uploadFolder, sanPham.AnhSP);

                        // Nếu đuôi ảnh thay đổi, xóa ảnh cũ
                        if (finalFileName != sanPham.AnhSP && System.IO.File.Exists(oldFullPath))
                        {
                            System.IO.File.Delete(oldFullPath);
                        }
                    }
                    // TRƯỜNG HỢP 2: Sản phẩm CHƯA CÓ ảnh
                    else
                    {
                        finalFileName = "SP_" + DateTime.Now.Ticks + Path.GetExtension(ImageUpload.FileName);
                    }

                    // Đường dẫn lưu file
                    string savePath = Path.Combine(uploadFolder, finalFileName);

                    // ⭐ QUAN TRỌNG: Lưu file lên server
                    ImageUpload.SaveAs(savePath);

                    // Cập nhật tên file vào database
                    sanPham.AnhSP = finalFileName;
                }
                // (Nếu không chọn ảnh mới, giữ nguyên ảnh cũ)

                // --- LƯU DATABASE ---
                db.Entry(sanPham).State = EntityState.Modified;
                db.SaveChanges();

                return RedirectToAction("ProductList");
            }

            // Nếu form lỗi, load lại dropdown
            ViewBag.MaDM = new SelectList(db.DanhMucSanPhams, "MaDM", "TenDanhMuc", sanPham.MaDM);
            ViewBag.MaNCC = new SelectList(db.NhaCungCaps, "MaNCC", "TenNCC", sanPham.MaNCC);
            return View(sanPham);
        }



        // GET: SanPhams/Delete/5
        // GET: Admin/ProductDelete/5
        // Hiển thị trang xác nhận xóa
        public ActionResult ProductDelete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            SanPham sanPham = db.SanPhams.Find(id);
            if (sanPham == null)
            {
                return HttpNotFound();
            }
            return View(sanPham);
        }

        // POST: Admin/ProductDelete/5
        // Thực hiện xóa sau khi bấm nút xác nhận
        [HttpPost, ActionName("ProductDelete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            SanPham sanPham = db.SanPhams.Find(id);

            // 1. XÓA FILE ẢNH TRÊN SERVER (Dọn rác)
            if (sanPham != null && !string.IsNullOrEmpty(sanPham.AnhSP))
            {
                string imagePath = Path.Combine(Server.MapPath("~/Images/Products/"), sanPham.AnhSP);
                if (System.IO.File.Exists(imagePath))
                {
                    try
                    {
                        System.IO.File.Delete(imagePath);
                    }
                    catch
                    {
                        // Nếu xóa ảnh lỗi thì bỏ qua, vẫn xóa data trong DB
                    }
                }
            }

            // 2. XÓA DỮ LIỆU TRONG DATABASE
            db.SanPhams.Remove(sanPham);
            db.SaveChanges();

            // 3. Quay về trang danh sách
            return RedirectToAction("ProductList");
        }
        protected override void Dispose(bool disposing)
        {
            if (disposing)
                db.Dispose();
            base.Dispose(disposing);
        }

    }
}