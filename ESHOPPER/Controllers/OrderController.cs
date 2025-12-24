using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using ESHOPPER.Models;

namespace ESHOPPER.Controllers
{
    public class OrderController : Controller
    {
        private QlyFashionShopEntities db = new QlyFashionShopEntities();

        // GET: Order
        public ActionResult Index()
        {
            var donHangs = db.DonHangs.Include(d => d.KhachHang).Include(d => d.TTDONHANG);
            ViewBag.trangthai = db.TTDONHANGs;
            return View(donHangs.ToList());
        }

        public ActionResult Details(int id)
        {
            var listChiTiet = db.ChiTietDonHangs
                .Where(x => x.MaDH == id)
                .Include("BienTheSanPham.SanPham")    
                .Include("BienTheSanPham.MauSac")     
                .Include("BienTheSanPham.KichThuoc")  
                .Include("DonHang")                   
                .ToList();                            

            // Kiểm tra nếu không tìm thấy dữ liệu
            if (listChiTiet == null || listChiTiet.Count == 0)
            {
                TempData["Error"] = "Không tìm thấy đơn hàng hoặc đơn hàng rỗng.";
                return RedirectToAction("Index");
            }

            return View(listChiTiet);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
