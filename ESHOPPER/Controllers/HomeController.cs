using ESHOPPER.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Data.Entity;

namespace ESHOPPER.Controllers.WebPage
{
    public class HomeController : Controller
    {
        QlyFashionShopEntities db = new QlyFashionShopEntities();

        public ActionResult Index()
        {
            var vm = new HomeViewModel
            {
                introes = db.Introes.ToList(),
                DanhMucSanPhams = db.DanhMucSanPhams.ToList(),
                nhaCungCaps = db.NhaCungCaps.ToList(),
                SanPhams = db.SanPhams
                        .OrderByDescending(p => p.MaSP) // MaSP giờ là int, sort vẫn ok
                        .Take(16)
                        .ToList(),
                SanPhamNgauNhiens = db.SanPhams
                                .OrderBy(r => Guid.NewGuid())
                                .Take(8)
                                .ToList()
            };

            return View(vm);
        }

        [ChildActionOnly]
        public ActionResult CategoryMenu()
        {
            var model = db.DanhMucSanPhams.ToList();
            return PartialView("ParCategories", model);
        }

        public ActionResult About()
        {
            ViewBag.Message = "Your application description page.";
            return View();
        }

        public ActionResult Contact()
        {
            var model = new HomeViewModel
            {
                DanhMucSanPhams = db.DanhMucSanPhams.ToList()
            };
            return View(model);
        }

        // [SỬA]: categoryId đổi sang int?
        public ActionResult Shop(string searchString, string sortOrder, string priceRange, int? categoryId, int page = 1)
        {
            int pageSize = 9;
            var products = db.SanPhams.AsQueryable();

            var categories = db.DanhMucSanPhams.ToList();

            // [SỬA]: So sánh int với int (nullable)
            if (categoryId.HasValue)
            {
                products = products.Where(p => p.MaDM == categoryId.Value);
            }

            if (!string.IsNullOrEmpty(searchString))
            {
                products = products.Where(p => p.TenSanPham.Contains(searchString));
            }

            if (!string.IsNullOrEmpty(priceRange))
            {
                switch (priceRange)
                {
                    case "0-100": products = products.Where(p => p.GiaBanLe >= 0 && p.GiaBanLe < 100); break;
                    case "100-200": products = products.Where(p => p.GiaBanLe >= 100 && p.GiaBanLe < 200); break;
                        // Các case giá tiền cần điều chỉnh lại cho phù hợp thực tế (vì DB để giá hàng triệu)
                        // Ví dụ demo giữ nguyên logic cũ
                }
            }

            switch (sortOrder)
            {
                case "price_asc": products = products.OrderBy(p => p.GiaBanLe); break;
                case "price_desc": products = products.OrderByDescending(p => p.GiaBanLe); break;
                default: products = products.OrderByDescending(p => p.MaSP); break;
            }

            int totalProducts = products.Count();
            int totalPages = (int)Math.Ceiling((double)totalProducts / pageSize);
            var displayProducts = products.Skip((page - 1) * pageSize).Take(pageSize).ToList();

            var model = new ShopViewModel
            {
                SanPhams = displayProducts,
                Categories = categories,
                CurrentCategoryId = categoryId, // ViewModel cần sửa property này sang int?
                SearchString = searchString,
                SortOrder = sortOrder,
                PriceRange = priceRange,
                PageNumber = page,
                PageSize = pageSize,
                TotalPages = totalPages
            };

            return View(model);
        }

