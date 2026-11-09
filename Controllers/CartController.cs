using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using BE4_LUUTHANHQUAN.Models;
using BE4_LUUTHANHQUAN.Models.ViewModel;

namespace BE4_LUUTHANHQUAN.Controllers
{
    public class CartController : Controller
    {
        private readonly MyStoreEntities db = new MyStoreEntities();
        private List<CartItemVM> Cart { get { var cart = Session["ShoppingCart"] as List<CartItemVM>; if (cart == null) { cart = new List<CartItemVM>(); Session["ShoppingCart"] = cart; } return cart; } }

        public ActionResult Index() { return View(Cart); }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AddToCart(int id, int quantity = 1)
        {
            if (quantity < 1) quantity = 1;
            var product = db.Products.Find(id); if (product == null) return HttpNotFound();
            var item = Cart.FirstOrDefault(x => x.ProductID == id);
            if (item == null) Cart.Add(new CartItemVM { ProductID = id, ProductName = product.ProductName, ProductImage = product.ProductImage, UnitPrice = product.ProductPrice, Quantity = quantity });
            else item.Quantity += quantity;
            TempData["Message"] = "Đã thêm sản phẩm vào giỏ hàng.";
            return RedirectToAction("Index");
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Update(int id, int quantity)
        {
            var item = Cart.FirstOrDefault(x => x.ProductID == id);
            if (item != null) { if (quantity <= 0) Cart.Remove(item); else item.Quantity = Math.Min(quantity, 99); }
            return RedirectToAction("Index");
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Remove(int id) { var item = Cart.FirstOrDefault(x => x.ProductID == id); if (item != null) Cart.Remove(item); return RedirectToAction("Index"); }

        [Authorize]
        public ActionResult Checkout()
        {
            if (!Cart.Any()) return RedirectToAction("Index");
            var customer = db.Customers.FirstOrDefault(c => c.Username == User.Identity.Name);
            if (customer == null) return RedirectToAction("Login", "Account");
            return View(new CheckoutVM { CustomerName = customer.CustomerName, CustomerPhone = customer.CustomerPhone, AddressDelivery = customer.CustomerAddress });
        }

        [HttpPost, Authorize, ValidateAntiForgeryToken]
        public ActionResult Checkout(CheckoutVM model)
        {
            if (!Cart.Any()) return RedirectToAction("Index");
            if (!ModelState.IsValid) return View(model);
            var customer = db.Customers.FirstOrDefault(c => c.Username == User.Identity.Name);
            if (customer == null) return RedirectToAction("Login", "Account");
            using (var tx = db.Database.BeginTransaction())
            {
                try
                {
                    var orderProducts = Cart.Select(item => new { Item = item, Product = db.Products.Find(item.ProductID) }).ToList();
                    if (orderProducts.Any(x => x.Product == null)) throw new InvalidOperationException("Một sản phẩm trong giỏ hàng không còn tồn tại.");
                    var order = new Order { CustomerID = customer.CustomerID, OrderDate = DateTime.Now, AddressDelivery = model.AddressDelivery, PaymentStatus = "Chưa thanh toán", TotalAmount = orderProducts.Sum(x => x.Product.ProductPrice * x.Item.Quantity) };
                    db.Orders.Add(order); db.SaveChanges();
                    foreach (var line in orderProducts)
                    {
                        db.OrderDetails.Add(new OrderDetail { OrderID = order.OrderID, ProductID = line.Item.ProductID, Quantity = line.Item.Quantity, UnitPrice = line.Product.ProductPrice });
                    }
                    customer.CustomerName = model.CustomerName; customer.CustomerPhone = model.CustomerPhone; customer.CustomerAddress = model.AddressDelivery;
                    db.SaveChanges(); tx.Commit(); Session.Remove("ShoppingCart");
                    return RedirectToAction("OrderSuccess", new { id = order.OrderID });
                }
                catch { tx.Rollback(); ModelState.AddModelError("", "Đặt hàng chưa thành công. Vui lòng kiểm tra giỏ hàng và thử lại."); return View(model); }
            }
        }

        [Authorize]
        public ActionResult OrderSuccess(int id)
        {
            var customer = db.Customers.FirstOrDefault(c => c.Username == User.Identity.Name);
            if (customer == null) return HttpNotFound();
            var order = db.Orders.FirstOrDefault(o => o.OrderID == id && o.CustomerID == customer.CustomerID);
            if (order == null) return HttpNotFound(); return View(order);
        }
        protected override void Dispose(bool disposing) { if (disposing) db.Dispose(); base.Dispose(disposing); }
    }
}
