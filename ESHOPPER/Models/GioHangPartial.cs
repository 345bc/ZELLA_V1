using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ESHOPPER.Models
{
    // 👇 QUAN TRỌNG: Phải là 'partial class GioHang'
    // Để nó ghép chung với class GioHang của Database
    public partial class GioHang
    {
        // Bây giờ code này sẽ chạy ngon lành vì GioHang có sẵn ChiTietGioHangs
        public decimal TongTienTamTinh()
        {
            if (this.ChiTietGioHangs == null) return 0;
            return this.ChiTietGioHangs.Sum(x => (x.DonGia ?? 0) * (x.SoLuong ?? 0));
        }

        public int TongSoLuong()
        {
            if (this.ChiTietGioHangs == null) return 0;
            return this.ChiTietGioHangs.Sum(x => x.SoLuong ?? 0);
        }

        public void AddItem(ChiTietGioHang item)
        {
            if (this.ChiTietGioHangs == null)
                this.ChiTietGioHangs = new List<ChiTietGioHang>();

            var existing = this.ChiTietGioHangs.FirstOrDefault(i =>
                i.MaSP == item.MaSP &&
                i.MaSize == item.MaSize &&
                i.MaMau== item.MaMau);

            if (existing != null)
            {
                existing.SoLuong += item.SoLuong;
            }
            else
            {
                this.ChiTietGioHangs.Add(item);
            }
        }

        public void RemoveItem(string id, string size, string color)
        {
            var item = this.ChiTietGioHangs.FirstOrDefault(i =>
                i.MaSP == id &&
                i.MaSize == size &&
                i.MaMau == color);

            if (item != null)
            {
                this.ChiTietGioHangs.Remove(item);
            }
        }

    }
}