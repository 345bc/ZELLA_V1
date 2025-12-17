using ESHOPPER.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using System.Data.Entity;
using System.Drawing; // Để xử lý ảnh
using System.Drawing.Drawing2D; // Để chỉnh chất lượng ảnh

namespace ESHOPPER.Controllers.Admin
{

    public class AdminController : Controller
    {
        QlyFashionShopEntities db = new QlyFashionShopEntities();


        // GET: Admin
        public ActionResult Dashboard()
        {


            //var vm = new AdminViewModel
            //{
            //    //var ProductCount = db.DanhMucSanPham.Count
            //    // 2. TỔNG SỐ ĐƠN HÀNG
            //    //OrderCount = db.DonHangs.Count(),

            //    //// 3. TỔNG SỐ KHÁCH HÀNG (Giả sử Role "Customer")
            //    //CustomerCount = db.Users.Count(u => u.Role == "Customer"),

            //    //// 4. TỔNG DOANH THU (Chỉ tính đơn hàng đã giao)
            //    //TotalRevenue = db.DonHangs
            //    //               .Where(dh => dh.TrangThai == "Đã giao")
            //    //               .Sum(dh => (decimal?)dh.TongTien) ?? 0,

            //    //// 5. ĐƠN HÀNG GẦN ĐÂY (Cho bảng phía dưới)
            //    //RecentOrders = db.DonHangs
            //    //               .OrderByDescending(dh => dh.NgayDat)
            //    //               .Take(5) // Chỉ lấy 5 đơn hàng
            //    //               .ToList(),

            //    //// 6. THỐNG KÊ NHANH (Ví dụ)
            //    //PendingOrders = db.DonHangs.Count(dh => dh.TrangThai == "Đang xử lý"),  
            //    //TopSellingProduct = "Áo Sơ Mi",
            //    //NewCustomersThisMonth = db.Users
            //    //                    .Count(u => u.Role == "Customer" &&
            //    //                                u.CreatedAt.Value.Month == System.DateTime.Now.Month &&
            //    //                                u.CreatedAt.Value.Year == System.DateTime.Now.Year)
            //};

            return View();
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
        public ActionResult ProductCreate([Bind(Include = "MaSP,MaDM,TenSanPham,MoTa,GiaBanLe,TrangThai,AnhSP,MaNCC")] SanPham sanPham)
        {
            if (ModelState.IsValid)
            {
                db.SanPhams.Add(sanPham);
                db.SaveChanges();
                return RedirectToAction("ProductList");
            }

            ViewBag.MaDM = new SelectList(db.DanhMucSanPhams, "MaDM", "TenDanhMuc", sanPham.MaDM);
            ViewBag.MaNCC = new SelectList(db.NhaCungCaps, "MaNCC", "TenNCC", sanPham.MaNCC);
            ViewBag.MaSP = new SelectList(db.SanPhams, "MaSP", "MaDM", sanPham.MaSP);
            ViewBag.MaSP = new SelectList(db.SanPhams, "MaSP", "MaDM", sanPham.MaSP);
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

        // POST: Admin/ProductEdit/5
        // POST: Admin/ProductEdit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ProductEdit(SanPham sanPham, HttpPostedFileBase ImageUpload)
        {
            if (ModelState.IsValid)
            {
                // --- XỬ LÝ ẢNH: ĐỔI TÊN THEO ẢNH CŨ & RESIZE ---

                // Kiểm tra nếu người dùng CÓ chọn file ảnh mới
                if (ImageUpload != null && ImageUpload.ContentLength > 0)
                {
                    string finalFileName = ""; // Tên file cuối cùng sẽ lưu

                    // TRƯỜNG HỢP 1: Sản phẩm ĐÃ CÓ ảnh cũ
                    if (!string.IsNullOrEmpty(sanPham.AnhSP))
                    {
                        // 1.1. Lấy tên gốc của ảnh cũ (bỏ đuôi .jpg/.png đi)
                        // Ví dụ: ảnh cũ là "ao-thun-xanh.jpg" -> lấy được "ao-thun-xanh"
                        string oldBaseName = Path.GetFileNameWithoutExtension(sanPham.AnhSP);

                        // 1.2. Lấy đuôi mở rộng của ảnh MỚI vừa up lên
                        // Ví dụ: up ảnh PNG -> lấy được ".png"
                        string newExtension = Path.GetExtension(ImageUpload.FileName);

                        // 1.3. Ghép lại thành tên file mới (Tên cũ + Đuôi mới)
                        finalFileName = oldBaseName + newExtension;

                        // 1.4. Dọn dẹp: Nếu đuôi ảnh thay đổi (vd: jpg -> png), cần xóa file jpg cũ đi để tránh rác
                        string oldFullPath = Path.Combine(Server.MapPath("~/Images/Products/"), sanPham.AnhSP);
                        // Chỉ xóa nếu tên mới khác tên cũ (nghĩa là khác đuôi)
                        if (finalFileName != sanPham.AnhSP && System.IO.File.Exists(oldFullPath))
                        {
                            System.IO.File.Delete(oldFullPath);
                        }
                    }
                    // TRƯỜNG HỢP 2: Sản phẩm CHƯA CÓ ảnh (lần đầu up)
                    else
                    {
                        // Tạo một tên ngẫu nhiên để tránh trùng
                        finalFileName = "SP_" + DateTime.Now.Ticks + Path.GetExtension(ImageUpload.FileName);
                    }

                    // 2. Xác định đường dẫn lưu file trên server
                    string savePath = Path.Combine(Server.MapPath("~/Images/Products/"), finalFileName);

                    

                    // 4. Cập nhật tên file mới nhất vào database
                    sanPham.AnhSP = finalFileName;
                }

                // (Nếu không chọn ảnh mới, sanPham.AnhSP vẫn giữ nguyên giá trị cũ nhờ HiddenFor trong View)

                // --- KẾT THÚC XỬ LÝ ẢNH ---

                db.Entry(sanPham).State = EntityState.Modified;
                db.SaveChanges();
                // Sửa lại tên Action bạn muốn quay về (ví dụ ProductList hoặc Index)
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