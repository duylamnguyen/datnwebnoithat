using System;
using System.Collections.Generic;

namespace WebsiteNoiThat.Models
{
    public class InvoiceItem
    {
        public int ProductId { get; set; }
        public string Name { get; set; }
        public string Photo { get; set; }
        public double Price { get; set; }
        public int Quantity { get; set; }
        public int Discount { get; set; }
    }

    public class InvoiceViewModel
    {
        public int OrderId { get; set; }
        public DateTime UpdateDate { get; set; }
        public string ShipName { get; set; }
        public string ShipPhone { get; set; }
        public string ShipEmail { get; set; }
        public string ShipAddress { get; set; }
        public string PaymentMethod { get; set; }
        public int StatusId { get; set; }
        public List<InvoiceItem> Items { get; set; } = new List<InvoiceItem>();
        public double Total { get; set; }
    }
}