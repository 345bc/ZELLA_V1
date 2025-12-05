using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ESHOPPER.Models
{
    public class CartItem
    {
        // 1. Mã Biến Thể (ID duy nhất của sản phẩm/size/màu)
        public string MaBienThe { get; set; }

        // 2. Số lượng người dùng đã thêm
        public int SoLuong { get; set; }

        // 3. Giá đã chốt tại thời điểm thêm vào giỏ (để trábnh thay đổi giá)
        // Giá này phải là decimal (không-null)
        public decimal Gia { get; set; }
    }
}