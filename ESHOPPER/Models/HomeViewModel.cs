using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ESHOPPER.Models
{
    public class HomeViewModel
    {
        public List<Intro> introes { get; set; }
        public List<DanhMucSanPham> DanhMucSanPhams { get; set; }
        public List<SanPham> SanPhams { get; set; }
        public List<SanPham> SanPhamNgauNhiens { get; set; }
        public List<NhaCungCap> nhaCungCaps { get; set; }
        public List<BienTheSanPham> bienTheSanPhams{ get; set; }
    }
}