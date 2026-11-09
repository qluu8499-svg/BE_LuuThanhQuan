using BE4_LUUTHANHQUAN.Models;
using PagedList.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Permissions;
using System.Web;

namespace BE4_LUUTHANHQUAN.Models.ViewModel
{
    public class ProductDetailsVM
    {
        public Product product { get; set; }
        public int quantity { get; set; } = 1;
        public decimal estimatedValue { get; set; }

        public int PageNumber { get; set; }
        public int PageSize { get; set; } = 3;

        // Đảm bảo tên thuộc tính ở đây phải khớp 100% với trong Partial View
        public List<Product> RelatedProducts { get; set; } = new List<Product>();

        // Nếu file _PVTopProduct dùng TopProducts thì khai báo dạng PagedList như sau:
        public PagedList.IPagedList<Product> TopProducts { get; set; }
    }
}