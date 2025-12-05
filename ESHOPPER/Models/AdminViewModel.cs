using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ESHOPPER.Models
{
    public class AdminViewModel
    {
        // === 1. THỐNG KÊ (KPI CARDS) ===
        public int ProductCount { get; set; } // Tổng sản phẩm (Hoạt động)
        public int OrderCount { get; set; }       // Tổng đơn hàng
        public int CustomerCount { get; set; }    // Tổng khách hàng
        public decimal TotalRevenue { get; set; } // Tổng doanh thu

        // === 2. BẢNG ĐƠN HÀNG GẦN ĐÂY ===
        // Thay thế DonHang bằng tên Entity/Model cho Đơn hàng của bạn
        //public IEnumerable<DonHang> RecentOrders { get; set; }

        // === 3. THỐNG KÊ NHANH (QUICK STATS) ===
        public int PendingOrders { get; set; }          // Đơn hàng đang xử lý
        public string TopSellingProduct { get; set; }   // Tên sản phẩm bán chạy nhất
        public int NewCustomersThisMonth { get; set; }  // Khách hàng mới trong tháng
    }
}