using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.Description;
using ESHOPPER.Models;

namespace ESHOPPER.Controllers
{
    public class CategoryAPIController : ApiController
    {
        private QlyFashionShopEntities db = new QlyFashionShopEntities();

        public CategoryAPIController()
        {
            // Tắt Proxy để tránh lỗi vòng lặp (Circular Reference) khi serialize JSON
            db.Configuration.ProxyCreationEnabled = false;
        }

        // GET: api/CategoryAPI
        public IQueryable<DanhMucSanPham> GetDanhMucSanPhams()
        {
            return db.DanhMucSanPhams;
        }

        // GET: api/CategoryAPI/5
        // SỬA: Đổi string id -> int id
        [ResponseType(typeof(DanhMucSanPham))]
        public IHttpActionResult GetDanhMucSanPham(int id)
        {
            DanhMucSanPham danhMucSanPham = db.DanhMucSanPhams.Find(id);
            if (danhMucSanPham == null)
            {
                return NotFound();
            }

            return Ok(danhMucSanPham);
        }

        // PUT: api/CategoryAPI/5
        [ResponseType(typeof(void))]
        public IHttpActionResult PutDanhMucSanPham(int id, DanhMucSanPham danhMucSanPham)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (id != danhMucSanPham.MaDM)
            {
                return BadRequest();
            }

            db.Entry(danhMucSanPham).State = EntityState.Modified;

            try
            {
                db.SaveChanges();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!DanhMucSanPhamExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return StatusCode(HttpStatusCode.NoContent);
        }

        // POST: api/CategoryAPI
        [ResponseType(typeof(DanhMucSanPham))]
        public IHttpActionResult PostDanhMucSanPham(DanhMucSanPham danhMucSanPham)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            db.DanhMucSanPhams.Add(danhMucSanPham);

            try
            {
                db.SaveChanges();
            }
            catch (DbUpdateException)
            {
                // Logic này chỉ chạy nếu bạn TỰ NHẬP ID bằng tay. 
                // Nếu ID là tự tăng (Identity) thì nó sẽ không bao giờ trùng.
                if (DanhMucSanPhamExists(danhMucSanPham.MaDM))
                {
                    return Conflict();
                }
                else
                {
                    throw;
                }
            }

            return CreatedAtRoute("DefaultApi", new { id = danhMucSanPham.MaDM }, danhMucSanPham);
        }

        // DELETE: api/CategoryAPI/5
        // SỬA: Đổi string id -> int id
        [ResponseType(typeof(DanhMucSanPham))]
        public IHttpActionResult DeleteDanhMucSanPham(int id)
        {
            DanhMucSanPham danhMucSanPham = db.DanhMucSanPhams.Find(id);
            if (danhMucSanPham == null)
            {
                return NotFound();
            }

            // Kiểm tra ràng buộc trước khi xóa (Optional)
            // Ví dụ: Nếu danh mục đã có sản phẩm thì không cho xóa
            // if (db.SanPhams.Any(x => x.MaDM == id)) { return BadRequest("Danh mục đã có sản phẩm!"); }

            db.DanhMucSanPhams.Remove(danhMucSanPham);
            db.SaveChanges();

            return Ok(danhMucSanPham);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }

        private bool DanhMucSanPhamExists(int id)
        {
            return db.DanhMucSanPhams.Count(e => e.MaDM == id) > 0;
        }
    }
}