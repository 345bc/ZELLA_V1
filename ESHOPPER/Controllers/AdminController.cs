using ESHOPPER.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using EntityState = System.Data.Entity.EntityState;

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

        

    }
}