        // [SỬA]: id đổi sang int
        // GET: ProductDetails/5
        public ActionResult ProductDetails(int id)
        {
            // 1. Lấy thông tin sản phẩm chính
            var sanPham = db.SanPhams.FirstOrDefault(s => s.MaSP == id);
            if (sanPham == null)
            {
                return HttpNotFound();
            }

            // 2. Lấy danh sách tất cả biến thể của sản phẩm này
            var listBienThe = db.BienTheSanPhams.Where(b => b.MaSP == id).ToList();

            // 3. Lấy danh sách các Mã Size và Mã Màu xuất hiện trong biến thể (loại bỏ null)
            var sizeIds = listBienThe.Where(b => b.MaSize.HasValue).Select(b => b.MaSize.Value).Distinct().ToList();
            var colorIds = listBienThe.Where(b => b.MaMau.HasValue).Select(b => b.MaMau.Value).Distinct().ToList();

            // 4. Truy vấn bảng KichThuoc và MauSac dựa trên danh sách ID đã lấy
            // Điều này thay thế cho việc dùng .Include() khi Model không có Navigation Property
            var sizes = db.KichThuocs.Where(s => sizeIds.Contains(s.MaSize)).OrderBy(s => s.TenSize).ToList();
            var colors = db.MauSacs.Where(c => colorIds.Contains(c.MaMau)).ToList();

            // 5. Lấy sản phẩm gợi ý ngẫu nhiên
            var randomProducts = db.SanPhams
                .Where(s => s.MaSP != id && s.TrangThai == "Hoạt động")
                .OrderBy(r => Guid.NewGuid())
                .Take(4)
                .ToList();

            // 6. Đóng gói vào ViewModel
            var viewModel = new ProductDetailsViewModel
            {
                SanPhamChinh = sanPham,
                CacSizeDuyNhat = sizes,
                CacMauDuyNhat = colors,
                SanPhamNgauNhiens = randomProducts,
                DanhSachBienThe = listBienThe // Đảm bảo ViewModel đã có thuộc tính này
            };

            return View(viewModel);
        }

        public ActionResult Cart()
        {
            List<ChiTietGioHang> danhSach = new List<ChiTietGioHang>();

            if (Session["MaKH"] != null)
            {
                // Lấy từ Database
                int maKH = (int)Session["MaKH"];
                var gioHang = db.GioHangs.FirstOrDefault(g => g.MaKH == maKH);
                if (gioHang != null)
                {
                    danhSach = db.ChiTietGioHangs
                                 .Where(c => c.MaGioHang == gioHang.MaGioHang)
                                 .Include("BienTheSanPham.SanPham")
                                 .ToList();
                }
            }
            else
            {
                // Lấy từ Session (Khách vãng lai)
                danhSach = Session["Cart"] as List<ChiTietGioHang> ?? new List<ChiTietGioHang>();
            }

            // Truyền trực tiếp danh sách sang View
            return View(danhSach);
        }

