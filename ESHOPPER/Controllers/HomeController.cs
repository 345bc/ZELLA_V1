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
                        .OrderByDescending(p => p.MaSP)
                        .Take(8) // lấy 8 sản phẩm mới nhất
                        .ToList(),
                SanPhamNgauNhiens = db.SanPhams
                                .OrderBy(r => Guid.NewGuid()) // sắp xếp ngẫu nhiên
                                .Take(8) // số lượng sản phẩm muốn hiển thị
                                .ToList()
            };

            return View(vm);
        }
        [ChildActionOnly] // Đảm bảo action này chỉ được gọi từ bên trong 1 View
        public ActionResult CategoryMenu()
        {
            // 1. Lấy DANH SÁCH danh mục từ CSDL
            var model = db.DanhMucSanPhams.ToList();

            // 2. Gửi DANH SÁCH này đến PartialView
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


        public ActionResult Shop(string searchString, string sortOrder, string priceRange, string categoryId, int page = 1)
        {
            int pageSize = 9;
            var products = db.SanPhams.AsQueryable();

            // --- 1. LẤY DANH SÁCH DANH MỤC ---
            var categories = db.DanhMucSanPhams.ToList();

            // --- 2. LOGIC LỌC DANH MỤC (SỬA LẠI) ---
            // 👇 Sửa lỗi CS0019 và CS1503: Kiểm tra chuỗi thay vì Nullable Int
            if (!string.IsNullOrEmpty(categoryId))
            {
                // Vì categoryId là string nên so sánh trực tiếp được với MaDM (string)
                products = products.Where(p => p.MaDM == categoryId);
            }

            // --- 3. CÁC LOGIC CŨ (Search, Price, Sort) ---
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
                    case "200-300": products = products.Where(p => p.GiaBanLe >= 200 && p.GiaBanLe < 300); break;
                    case "300-400": products = products.Where(p => p.GiaBanLe >= 300 && p.GiaBanLe < 400); break;
                }
            }

            switch (sortOrder)
            {
                case "price_asc": products = products.OrderBy(p => p.GiaBanLe); break;
                case "price_desc": products = products.OrderByDescending(p => p.GiaBanLe); break;
                default: products = products.OrderByDescending(p => p.MaSP); break;
            }

            // --- 4. PHÂN TRANG ---
            int totalProducts = products.Count();
            int totalPages = (int)Math.Ceiling((double)totalProducts / pageSize);
            var displayProducts = products.Skip((page - 1) * pageSize).Take(pageSize).ToList();

            // --- 5. GÁN DATA VÀO MODEL ---
            var model = new ShopViewModel
            {
                SanPhams = displayProducts,
                Categories = categories,

                // 👇 Sửa lỗi CS0029: Bây giờ cả 2 đều là string, gán OK
                CurrentCategoryId = categoryId,

                SearchString = searchString,
                SortOrder = sortOrder,
                PriceRange = priceRange,
                PageNumber = page,
                PageSize = pageSize,
                TotalPages = totalPages
            };

            return View(model);
        }



        public ActionResult ProductDetails(string id)
        {
            // 1. Vẫn tìm sản phẩm và 'Include' biến thể như cũ
            var sanPham = db.SanPhams
                            .Include(p => p.BienTheSanPhams)
                            .FirstOrDefault(p => p.MaSP == id);

            if (sanPham == null)
            {
                return HttpNotFound();
            }

            // 2. Lọc ra các list size/màu duy nhất (như đã làm)
            var uniqueSizes = sanPham.BienTheSanPhams
                                .Where(b => !string.IsNullOrEmpty(b.MaSize))
                                .Select(b => b.MaSize)
                                .Distinct()
                                .ToList();

            var uniqueColors = sanPham.BienTheSanPhams
                                .Where(b => !string.IsNullOrEmpty(b.MaMau))
                                .Select(b => b.MaMau)
                                .Distinct()
                                .ToList();

            // 3. ĐÂY LÀ BƯỚC QUAN TRỌNG NHẤT
            // Tạo một đối tượng ProductDetailViewModel mới
            var viewModel = new ProductDetailsViewModel
            {
                SanPhamChinh = sanPham,         // Gán sản phẩm vào
                CacSizeDuyNhat = uniqueSizes,   // Gán list size vào
                CacMauDuyNhat = uniqueColors,    // Gán list màu vào
                SanPhamNgauNhiens = db.SanPhams
                                .OrderBy(r => Guid.NewGuid()) // sắp xếp ngẫu nhiên
                                .Take(8) // số lượng sản phẩm muốn hiển thị
                                .ToList()
            };

            // 4. Trả về 'viewModel' (thay vì 'sanPham')
            return View(viewModel);
        }


        public ActionResult Cart()
        {
            GioHang gioHang = null;

            // A. ĐÃ ĐĂNG NHẬP -> Lấy từ Database
            if (Session["MaKH"] != null)
            {
                int maKH = (int)Session["MaKH"];

                // Lấy giỏ hàng và NẠP LUÔN thông tin sản phẩm (Include) để hiển thị tên/ảnh
                gioHang = db.GioHangs
                            .Include("ChiTietGioHangs.SanPham") // Nạp bảng SanPham qua khóa ngoại
                            .FirstOrDefault(g => g.MaKH == maKH);
            }
            // B. CHƯA ĐĂNG NHẬP -> Lấy từ Session
            else
            {
                gioHang = Session["Cart"] as GioHang;
            }

            // Nếu chưa có giỏ, tạo mới để View không bị lỗi Null
            if (gioHang == null)
            {
                gioHang = new GioHang();
                gioHang.ChiTietGioHangs = new List<ChiTietGioHang>();
            }

            return View(gioHang);
        }

        [HttpPost]
        public JsonResult AddToCart(string productId, int quantity = 1, string selectedSize = null, string selectedColor = null)
        {
            try
            {
                if (string.IsNullOrEmpty(productId))
                    return Json(new { success = false, message = "Sản phẩm không hợp lệ." });

                if (quantity < 1) quantity = 1;

                // Lấy sản phẩm chính
                var sanPham = db.SanPhams.FirstOrDefault(p => p.MaSP == productId);
                if (sanPham == null)
                    return Json(new { success = false, message = "Sản phẩm không tồn tại." });

                // Tìm biến thể để lấy giá đúng (nếu có)
                var bienThe = db.BienTheSanPhams.FirstOrDefault(b =>
                    b.MaSP == productId &&
                    b.MaSize == selectedSize &&
                    b.MaMau == selectedColor);

                decimal donGia = bienThe?.GiaBan ?? sanPham.GiaBanLe ?? 0;

                // =============================================
                // 1. ĐÃ ĐĂNG NHẬP → Lưu vào DATABASE
                // =============================================
                if (Session["MaKH"] != null)
                {
                    int maKH = (int)Session["MaKH"];

                    var gioHang = db.GioHangs.FirstOrDefault(g => g.MaKH == maKH);
                    if (gioHang == null)
                    {
                        gioHang = new GioHang { MaKH = maKH, NgayTao = DateTime.Now };
                        db.GioHangs.Add(gioHang);
                        db.SaveChanges(); // Cần SaveChanges để có MaGioHang
                    }

                    var chiTiet = db.ChiTietGioHangs.FirstOrDefault(c =>
                        c.MaGioHang == gioHang.MaGioHang &&
                        c.MaSP == productId &&
                        c.MaSize == selectedSize &&
                        c.MaMau == selectedColor);

                    if (chiTiet != null)
                    {
                        chiTiet.SoLuong += quantity;
                    }
                    else
                    {
                        db.ChiTietGioHangs.Add(new ChiTietGioHang
                        {
                            MaGioHang = gioHang.MaGioHang,
                            MaSP = productId,
                            SoLuong = quantity,
                            DonGia = donGia,
                            MaSize = selectedSize,
                            MaMau = selectedColor
                        });
                    }
                    db.SaveChanges();
                }
                // =============================================
                // 2. CHƯA ĐĂNG NHẬP → Lưu vào SESSION
                // =============================================
                else
                {
                    GioHang gioHang = Session["Cart"] as GioHang ?? new GioHang();

                    if (gioHang.ChiTietGioHangs == null)
                        gioHang.ChiTietGioHangs = new List<ChiTietGioHang>();

                    var item = gioHang.ChiTietGioHangs.FirstOrDefault(i =>
                        i.MaSP == productId &&
                        i.MaSize == selectedSize &&
                        i.MaMau == selectedColor);

                    if (item != null)
                    {
                        item.SoLuong += quantity;
                    }
                    else
                    {
                        gioHang.ChiTietGioHangs.Add(new ChiTietGioHang
                        {
                            MaSP = productId,
                            SanPham = sanPham, // Để hiển thị tên + ảnh trong giỏ
                            SoLuong = quantity,
                            DonGia = donGia,
                            MaSize = selectedSize,
                            MaMau = selectedColor
                        });
                    }

                    Session["Cart"] = gioHang;
                }

                // Tính tổng số lượng trong giỏ (realtime)
                int totalItems = GetCartTotalItems();

                return Json(new
                {
                    success = true,
                    message = "Đã thêm vào giỏ hàng!",
                    totalItems = totalItems
                });
            }
            catch (Exception)
            {
                return Json(new { success = false, message = "Đã có lỗi xảy ra. Vui lòng thử lại!" });
            }
        }

        // Helper tính tổng số món trong giỏ (dùng cho cả DB và Session)
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
                return gioHang?.TongSoLuong() ?? 0;
            }
        }



        // ==========================================
        // 3. CẬP NHẬT SỐ LƯỢNG (UPDATE)
        // ==========================================
        [HttpPost]
        public ActionResult UpdateCart(string id, string size, string color, int quantity)
        {
            if (quantity < 1) quantity = 1;

            if (Session["MaKH"] != null)
            {
                int maKH = (int)Session["MaKH"];
                var gioHang = db.GioHangs.FirstOrDefault(g => g.MaKH == maKH);

                if (gioHang != null)
                {
                    var chiTiet = db.ChiTietGioHangs.FirstOrDefault(c =>
                        c.MaGioHang == gioHang.MaGioHang &&
                        c.MaSP == id &&
                        c.MaSize == size &&   // <--- ĐÃ SỬA: MaSize
                        c.MaMau == color);    // <--- ĐÃ SỬA: MaMau

                    if (chiTiet != null)
                    {
                        chiTiet.SoLuong = quantity;
                        db.SaveChanges();
                    }
                }
            }
            else
            {
                var gioHang = Session["Cart"] as GioHang;
                if (gioHang != null)
                {
                    // Lưu ý: Nếu dùng Session, thuộc tính của object trong list cũng phải khớp
                    var item = gioHang.ChiTietGioHangs.FirstOrDefault(i => i.MaSP == id && i.MaSize == size && i.MaMau == color);
                    if (item != null) item.SoLuong = quantity;
                }
            }
            return RedirectToAction("Cart");
        }

        // ==========================================
        // 4. XÓA SẢN PHẨM (REMOVE)
        // ==========================================
        public ActionResult RemoveFromCart(string id, string size, string color)
        {
            if (Session["MaKH"] != null)
            {
                int maKH = (int)Session["MaKH"];
                var gioHang = db.GioHangs.FirstOrDefault(g => g.MaKH == maKH);

                if (gioHang != null)
                {
                    var chiTiet = db.ChiTietGioHangs.FirstOrDefault(c =>
                        c.MaGioHang == gioHang.MaGioHang &&
                        c.MaSP == id &&
                        c.MaSize == size &&   // <--- ĐÃ SỬA
                        c.MaMau == color);    // <--- ĐÃ SỬA

                    if (chiTiet != null)
                    {
                        db.ChiTietGioHangs.Remove(chiTiet);
                        db.SaveChanges();
                    }
                }
            }
            else
            {
                var gioHang = Session["Cart"] as GioHang;
                if (gioHang != null)
                {
                    gioHang.RemoveItem(id, size, color);
                }
            }
            return RedirectToAction("Cart");
        }

        // ==========================================
        // 5. ICON SỐ LƯỢNG TRÊN HEADER
        // ==========================================
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
                    // Tính tổng SQL
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
                    // Gọi hàm TongSoLuong trong Partial Class
                    total = gioHang.TongSoLuong();
                }
            }

            ViewBag.Quantity = total;
            return PartialView("_CartSummary");
        }

        // File: HomeController.cs

        // Hàm này chịu trách nhiệm: Chỉ xóa những món đã mua
        // Helper: Chỉ xóa những sản phẩm đã có trong Đơn Hàng ra khỏi Giỏ Hàng
        private void XoaSanPhamDaMuaKhoiGio(int maDH)
        {
            // 1. Lấy danh sách sản phẩm đã mua trong đơn hàng
            var chiTietDonHang = db.ChiTietDonHangs.Where(d => d.MaDH == maDH).ToList();
            if (!chiTietDonHang.Any()) return;

            // 2. Xử lý xóa cho Thành viên (DB)
            if (Session["MaKH"] != null)
            {
                int maKH = (int)Session["MaKH"];
                var gioHang = db.GioHangs.FirstOrDefault(g => g.MaKH == maKH);
                if (gioHang != null)
                {
                    foreach (var itemMua in chiTietDonHang)
                    {
                        var itemGio = db.ChiTietGioHangs.FirstOrDefault(c =>
                            c.MaGioHang == gioHang.MaGioHang &&
                            c.MaSP == itemMua.MaSP &&
                            c.MaSize == itemMua.Size &&
                            c.MaMau == itemMua.Mau);

                        if (itemGio != null) db.ChiTietGioHangs.Remove(itemGio);
                    }
                    db.SaveChanges();
                }
            }
            // 3. Xử lý xóa cho Khách vãng lai (Session)
            else
            {
                var gioHang = Session["Cart"] as GioHang;
                if (gioHang != null)
                {
                    foreach (var itemMua in chiTietDonHang)
                    {
                        // Gọi hàm RemoveItem trong Partial Class GioHang
                        gioHang.RemoveItem(itemMua.MaSP, itemMua.Size, itemMua.Mau);
                    }
                    Session["Cart"] = gioHang; // Cập nhật lại Session
                }
            }
        }

        [HttpGet]
        public ActionResult Checkout(string selectedIds)
        {
            // 1. Khởi tạo
            GioHang gioHang = null;
            // Lưu ý: MaKH trong Session thường là int, nhưng nếu bạn lưu MaKH là string thì sửa thành string
            int? maKH = Session["MaKH"] as int?;

            // 2. Lấy dữ liệu giỏ hàng
            if (maKH.HasValue)
            {
                gioHang = db.GioHangs
                            .Include("ChiTietGioHangs.SanPham")
                            .FirstOrDefault(g => g.MaKH == maKH.Value);
            }
            else
            {
                gioHang = Session["Cart"] as GioHang;
            }

            if (gioHang == null || gioHang.ChiTietGioHangs == null || !gioHang.ChiTietGioHangs.Any())
            {
                TempData["ErrorMessage"] = "Giỏ hàng trống.";
                return RedirectToAction("Shop");
            }

            // ==========================================
            // B. LỌC SẢN PHẨM (PHIÊN BẢN ALL STRING)
            // ==========================================
            List<ChiTietGioHang> danhSachThanhToan = new List<ChiTietGioHang>();

            if (!string.IsNullOrEmpty(selectedIds))
            {
                var listIds = selectedIds.Split(',').ToList();

                // Bước 1: Tách chuỗi nhưng KHÔNG ép kiểu số
                var listKeys = listIds.Select(s => {
                    var parts = s.Split('_');
                    return new
                    {
                        // Lấy trực tiếp chuỗi (String)
                        MaSP = parts[0],

                        // Nếu chuỗi rỗng (không có size/màu) thì trả về null để so sánh, ngược lại lấy giá trị
                        MaSize = string.IsNullOrEmpty(parts[1]) ? null : parts[1],
                        MaMau = string.IsNullOrEmpty(parts[2]) ? null : parts[2]
                    };
                }).ToList();

                // Bước 2: So sánh String == String
                danhSachThanhToan = gioHang.ChiTietGioHangs
                                           .Where(item => listKeys.Any(k =>
                                                k.MaSP == item.MaSP &&      // String vs String
                                                k.MaSize == item.MaSize &&  // String vs String
                                                k.MaMau == item.MaMau))     // String vs String
                                           .ToList();
            }
            else
            {
                TempData["ErrorMessage"] = "Vui lòng chọn sản phẩm.";
                return RedirectToAction("Cart");
            }

            // C. Tính tổng tiền
            var donHang = new DonHang
            {
                NgayDat = DateTime.Now,
                TongTien = danhSachThanhToan.Sum(x => (x.DonGia ?? 0) * (x.SoLuong ?? 0))
            };

            // Autofill thông tin khách hàng
            if (maKH.HasValue)
            {
                var khach = db.KhachHangs.Find(maKH.Value);
                if (khach != null)
                {
                    donHang.MaKH = maKH.Value;
                    donHang.TenNguoiNhan = khach.TenKH;
                    donHang.SDTNguoiNhan = khach.SoDT;
                    // donHang.DiaChiNhan = khach.DiaChi; 
                }
            }

            // D. Truyền dữ liệu sang View
            ViewBag.ListThanhToan = danhSachThanhToan;
            ViewBag.SelectedIds = selectedIds;

            return View(donHang);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Checkout(DonHang model, string paymentMethod, string selectedIds)
        {
            // 1. Lấy giỏ hàng hiện tại (DB hoặc Session)
            GioHang gioHang = null;
            if (Session["MaKH"] != null)
            {
                int maKH = (int)Session["MaKH"];
                gioHang = db.GioHangs.Include("ChiTietGioHangs.SanPham").FirstOrDefault(g => g.MaKH == maKH);
            }
            else
            {
                gioHang = Session["Cart"] as GioHang;
            }

            // 2. Lọc sản phẩm theo selectedIds (Logic Partial Checkout - ĐÃ SỬA)
            List<ChiTietGioHang> itemsToBuy = new List<ChiTietGioHang>();

            if (gioHang != null && gioHang.ChiTietGioHangs != null)
            {
                if (!string.IsNullOrEmpty(selectedIds))
                {
                    // === [BẮT ĐẦU SỬA] ===
                    // Tách chuỗi selectedIds dạng: "SP01_L_Xanh,SP02_M_Do"
                    var arrRaw = selectedIds.Split(',');

                    // Tạo danh sách Key để so sánh (Toàn bộ là String)
                    var listKeys = arrRaw.Select(s => {
                        var parts = s.Split('_');
                        return new
                        {
                            MaSP = parts[0],
                            // Xử lý null nếu chuỗi rỗng
                            MaSize = string.IsNullOrEmpty(parts[1]) ? null : parts[1],
                            MaMau = string.IsNullOrEmpty(parts[2]) ? null : parts[2]
                        };
                    }).ToList();

                    // Lọc sản phẩm khớp cả 3 điều kiện: MaSP && Size && Màu
                    itemsToBuy = gioHang.ChiTietGioHangs
                                        .Where(item => listKeys.Any(k =>
                                            k.MaSP == item.MaSP &&
                                            k.MaSize == item.MaSize &&
                                            k.MaMau == item.MaMau))
                                        .ToList();
                    // === [KẾT THÚC SỬA] ===
                }
                else
                {
                    // Fallback: Nếu không có ID nào, báo lỗi hoặc lấy hết (ở đây tôi chặn lại)
                    itemsToBuy = new List<ChiTietGioHang>();
                }
            }

            if (itemsToBuy.Count == 0)
            {
                TempData["ErrorMessage"] = "Vui lòng chọn sản phẩm để thanh toán.";
                return RedirectToAction("Cart");
            }

            // 3. Thiết lập thông tin đơn hàng
            model.NgayDat = DateTime.Now;
            // Tính tổng tiền CHỈ DỰA TRÊN CÁC MÓN ĐƯỢC CHỌN
            model.TongTien = itemsToBuy.Sum(x => (x.DonGia ?? 0) * (x.SoLuong ?? 0));

            if (Session["MaKH"] != null) model.MaKH = (int)Session["MaKH"];

            // Set trạng thái
            if (paymentMethod == "VNPay") model.TrangThai = 1; // Chờ thanh toán
            else model.TrangThai = 2; // COD - Mới đặt

            // 4. Lưu Header Đơn hàng
            db.DonHangs.Add(model);
            db.SaveChanges(); // Có MaDH

            // 5. Lưu Chi tiết Đơn hàng (Chỉ lưu itemsToBuy)
            foreach (var item in itemsToBuy)
            {
                var chiTiet = new ChiTietDonHang
                {
                    MaDH = model.MaDH,
                    MaSP = item.MaSP,
                    TenSP = item.SanPham?.TenSanPham ?? "Unknown",
                    AnhSP = item.SanPham?.AnhSP,
                    // Lưu đúng Size/Màu của item đó
                    Size = item.MaSize,
                    Mau = item.MaMau,
                    SoLuong = item.SoLuong,
                    DonGia = item.DonGia
                };
                db.ChiTietDonHangs.Add(chiTiet);
            }
            db.SaveChanges();

            // 6. Xử lý Thanh toán & Xóa giỏ hàng thông minh
            if (paymentMethod == "VNPay")
            {
                // VNPay: Chưa xóa vội, đợi Callback thành công mới xóa
                return Redirect(CreateVnpayUrl(model));
            }
            else
            {
                // COD: Mua xong -> Gọi hàm xóa các món đã mua
                XoaSanPhamDaMuaKhoiGio(model.MaDH);

                return RedirectToAction("OrderSuccess");
            }
        }

        // =======================================================
        // 2. HÀM TẠO URL VNPAY (HELPER) - ĐÃ FIX CALLBACK URL
        // =======================================================
        private string CreateVnpayUrl(DonHang order)
        {
            // Credentials của bạn
            string vnp_TmnCode = "QE91CB08";
            string vnp_HashSecret = "R08TH1P0J1N4T0L63X0Q3KDTFWBYS8YT";
            string vnp_Url = "https://sandbox.vnpayment.vn/paymentv2/vpcpay.html";

            // FIX QUAN TRỌNG: Sửa lại để trỏ đúng đến Action PaymentCallback trong controller hiện tại
            string vnp_Returnurl = Url.Action("PaymentCallback", "Home", null, Request.Url.Scheme);

            // Khởi tạo thư viện
            VnPayLibrary vnpay = new VnPayLibrary();

            // Amount: Phải là LONG và nhân 100 (đổi sang cent)
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

        // ==========================================
        // 9. XỬ LÝ KẾT QUẢ TRẢ VỀ TỪ VNPAY
        // ==========================================
        public ActionResult PaymentCallback()
        {
            string vnp_HashSecret = "R08TH1P0J1N4T0L63X0Q3KDTFWBYS8YT";
            var vnpayData = Request.QueryString;
            VnPayLibrary vnpay = new VnPayLibrary();

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
                            order.TrangThai = 1; // Đã thanh toán
                            db.SaveChanges();

                            // 👇 LOGIC MỚI: Chỉ xóa những món trong đơn hàng này khỏi giỏ
                            XoaSanPhamDaMuaKhoiGio(maDH);

                            TempData["SuccessMessage"] = "Thanh toán thành công!";
                            return RedirectToAction("OrderSuccess");
                        }
                        else if (vnp_ResponseCode == "24") // HỦY
                        {
                            order.TrangThai = 0;
                            db.SaveChanges();
                            TempData["PaymentError"] = "Bạn đã hủy giao dịch.";
                            return RedirectToAction("PaymentFail");
                        }
                        else // LỖI KHÁC
                        {
                            order.TrangThai = 0;
                            db.SaveChanges();
                            TempData["PaymentError"] = "Lỗi VNPay: " + vnp_ResponseCode;
                            return RedirectToAction("PaymentFail");
                        }
                    }
                }
                TempData["PaymentError"] = "Không tìm thấy đơn hàng.";
                return RedirectToAction("PaymentFail");
            }
            else
            {
                TempData["PaymentError"] = "Sai chữ ký bảo mật.";
                return RedirectToAction("PaymentFail");
            }
        
        }
        // ==========================================
        // 8. TRANG THÔNG BÁO THÀNH CÔNG
        // ==========================================
        public ActionResult OrderSuccess()
        {
            return View();
        }
        public ActionResult PaymentFail()
        {
            // Lấy thông báo lỗi từ TempData (do PaymentCallback gửi đến)
            ViewBag.ErrorMessage = TempData["PaymentError"] ?? "Đã xảy ra lỗi không xác định trong quá trình thanh toán.";

            // Nếu có thể, bạn có thể truyền thêm MaDH vào ViewBag để khách hàng thử lại
            // ViewBag.LastMaDH = TempData["LastMaDH"]; 

            return View();
        }
    }
}