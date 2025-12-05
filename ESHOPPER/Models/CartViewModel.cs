using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ESHOPPER.Models
{
    public class CartViewModel
    {
        // --- CÁC THÔNG TIN NHẬN ĐƯỢC TỪ SESSION & CSDL ---

        // 1. Dữ liệu nhận từ CartItem trong Session (bắt buộc)
        public string MaBienThe { get; set; }
        public int SoLuong { get; set; }

        // 2. Giá đã CHỐT tại thời điểm thêm vào giỏ (nhận từ CartItem)
        public decimal Gia { get; set; }

        // 3. Các thông tin chi tiết tải từ CSDL (nhận từ BienTheSP.SanPham)
        public string TenSP { get; set; }
        public string AnhSP { get; set; }
        public string MaSize { get; set; }
        public string MaMau { get; set; }

        // --- THUỘC TÍNH TÍNH TOÁN ---
        public decimal ThanhTien
        {
            get
            {
                // Sửa logic tính toán để KHÔNG cần BienTheSP nữa
                return SoLuong * Gia;
            }
        }
    }
}