        // [SỬA]: Các tham số chuyển sang int
        [HttpPost]
[ValidateAntiForgeryToken]
public ActionResult AddToCart(int productId, int quantity = 1, int? selectedSize = null, int? selectedColor = null)
{
    try
    {
        // 1. Kiểm tra số lượng hợp lệ
        if (quantity < 1) quantity = 1;

        // 2. Kiểm tra sản phẩm gốc tồn tại
        var sanPham = db.SanPhams.FirstOrDefault(p => p.MaSP == productId);
        if (sanPham == null)
        {
            TempData["ErrorMessage"] = "Sản phẩm không tồn tại.";
            return RedirectToAction("Index", "Home");
        }

        // 3. Tìm biến thể (Variant) dựa trên Màu và Size đã chọn
        var bienThe = db.BienTheSanPhams.FirstOrDefault(b =>
            b.MaSP == productId &&
            b.MaSize == selectedSize &&
            b.MaMau == selectedColor);

        // Nếu khách chưa chọn đủ thuộc tính dẫn đến không xác định được biến thể
        if (bienThe == null)
        {
            TempData["ErrorMessage"] = "Vui lòng chọn đầy đủ Kích thước và Màu sắc hợp lệ.";
            return RedirectToAction("ProductDetails", new { id = productId });
        }

        // Xác định đơn giá (Ưu tiên giá biến thể, nếu không có lấy giá lẻ sản phẩm)
        decimal donGia = bienThe.GiaBan ?? sanPham.GiaBanLe ?? 0;

        // --- TRƯỜNG HỢP 1: NGƯỜI DÙNG ĐÃ ĐĂNG NHẬP (Lưu vào Database) ---
        if (Session["MaKH"] != null)
        {
            int maKH = (int)Session["MaKH"];

            // Tìm hoặc tạo mới Giỏ hàng cho khách hàng
            var gioHang = db.GioHangs.FirstOrDefault(g => g.MaKH == maKH);
            if (gioHang == null)
            {
                gioHang = new GioHang { MaKH = maKH, NgayTao = DateTime.Now };
                db.GioHangs.Add(gioHang);
                db.SaveChanges();
            }

            // Tìm chi tiết giỏ hàng theo MaBienThe (Cấu trúc mới tập trung vào MaBienThe)
            var chiTiet = db.ChiTietGioHangs.FirstOrDefault(c =>
                c.MaGioHang == gioHang.MaGioHang &&
                c.MaBienThe == bienThe.MaBienThe);

            if (chiTiet != null)
            {
                chiTiet.SoLuong += quantity;
                chiTiet.DonGia = donGia; // Cập nhật lại giá mới nhất
            }
            else
            {
                db.ChiTietGioHangs.Add(new ChiTietGioHang
                {
                    MaGioHang = gioHang.MaGioHang,
                    MaBienThe = bienThe.MaBienThe, // Chỉ lưu MaBienThe theo đúng Model mới
                    SoLuong = quantity,
                    DonGia = donGia
                });
            }
            db.SaveChanges();
        }
                // Trong HomeController.cs -> AddToCart
                else // Trường hợp Khách vãng lai
                {
                    List<ChiTietGioHang> cart = Session["Cart"] as List<ChiTietGioHang> ?? new List<ChiTietGioHang>();
                    var existingItem = cart.FirstOrDefault(x => x.MaBienThe == bienThe.MaBienThe);

                    if (existingItem != null)
                    {
                        existingItem.SoLuong += quantity;
                    }
                    else
                    {
                        var newItem = new ChiTietGioHang
                        {
                            MaBienThe = bienThe.MaBienThe,
                            SoLuong = quantity,
                            DonGia = donGia,
                            // Nạp object để View hiển thị được Tên/Ảnh qua Navigation Property
                            BienTheSanPham = bienThe
                        };

                        // Đảm bảo nạp dữ liệu Sản phẩm cha
                        if (newItem.BienTheSanPham.SanPham == null)
                            newItem.BienTheSanPham.SanPham = sanPham;

                        cart.Add(newItem);
                    }
                    Session["Cart"] = cart;
                }

                TempData["SuccessMessage"] = "Đã thêm sản phẩm vào giỏ hàng thành công!";
        return RedirectToAction("ProductDetails", new { id = productId });
    }
    catch (Exception ex)
    {
        TempData["ErrorMessage"] = "Có lỗi xảy ra: " + ex.Message;
        return RedirectToAction("ProductDetails", new { id = productId });
    }
}

        private int GetCartTotalItems()
        {
            if (Session["MaKH"] != null)
            {
                int maKH = (int)Session["MaKH"];
                var gioHang = db.GioHangs.FirstOrDefault(g => g.MaKH == maKH);
                return gioHang?.ChiTietGioHangs.Sum(c => c.SoLuong) ?? 0;
            }
            else
            {
                var gioHang = Session["Cart"] as GioHang;
                return gioHang?.TongSoLuong() ?? 0; // Giả sử TongSoLuong() đã sửa logic tính toán
            }
        }

