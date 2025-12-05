using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ESHOPPER.Models
{
    public class ProductDetailsViewModel
    {
        public SanPham SanPhamChinh { get; set; }
        public List<string> CacSizeDuyNhat { get; set; }
        public List<string> CacMauDuyNhat { get; set; }
        public List<SanPham> SanPhamNgauNhiens { get; set; }

    }
}