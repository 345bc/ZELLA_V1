using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ESHOPPER.Models
{
    public class ProductDetailsViewModel
    {
        public SanPham SanPhamChinh { get; set; }
        public List<KichThuoc> CacSizeDuyNhat { get; set; }
        public List<MauSac> CacMauDuyNhat { get; set; }
        public List<SanPham> SanPhamLienQuans { get; set; }
        public List<BienTheSanPham> DanhSachBienThe { get; set; }

    }
}