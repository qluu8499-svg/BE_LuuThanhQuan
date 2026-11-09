using System;

namespace BE4_LUUTHANHQUAN.Models.ViewModel
{
    [Serializable]
    public class CartItemVM
    {
        public int ProductID { get; set; }
        public string ProductName { get; set; }
        public string ProductImage { get; set; }
        public decimal UnitPrice { get; set; }
        public int Quantity { get; set; }
        public decimal Subtotal { get { return UnitPrice * Quantity; } }
    }
}