        // [SỬA]: Tham số int
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult UpdateCart(int id, int quantity)
        {
            // 1. Đảm bảo số lượng tối thiểu là 1
            if (quantity < 1) quantity = 1;

            // --- TRƯỜNG HỢP 1: NGƯỜI DÙNG ĐÃ ĐĂNG NHẬP (Cập nhật Database) ---
            if (Session["MaKH"] != null)
            {
                int maKH = (int)Session["MaKH"];
                // Tìm giỏ hàng của người dùng này
                var gioHang = db.GioHangs.FirstOrDefault(g => g.MaKH == maKH);

                if (gioHang != null)
                {
                    // Tìm dòng chi tiết dựa vào MaGioHang và MaBienThe (định danh duy nhất)
                    var chiTiet = db.ChiTietGioHangs.FirstOrDefault(c =>
                        c.MaGioHang == gioHang.MaGioHang &&
                        c.MaBienThe == id);

                    if (chiTiet != null)
                    {
                        chiTiet.SoLuong = quantity;
                        db.SaveChanges();
                    }
                }
            }
            // --- TRƯỜNG HỢP 2: KHÁCH VÃNG LAI (Cập nhật Session) ---
            else
            {
                // Lấy danh sách ChiTietGioHang từ Session (đã sửa ở bước AddToCart)
                List<ChiTietGioHang> cart = Session["Cart"] as List<ChiTietGioHang>;

                if (cart != null)
                {
                    // Tìm sản phẩm trong danh sách dựa trên MaBienThe
                    var item = cart.FirstOrDefault(i => i.MaBienThe == id);
                    if (item != null)
                    {
                        item.SoLuong = quantity;
                    }
                }
            }

            // Chuyển hướng về trang Index của Controller Cart để thấy thay đổi
            return RedirectToAction("Index", "Cart");
        }

        // [SỬA]: Tham số int
        public ActionResult RemoveFromCart(int id)
        {
            // --- TRƯỜNG HỢP 1: NGƯỜI DÙNG ĐÃ ĐĂNG NHẬP (Xóa trong Database) ---
            if (Session["MaKH"] != null)
            {
                int maKH = (int)Session["MaKH"];
                // Tìm giỏ hàng của người dùng
                var gioHang = db.GioHangs.FirstOrDefault(g => g.MaKH == maKH);

                if (gioHang != null)
                {
                    // Tìm dòng chi tiết khớp với MaGioHang và MaBienThe
                    var chiTiet = db.ChiTietGioHangs.FirstOrDefault(c =>
                        c.MaGioHang == gioHang.MaGioHang &&
                        c.MaBienThe == id);

                    if (chiTiet != null)
                    {
                        db.ChiTietGioHangs.Remove(chiTiet);
                        db.SaveChanges();
                    }
                }
            }
            // --- TRƯỜNG HỢP 2: KHÁCH VÃNG LAI (Xóa trong Session) ---
            else
            {
                // Lấy danh sách ChiTietGioHang từ Session (đồng bộ với logic List đã sửa trước đó)
                List<ChiTietGioHang> cart = Session["Cart"] as List<ChiTietGioHang>;

                if (cart != null)
                {
                    // Tìm item cần xóa dựa trên MaBienThe
                    var itemToRemove = cart.FirstOrDefault(i => i.MaBienThe == id);

                    if (itemToRemove != null)
                    {
                        cart.Remove(itemToRemove);
                    }

                    // Cập nhật lại Session sau khi xóa
                    Session["Cart"] = cart;
                }
            }

            // Chuyển hướng về trang Index của giỏ hàng
            return RedirectToAction("Cart", "Home");
        }

        [ChildActionOnly]
        public ActionResult CartSummary()
        {
            int total = 0;
            if (Session["MaKH"] != null)
            {
                int maKH = (int)Session["MaKH"];
                var gioHang = db.GioHangs.FirstOrDefault(g => g.MaKH == maKH);
                if (gioHang != null)
                {
                    total = (int)(db.ChiTietGioHangs
                                    .Where(c => c.MaGioHang == gioHang.MaGioHang)
                                    .Sum(c => c.SoLuong) ?? 0);
                }
            }
            else
            {
                var gioHang = Session["Cart"] as GioHang;
                if (gioHang != null)
                {
                    // total = gioHang.TongSoLuong();
                    total = gioHang.ChiTietGioHangs.Sum(c => c.SoLuong) ?? 0;
                }
            }
            ViewBag.Quantity = total;
            return PartialView("_CartSummary");
        }

