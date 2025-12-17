using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ESHOPPER.Models
{
    // 👇 partial class GioHang giúp mở rộng logic cho class được sinh tự động bởi Entity Framework
    public partial class GioHang
    {
        // 1. Tính tổng tiền tạm tính dựa trên danh sách chi tiết
        public decimal TongTienTamTinh()
        {
            if (this.ChiTietGioHangs == null) return 0;
            // Sử dụng các thuộc tính có sẵn trong Model mới
            return this.ChiTietGioHangs.Sum(x => (x.DonGia ?? 0) * (x.SoLuong ?? 0));
        }

        // 2. Tính tổng số lượng sản phẩm có trong giỏ
        public int TongSoLuong()
        {
            if (this.ChiTietGioHangs == null) return 0;
            return this.ChiTietGioHangs.Sum(x => x.SoLuong ?? 0);
        }

        // 3. Thêm sản phẩm vào giỏ (Dựa trên định danh MaBienThe)
        public void AddItem(ChiTietGioHang item)
        {
            if (this.ChiTietGioHangs == null)
                this.ChiTietGioHangs = new List<ChiTietGioHang>();

            // [SỬA]: Chỉ cần so khớp MaBienThe là đủ xác định cặp Sản phẩm-Size-Màu duy nhất
            var existing = this.ChiTietGioHangs.FirstOrDefault(i => i.MaBienThe == item.MaBienThe);

            if (existing != null)
            {
                existing.SoLuong += item.SoLuong;
            }
            else
            {
                this.ChiTietGioHangs.Add(item);
            }
        }

        // 4. Xóa sản phẩm khỏi giỏ (Chỉ cần truyền MaBienThe làm ID)
        public void RemoveItem(int maBienThe)
        {
            if (this.ChiTietGioHangs == null) return;

            // [SỬA]: Tìm trực tiếp theo MaBienThe
            var item = this.ChiTietGioHangs.FirstOrDefault(i => i.MaBienThe == maBienThe);

            if (item != null)
            {
                this.ChiTietGioHangs.Remove(item);
            }
        }
    }
}