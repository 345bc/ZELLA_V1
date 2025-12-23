using ESHOPPER.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Entity;
using System.Linq;
using System.Web;
using System.Web.Mvc;

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
                        .OrderByDescending(p => p.MaSP) 
                        .Take(16)
                        .ToList(),
                SanPhamNgauNhiens = db.SanPhams
                                .OrderBy(r => Guid.NewGuid())
                                .Take(8)
                                .ToList()
            };

            return View(vm);
        }

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
                    case "1": products = products.Where(p => p.GiaBanLe >= 100000 && p.GiaBanLe < 500000); break;
                    case "2": products = products.Where(p => p.GiaBanLe >= 500000 && p.GiaBanLe < 1000000); break;
                    case "3": products = products.Where(p => p.GiaBanLe >= 1000000 && p.GiaBanLe < 2000000); break;
                    case "4": products = products.Where(p => p.GiaBanLe >= 2000000); break;
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

            var listBienThe = db.BienTheSanPhams.Where(b => b.MaSP == id).ToList();

            // 3. Lấy danh sách các Mã Size và Mã Màu xuất hiện trong biến thể (loại bỏ null)
            var sizeIds = listBienThe.Where(b => b.MaSize.HasValue).Select(b => b.MaSize.Value).Distinct().ToList();
            var colorIds = listBienThe.Where(b => b.MaMau.HasValue).Select(b => b.MaMau.Value).Distinct().ToList();

            var sizes = db.KichThuocs.Where(s => sizeIds.Contains(s.MaSize)).OrderBy(s => s.TenSize).ToList();
            var colors = db.MauSacs.Where(c => colorIds.Contains(c.MaMau)).ToList();

            var randomProducts = db.SanPhams
                .Where(s => s.MaSP != id && s.TrangThai == "Hoạt động")
                .OrderBy(r => Guid.NewGuid())
                .Take(5)
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

                int currentCount = 0;
                if (Session["MaKH"] != null)
                {
                    int maKH = (int)Session["MaKH"];
                    var gh = db.GioHangs.FirstOrDefault(g => g.MaKH == maKH);
                    currentCount = gh?.ChiTietGioHangs.Sum(c => c.SoLuong) ?? 0;
                }
                else
                {
                    var cartList = Session["Cart"] as List<ChiTietGioHang>;
                    currentCount = cartList?.Sum(c => c.SoLuong) ?? 0;
                }

                Session["CartCount"] = currentCount;

                TempData["SuccessMessage"] = "Đã thêm sản phẩm vào giỏ hàng thành công!";
                return RedirectToAction("ProductDetails", new { id = productId });
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Có lỗi xảy ra: " + ex.Message;
                return RedirectToAction("ProductDetails", new { id = productId });
            }
        }

        // Đặt hàm này trong Controller (ví dụ: HomeController hoặc BaseController)
        public   int GetCartTotalItems()
        {
            int totalQuantity = 0;

            // TRƯỜNG HỢP 1: Đã đăng nhập (Lấy từ Database)
            if (Session["MaKH"] != null)
            {
                int maKH = (int)Session["MaKH"];

                // Tìm giỏ hàng của khách
                var gioHang = db.GioHangs.FirstOrDefault(g => g.MaKH == maKH);

                if (gioHang != null)
                {
                    // Tính tổng cột SoLuong trong bảng ChiTietGioHang
                    // Sử dụng (c.SoLuong ?? 0) để an toàn nếu cột này cho phép null trong DB
                    totalQuantity = gioHang.ChiTietGioHangs.Sum(c => c.SoLuong) ?? 0;
                }
            }
            // TRƯỜNG HỢP 2: Khách vãng lai (Lấy từ Session)
            else
            {
                // Ép kiểu Session về List<ChiTietGioHang>
                var cart = Session["Cart"] as List<ChiTietGioHang>;

                if (cart != null)
                {
                    // Tính tổng số lượng các item trong list
                    totalQuantity = cart.Sum(c => c.SoLuong) ?? 0;
                }
            }

            return totalQuantity;
        }

        // --- ACTION GỌI TỪ VIEW ---
        [ChildActionOnly]
        public ActionResult CartBadge()
        {
            int count = GetCartTotalItems();
            return PartialView("_CartBadge", count);
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
            return RedirectToAction("Cart", "Home");
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

        [HttpGet]
        [Authorize]
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
                return Redirect(CreateVnpayUrl(model)); // Giả định bạn đã có hàm tạo link VNPay
                //return Content("Chuyển hướng VNPay...");

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

        public string CreateVnpayUrl(DonHang order)
        {
            string vnp_TmnCode = ConfigurationManager.AppSettings["vnp_TmnCode"];
            string vnp_HashSecret = ConfigurationManager.AppSettings["vnp_HashSecret"];
            string vnp_Url = ConfigurationManager.AppSettings["vnp_Url"];
            string vnp_Returnurl = ConfigurationManager.AppSettings["vnp_Returnurl"];

            VnPayLibrary vnpay = new VnPayLibrary();
            // Số tiền nhân 100 theo quy tắc VNPay
            long amount = (long)(order.TongTien * 100);

            vnpay.AddRequestData("vnp_Version", "2.1.0");
            vnpay.AddRequestData("vnp_Command", "pay");
            vnpay.AddRequestData("vnp_TmnCode", vnp_TmnCode);
            vnpay.AddRequestData("vnp_Amount", amount.ToString());
            vnpay.AddRequestData("vnp_CreateDate", DateTime.Now.ToString("yyyyMMddHHmmss"));
            vnpay.AddRequestData("vnp_CurrCode", "VND");

            // Lấy IP người dùng (bắt buộc)
            vnpay.AddRequestData("vnp_IpAddr", Utils.GetIpAddress());
            vnpay.AddRequestData("vnp_Locale", "vn");
            vnpay.AddRequestData("vnp_OrderInfo", "Thanh toan don hang " + order.MaDH);
            vnpay.AddRequestData("vnp_OrderType", "other");
            vnpay.AddRequestData("vnp_ReturnUrl", vnp_Returnurl);

            // Mã tham chiếu giao dịch (phải là duy nhất)
            vnpay.AddRequestData("vnp_TxnRef", order.MaDH.ToString());

            return vnpay.CreateRequestUrl(vnp_Url, vnp_HashSecret);
        }

        // 2. XỬ LÝ KẾT QUẢ TRẢ VỀ (CALLBACK)
        public ActionResult PaymentCallback()
        {
            string vnp_HashSecret = ConfigurationManager.AppSettings["vnp_HashSecret"];
            var vnpayData = Request.QueryString;
            VnPayLibrary vnpay = new VnPayLibrary();

            // Lấy toàn bộ dữ liệu trả về để kiểm tra chữ ký
            foreach (string s in vnpayData)
            {
                if (!string.IsNullOrEmpty(s) && s.StartsWith("vnp_"))
                {
                    vnpay.AddResponseData(s, vnpayData[s]);
                }
            }

            string vnp_ResponseCode = vnpay.GetResponseData("vnp_ResponseCode");
            string vnp_TxnRef = vnpay.GetResponseData("vnp_TxnRef");
            string vnp_SecureHash = Request.QueryString["vnp_SecureHash"];

            // Kiểm tra chữ ký bảo mật (Tránh giả mạo URL)
            bool checkSignature = vnpay.ValidateSignature(vnp_SecureHash, vnp_HashSecret);

            if (checkSignature)
            {
                if (int.TryParse(vnp_TxnRef, out int maDH))
                {
                    var order = db.DonHangs.Find(maDH);
                    if (order != null)
                    {
                        if (vnp_ResponseCode == "00") // THÀNH CÔNG
                        {
                            // 1. Cập nhật trạng thái đơn hàng
                            order.TrangThai = 1; // Đã thanh toán
                            db.SaveChanges();

                            // 2. Xóa sản phẩm đã mua khỏi giỏ hàng
                            var itemsInOrder = db.ChiTietDonHangs.Where(d => d.MaDH == maDH).ToList();

                            // --- XÓA GIỎ HÀNG DB (Đã đăng nhập) ---
                            if (Session["MaKH"] != null)
                            {
                                int maKH = (int)Session["MaKH"];
                                var gioHang = db.GioHangs.FirstOrDefault(g => g.MaKH == maKH);
                                if (gioHang != null)
                                {
                                    foreach (var itemOrder in itemsInOrder)
                                    {
                                        // Tìm và xóa đúng biến thể đã mua
                                        var itemToRemove = db.ChiTietGioHangs.FirstOrDefault(c =>
                                            c.MaGioHang == gioHang.MaGioHang &&
                                            c.MaBienThe == itemOrder.MaBienThe); // Khớp MaBienThe

                                        if (itemToRemove != null)
                                        {
                                            db.ChiTietGioHangs.Remove(itemToRemove);
                                        }
                                    }
                                    db.SaveChanges();
                                }
                            }

                            // --- XÓA GIỎ HÀNG SESSION (Vãng lai) ---
                            // QUAN TRỌNG: Session Cart là List<ChiTietGioHang>, không phải GioHang Object
                            var cartSession = Session["Cart"] as List<ChiTietGioHang>;
                            if (cartSession != null)
                            {
                                foreach (var itemOrder in itemsInOrder)
                                {
                                    // Tìm item trong list session để xóa
                                    var itemToRemove = cartSession.FirstOrDefault(x => x.MaBienThe == itemOrder.MaBienThe);
                                    if (itemToRemove != null)
                                    {
                                        cartSession.Remove(itemToRemove);
                                    }
                                }
                                Session["Cart"] = cartSession; // Cập nhật lại Session
                            }

                            TempData["SuccessMessage"] = "Thanh toán thành công qua VNPay!";
                            return RedirectToAction("OrderSuccess");
                        }
                        else // THANH TOÁN THẤT BẠI (Do hủy hoặc lỗi thẻ)
                        {
                            // Không cập nhật trạng thái đơn (vẫn để là Chờ thanh toán hoặc Hủy)
                            // order.TrangThai = 0; // Tùy bạn có muốn hủy luôn đơn không
                            // db.SaveChanges();

                            TempData["PaymentError"] = "Giao dịch thất bại. Mã lỗi: " + vnp_ResponseCode;
                            return RedirectToAction("PaymentFail");
                        }
                    }
                }
            }

            TempData["PaymentError"] = "Chữ ký không hợp lệ!";
            return RedirectToAction("PaymentFail");
        }


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