        // [SỬA]: Hàm này xử lý xóa khỏi giỏ sau khi mua
        private void XoaSanPhamDaMuaKhoiGio(int maDH)
        {
            // Lấy chi tiết đơn hàng (lưu ý: Bảng này lưu ID SP nhưng lưu TÊN size/màu dạng text)
            // Tuy nhiên, để xóa khỏi giỏ (dùng ID), ta cần map lại hoặc giả định lúc thanh toán
            // ta đã lưu thông tin để đối chiếu.
            // CACH TOT NHAT: Khi thanh toán thành công, ta nên dùng list `ChiTietGioHang` đã select
            // để xóa, thay vì query ngược lại từ DonHang (vì DonHang lưu text).
            // NHƯNG để sửa ít nhất, ta sẽ cập nhật logic ở Checkout thay vì hàm này.
            // -> Xem phần Checkout bên dưới.
        }

        [HttpGet]
        public ActionResult Checkout(string selectedIds) // selectedIds truyền vào là chuỗi: "12,15,18" (các MaBienThe)
        {
            List<ChiTietGioHang> danhSachThanhToan = new List<ChiTietGioHang>();
            int? maKH = Session["MaKH"] as int?;

            // 1. Kiểm tra đầu vào
            if (string.IsNullOrEmpty(selectedIds))
            {
                TempData["ErrorMessage"] = "Vui lòng chọn sản phẩm để thanh toán.";
                return RedirectToAction("Index", "Cart");
            }

            // Chuyển chuỗi MaBienThe thành danh sách int
            var listMaBienThe = selectedIds.Split(',')
                                           .Select(id => int.Parse(id))
                                           .ToList();

            // 2. Lấy dữ liệu nguồn dựa trên trạng thái đăng nhập
            if (maKH.HasValue)
            {
                // Lấy từ Database
                var gioHang = db.GioHangs.FirstOrDefault(g => g.MaKH == maKH.Value);
                if (gioHang != null)
                {
                    danhSachThanhToan = db.ChiTietGioHangs
                        .Where(c => c.MaGioHang == gioHang.MaGioHang && listMaBienThe.Contains(c.MaBienThe.Value))
                        .Include(c => c.BienTheSanPham.SanPham)
                        .Include(c => c.BienTheSanPham.MauSac)   // Giả sử Model có navigation
                        .Include(c => c.BienTheSanPham.KichThuoc) // Giả sử Model có navigation
                        .ToList();
                }
            }
            else
            {
                // Lấy từ Session (Sử dụng List<ChiTietGioHang> như đã sửa ở hàm AddToCart)
                List<ChiTietGioHang> cartSession = Session["Cart"] as List<ChiTietGioHang>;
                if (cartSession != null)
                {
                    danhSachThanhToan = cartSession
                        .Where(c => listMaBienThe.Contains(c.MaBienThe.Value))
                        .ToList();
                }
            }

            // 3. Kiểm tra danh sách sau lọc
            if (!danhSachThanhToan.Any())
            {
                TempData["ErrorMessage"] = "Không tìm thấy sản phẩm hợp lệ để thanh toán.";
                return RedirectToAction("Index", "Cart");
            }

            // 4. Khởi tạo đối tượng Đơn Hàng để mang sang View
            var donHang = new DonHang
            {
                NgayDat = DateTime.Now,
                TongTien = danhSachThanhToan.Sum(x => (x.DonGia ?? 0) * (x.SoLuong ?? 0))
            };

            // Điền trước thông tin nếu đã đăng nhập
            if (maKH.HasValue)
            {
                var khach = db.KhachHangs.Find(maKH.Value);
                if (khach != null)
                {
                    donHang.MaKH = maKH.Value;
                    donHang.TenNguoiNhan = khach.TenKH;
                    donHang.SDTNguoiNhan = khach.SoDT;
                }
            }

            ViewBag.ListThanhToan = danhSachThanhToan;
            ViewBag.SelectedIds = selectedIds; // Giữ lại để dùng cho bước tạo đơn hàng chính thức (POST)

            return View(donHang);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Checkout(DonHang model, string paymentMethod, string selectedIds)
        {
            // 1. Khởi tạo danh sách sản phẩm sẽ mua
            List<ChiTietGioHang> itemsToBuy = new List<ChiTietGioHang>();
            int? maKH = Session["MaKH"] as int?;

            if (string.IsNullOrEmpty(selectedIds))
            {
                TempData["ErrorMessage"] = "Vui lòng chọn sản phẩm để thanh toán.";
                return RedirectToAction("Index", "Cart");
            }

            // Chuyển chuỗi MaBienThe từ "12,15,18" thành List<int>
            var listMaBienThe = selectedIds.Split(',').Select(id => int.Parse(id)).ToList();

            // 2. Lấy dữ liệu nguồn để kiểm tra và lưu đơn hàng
            if (maKH.HasValue)
            {
                // Lấy từ Database kèm theo các quan hệ để lấy thông tin Tên/Size/Màu
                var gioHang = db.GioHangs.FirstOrDefault(g => g.MaKH == maKH.Value);
                if (gioHang != null)
                {
                    itemsToBuy = db.ChiTietGioHangs
                        .Where(c => c.MaGioHang == gioHang.MaGioHang && listMaBienThe.Contains(c.MaBienThe.Value))
                        .Include(c => c.BienTheSanPham.SanPham)
                        .Include(c => c.BienTheSanPham.MauSac)
                        .Include(c => c.BienTheSanPham.KichThuoc)
                        .ToList();
                }
            }
            else
            {
                // Lấy từ Session List<ChiTietGioHang>
                List<ChiTietGioHang> cartSession = Session["Cart"] as List<ChiTietGioHang>;
                if (cartSession != null)
                {
                    itemsToBuy = cartSession.Where(i => listMaBienThe.Contains(i.MaBienThe.Value)).ToList();
                }
            }

            if (itemsToBuy.Count == 0)
            {
                TempData["ErrorMessage"] = "Giỏ hàng không hợp lệ.";
                return RedirectToAction("Index", "Cart");
            }

            // 3. Cập nhật thông tin Header đơn hàng
            model.NgayDat = DateTime.Now;
            model.TongTien = itemsToBuy.Sum(x => (x.DonGia ?? 0) * (x.SoLuong ?? 0));
            if (maKH.HasValue) model.MaKH = maKH.Value;

            // Trạng thái: 1 - Đang chờ thanh toán (VNPay), 2 - Đang xử lý (COD)
            model.TrangThai = (paymentMethod == "VNPay") ? 1 : 2;

            // Lưu Header DonHang trước để lấy MaDH
            db.DonHangs.Add(model);
            db.SaveChanges();

            // 4. Lưu Chi tiết đơn hàng (ChiTietDonHang)
            foreach (var item in itemsToBuy)
            {
                var chiTiet = new ChiTietDonHang
                {
                    MaDH = model.MaDH,
                    MaBienThe = item.MaBienThe,
                    SoLuong = item.SoLuong,
                    DonGia = item.DonGia,
                    // Lưu text để làm lịch sử (đề phòng biến thể hoặc sản phẩm bị xóa sau này)
                    //TenSP = item.BienTheSanPham?.SanPham?.TenSanPham ?? "Sản phẩm không xác định",
                    //Size = item.BienTheSanPham?.KichThuoc?.TenSize ?? "N/A",
                    //Mau = item.BienTheSanPham?.MauSac?.TenMau ?? "N/A"
                };
                db.ChiTietDonHangs.Add(chiTiet);
            }
            db.SaveChanges();

            // 5. Xử lý thanh toán và Xóa giỏ hàng
            if (paymentMethod == "VNPay")
            {
                // return Redirect(CreateVnpayUrl(model)); // Giả định bạn đã có hàm tạo link VNPay
                return Content("Chuyển hướng VNPay...");
            }
            else
            {
                // Xóa các item đã mua khỏi giỏ hàng
                if (maKH.HasValue)
                {
                    db.ChiTietGioHangs.RemoveRange(itemsToBuy);
                    db.SaveChanges();
                }
                else
                {
                    var gh = Session["Cart"] as List<ChiTietGioHang>;
                    if (gh != null)
                    {
                        gh.RemoveAll(x => listMaBienThe.Contains(x.MaBienThe.Value));
                        Session["Cart"] = gh;
                    }
                }
                TempData["SuccessMessage"] = "Đặt hàng thành công!";
                return RedirectToAction("OrderSuccess");
            }
        }

        private string CreateVnpayUrl(DonHang order)
        {
            string vnp_TmnCode = "QE91CB08";
            string vnp_HashSecret = "R08TH1P0J1N4T0L63X0Q3KDTFWBYS8YT";
            string vnp_Url = "https://sandbox.vnpayment.vn/paymentv2/vpcpay.html";
            string vnp_Returnurl = Url.Action("PaymentCallback", "Home", null, Request.Url.Scheme);

            VnPayLibrary vnpay = new VnPayLibrary();
            long amount = (long)(order.TongTien * 100);

            vnpay.AddRequestData("vnp_Version", "2.1.0");
            vnpay.AddRequestData("vnp_Command", "pay");
            vnpay.AddRequestData("vnp_TmnCode", vnp_TmnCode);
            vnpay.AddRequestData("vnp_Amount", amount.ToString());
            vnpay.AddRequestData("vnp_CreateDate", DateTime.Now.ToString("yyyyMMddHHmmss"));
            vnpay.AddRequestData("vnp_CurrCode", "VND");
            vnpay.AddRequestData("vnp_IpAddr", Request.UserHostAddress);
            vnpay.AddRequestData("vnp_Locale", "vn");
            vnpay.AddRequestData("vnp_OrderInfo", "Thanh toan don hang " + order.MaDH);
            vnpay.AddRequestData("vnp_OrderType", "other");
            vnpay.AddRequestData("vnp_ReturnUrl", vnp_Returnurl);
            vnpay.AddRequestData("vnp_TxnRef", order.MaDH.ToString());

            return vnpay.CreateRequestUrl(vnp_Url, vnp_HashSecret);
        }

        //public ActionResult PaymentCallback()
        //{
        //    string vnp_HashSecret = "R08TH1P0J1N4T0L63X0Q3KDTFWBYS8YT";
        //    var vnpayData = Request.QueryString;
        //    VnPayLibrary vnpay = new VnPayLibrary();

        //    foreach (string s in vnpayData)
        //    {
        //        if (!string.IsNullOrEmpty(s) && s.StartsWith("vnp_"))
        //        {
        //            vnpay.AddResponseData(s, vnpayData[s]);
        //        }
        //    }

        //    string vnp_ResponseCode = vnpay.GetResponseData("vnp_ResponseCode");
        //    string vnp_TxnRef = vnpay.GetResponseData("vnp_TxnRef");
        //    string vnp_SecureHash = Request.QueryString["vnp_SecureHash"];

        //    bool checkSignature = vnpay.ValidateSignature(vnp_SecureHash, vnp_HashSecret);

        //    if (checkSignature)
        //    {
        //        if (int.TryParse(vnp_TxnRef, out int maDH))
        //        {
        //            var order = db.DonHangs.Find(maDH);
        //            if (order != null)
        //            {
        //                if (vnp_ResponseCode == "00") // THÀNH CÔNG
        //                {
        //                    order.TrangThai = 1;
        //                    db.SaveChanges();

        //                    // Logic xóa giỏ hàng cho VNPay (Xóa các món trong đơn hàng đó)
        //                    // Cần lấy lại chi tiết đơn hàng vừa tạo để biết món nào cần xóa
        //                    var itemsInOrder = db.ChiTietDonHangs.Where(d => d.MaDH == maDH).ToList();

        //                    if (Session["MaKH"] != null)
        //                    {
        //                        int maKH = (int)Session["MaKH"];
        //                        var gioHang = db.GioHangs.FirstOrDefault(g => g.MaKH == maKH);
        //                        if (gioHang != null)
        //                        {
        //                            foreach (var i in itemsInOrder)
        //                            {
        //                                // Lưu ý: i.Size và i.Mau ở đây là Text. Trong giỏ hàng lại lưu ID.
        //                                // Đây là vấn đề nan giải khi thiết kế CSDL lệch pha (Order lưu text, Cart lưu ID).
        //                                // Giải pháp tạm thời: Xóa theo MaSP (nếu mua tất cả biến thể).
        //                                // Hoặc phải truy ngược ID từ Text (rủi ro).
        //                                // Giải pháp tốt nhất: Ở bước Checkout, sau khi add DonHang, ta xóa giỏ hàng LUÔN trước khi redirect VNPay.
        //                                // Nếu thanh toán thất bại thì User phải pick lại.

        //                                // Nhưng để code chạy tạm thời với CSDL hiện tại:
        //                                var itemsToRemove = db.ChiTietGioHangs.Where(c => c.MaGioHang == gioHang.MaGioHang && c.MaSP == i.MaSP).ToList();
        //                                // Đoạn này xóa hơi "lố" (xóa hết size của sp đó), nhưng an toàn hơn việc ko xóa được.
        //                                db.ChiTietGioHangs.RemoveRange(itemsToRemove);
        //                            }
        //                            db.SaveChanges();
        //                        }
        //                    }
        //                    else
        //                    {
        //                        // Session cart
        //                        var gh = Session["Cart"] as GioHang;
        //                        if (gh != null)
        //                        {
        //                            foreach (var i in itemsInOrder)
        //                            {
        //                                // 1. Tìm các item cần xóa và chuyển sang List tạm
        //                                var itemsToRemove = gh.ChiTietGioHangs
        //                                                      .Where(x => x.MaSP == i.MaSP)
        //                                                      .ToList();

        //                                // 2. Lặp và xóa từng item khỏi collection gốc
        //                                foreach (var item in itemsToRemove)
        //                                {
        //                                    gh.ChiTietGioHangs.Remove(item);
        //                                }
        //                            }
        //                            Session["Cart"] = gh;
        //                        }
        //                    }

        //                    TempData["SuccessMessage"] = "Thanh toán thành công!";
        //                    return RedirectToAction("OrderSuccess");
        //                }
        //                else
        //                {
        //                    order.TrangThai = 0; // Hủy/Lỗi
        //                    db.SaveChanges();
        //                    TempData["PaymentError"] = "Lỗi thanh toán VNPay: Code " + vnp_ResponseCode;
        //                    return RedirectToAction("PaymentFail");
        //                }
        //            }
        //        }
        //        TempData["PaymentError"] = "Không tìm thấy đơn hàng.";
        //        return RedirectToAction("PaymentFail");
        //    }
        //    else
        //    {
        //        TempData["PaymentError"] = "Sai chữ ký bảo mật.";
        //        return RedirectToAction("PaymentFail");
        //    }
        //}

        public ActionResult OrderSuccess()
        {
            return View();
        }
        public ActionResult PaymentFail()
        {
            ViewBag.ErrorMessage = TempData["PaymentError"] ?? "Lỗi thanh toán.";
            return View();
        }
    }
}