using System.Collections.Generic;

namespace ESHOPPER.Models
{
    public class ShopViewModel
    {
        public List<SanPham> SanPhams { get; set; }

        public string SearchString { get; set; }
        public string SortOrder { get; set; }
        public string PriceRange { get; set; }

        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }

        public bool HasPreviousPage => PageNumber > 1;
        public bool HasNextPage => PageNumber < TotalPages;

        // 👇 KIỂM TRA KỸ: Chỉ được có 1 dòng này thôi
        public int? CurrentCategoryId { get; set; }

        // 👇 KIỂM TRA KỸ: Chỉ được có 1 dòng này thôi
        public List<DanhMucSanPham> Categories { get; set; }
    }
}