using Models.DAO;
using Models.EF;
using PagedList;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Script.Serialization;
using WebsiteNoiThat.Common;
using WebsiteNoiThat.Models;
using System.IO;
using System.Drawing.Imaging;
using QRCoder;

namespace WebsiteNoiThat.Controllers
{
    public class CartController : Controller
    {

        DBNoiThat db = new DBNoiThat();
        private const string CartSession = "CartSession";
        private const string HistorySession = "HistorySession";
        private const string FakeTokenSessionKey = "FakePayTokens";

        public ActionResult Index()
        {
            var session = (UserLogin)Session[WebsiteNoiThat.Common.Commoncontent.user_sesion];
            if (session != null)
            {
                var cart = Session[CartSession];
                var list = new List<CartItem>();
                if (cart != null)
                {
                    ViewBag.Status = "Đang chờ xác nhận";
                    list = (List<CartItem>)cart;
                }
                return View(list);
            }
            else
            {
                return Redirect("/dang-nhap");
            }
        }

        public JsonResult DeleteAll()
        {
            Session[CartSession] = null;
            return Json(new
            {
                status = true
            });
        }

        public JsonResult Delete(long id)
        {
            var sessionCart = (List<CartItem>)Session[CartSession];
            sessionCart.RemoveAll(x => x.Product.ProductId == id);
            Session[CartSession] = sessionCart;
            return Json(new
            {
                status = true
            });
        }
        public ActionResult DeleteItem(long id)
        {
            var model = db.OrderDetails.SingleOrDefault(n => n.OrderDetailId == id);
            var order = db.Orders.SingleOrDefault(o => o.OrderId == model.OrderId);

            if (order.StatusId == 1 || order.StatusId == 2)
            {
                db.OrderDetails.Remove(model);
                db.SaveChanges();
            }
            else
            {
                return Redirect("/loi-huy-hang");
            }

            return RedirectToAction("HistoryCart");
        }
        public JsonResult Update(string cartModel)
        {
            var jsonCart = new JavaScriptSerializer().Deserialize<List<CartItem>>(cartModel);
            var sessionCart = (List<CartItem>)Session[CartSession];
            foreach (var item in sessionCart)
            {
                var jsonItem = jsonCart.SingleOrDefault(x => x.Product.ProductId == item.Product.ProductId);
                if (jsonItem != null)
                {
                    item.Quantity = jsonItem.Quantity;
                }
            }
            Session[CartSession] = sessionCart;
            return Json(new
            {
                status = true
            });
        }
        public ActionResult AddCart(int productId, int quantity)
        {
            var product = new ProductDao().ViewDetail(productId);
            var cart = Session[CartSession];
            if (cart != null)
            {
                var list = (List<CartItem>)cart;
                if (list.Exists(x => x.Product.ProductId == productId))
                {
                    foreach (var item in list)
                    {
                        if (item.Product.ProductId == productId)
                        {
                            item.Quantity += quantity;
                        }
                    }
                }
                else
                {
                    //tạo mới đối tượng cart item
                    var item = new CartItem();
                    item.Product = product;
                    item.Quantity = quantity;
                    list.Add(item);
                }
                //Gán vào session
                Session[CartSession] = list;
                //Session[HistorySession] = list;
            }
            else
            {
                //tạo mới đối tượng cart item
                var item = new CartItem();
                item.Product = product;
                item.Quantity = quantity;
                var list = new List<CartItem>();
                list.Add(item);
                //Gán vào session
                Session[CartSession] = list;
                //Session[HistorySession] = list;
            }
            return RedirectToAction("Index");
        }
        

        [HttpGet]
        public ActionResult PayBy()
        {
            var session = (UserLogin)Session[WebsiteNoiThat.Common.Commoncontent.user_sesion];
            if (session != null)
            {
                var model = db.Users.SingleOrDefault(n => n.UserId == session.UserId);
                var cart = Session[CartSession];
                var list = new List<CartItem>();
                var total = 0;
                if (cart != null)
                {
                    ViewBag.Status = "Đang chờ xác nhận";
                    list = (List<CartItem>)cart;
                    
                    foreach(CartItem item in list)
                    {
                        total = total + Convert.ToInt32(item.Product.Price * item.Quantity - item.Product.Price*item.Product.Discount*0.01 * item.Quantity);
                    }
                }
                ViewBag.ListItem = list;
                ViewBag.total = total;

                return View(model);
            }
            else
            {
                return Redirect("/dang-nhap");
            }
        }

        [HttpPost]
        public ActionResult PayBy(User n, string paymentMethod)
        {
            var session = (UserLogin)Session[WebsiteNoiThat.Common.Commoncontent.user_sesion];
            var model = db.Users.SingleOrDefault(a => a.UserId == session.UserId);

            // update user info
            model.Name = n.Name;
            model.Phone = n.Phone;
            model.Address = n.Address;
            model.Email = n.Email;
            db.Entry(model).State = System.Data.Entity.EntityState.Modified;
            db.SaveChanges();

            var order = new Order
            {
                UpdateDate = DateTime.Now,
                ShipAddress = n.Address,
                ShipPhone = n.Phone,
                ShipName = n.Name,
                ShipEmail = n.Email,
                UserId = session.UserId,
                StatusId = 1 // new order - waiting
            };

            var id = new OrderDao().Insert(order);
            var cart = (List<CartItem>)Session[CartSession];
            var detailDao = new OrderDetailDao();
            double total = 0;
            foreach (var item in cart)
            {
                var orderDetail = new OrderDetail
                {
                    OrderId = id,
                    ProductId = item.Product.ProductId,
                    Price = Convert.ToInt32(item.Product.Price - item.Product.Price * item.Product.Discount * 0.01),
                    Quantity = item.Quantity
                };
                detailDao.Insert(orderDetail);
                total += (double)(orderDetail.Price * orderDetail.Quantity);
                var pro = db.Products.FirstOrDefault(m => m.ProductId == item.Product.ProductId);
                pro.Quantity -= item.Quantity;
                db.SaveChanges();
            }

            // Temporary: skip sending initial order emails
            // new MailHelper().SendMail(n.Email, "Đơn hàng mới...", content);

            // If COD chosen => show invoice immediately and send invoice email
            if (string.Equals(paymentMethod, "COD", StringComparison.OrdinalIgnoreCase))
            {
                var invoice = BuildInvoiceModel(id, "COD");
                // send invoice email (best-effort, swallow exceptions)
                try
                {
                    SendInvoiceEmail(invoice);
                }
                catch
                {
                    // ignore mail errors to avoid blocking UX
                }

                // clear session cart after building invoice
                Session[CartSession] = null;
                return View("InvoiceFake", invoice);
            }

            // Otherwise: Pay now (QR) - existing fake payment flow
            var token = Guid.NewGuid().ToString("N");
            var expiry = DateTime.UtcNow.AddMinutes(30);
            var tokens = Session["FakePayTokens"] as Dictionary<string, Tuple<int, DateTime>>;
            if (tokens == null) tokens = new Dictionary<string, Tuple<int, DateTime>>(StringComparer.OrdinalIgnoreCase);
            tokens[token] = Tuple.Create(id, expiry);
            Session["FakePayTokens"] = tokens;

            string confirmUrl = Url.Action("ConfirmPayment", "Cart", new { token = token }, Request.Url?.Scheme ?? "http");

            string qrDataUrl;
            using (var qrGenerator = new QRCodeGenerator())
            {
                var qrData = qrGenerator.CreateQrCode(confirmUrl, QRCodeGenerator.ECCLevel.Q);
                using (var qrCode = new QRCode(qrData))
                using (var bitmap = qrCode.GetGraphic(20))
                using (var ms = new MemoryStream())
                {
                    bitmap.Save(ms, ImageFormat.Png);
                    qrDataUrl = "data:image/png;base64," + Convert.ToBase64String(ms.ToArray());
                }
            }

            ViewBag.Token = token;
            ViewBag.ConfirmUrl = confirmUrl;
            ViewBag.QRDataUrl = qrDataUrl;
            ViewBag.Total = total;
            ViewBag.OrderId = id;
            ViewBag.ExpiryUtc = expiry.ToString("o");

            return View("PaymentFake");
        }

        // Endpoint user clicks (from QR or link). Validates token (from session), marks order as paid.
        [HttpGet]
        public ActionResult ConfirmPayment(string token)
        {
            if (string.IsNullOrEmpty(token)) return View("PaymentInvalid");

            var tokens = Session["FakePayTokens"] as Dictionary<string, Tuple<int, DateTime>>;
            if (tokens == null || !tokens.ContainsKey(token)) return View("PaymentInvalid");

            var pair = tokens[token];
            var orderId = pair.Item1;
            var expiry = pair.Item2;
            if (DateTime.UtcNow > expiry) { tokens.Remove(token); Session["FakePayTokens"] = tokens; return View("PaymentInvalid"); }

            var order = db.Orders.SingleOrDefault(o => o.OrderId == orderId);
            if (order == null) { tokens.Remove(token); Session["FakePayTokens"] = tokens; return View("PaymentInvalid"); }

            order.StatusId = 2;
            db.Entry(order).State = System.Data.Entity.EntityState.Modified;
            db.SaveChanges();

            tokens.Remove(token);
            Session["FakePayTokens"] = tokens;
            // do not clear full cart here — invoice will be shown; clear cart because order is placed
            Session[CartSession] = null;

            // Build invoice and show it (payment completed)
            var invoice = BuildInvoiceModel(orderId, "Online (Giả lập)");

            // send invoice email (best-effort)
            try
            {
                SendInvoiceEmail(invoice);
            }
            catch
            {
                // ignore email errors
            }

            return View("InvoiceFake", invoice);
        }

        // helper to build invoice view model from DB
        private InvoiceViewModel BuildInvoiceModel(int orderId, string paymentMethod)
        {
            var order = db.Orders.SingleOrDefault(o => o.OrderId == orderId);
            var vm = new InvoiceViewModel
            {
                OrderId = orderId,
                UpdateDate = order?.UpdateDate ?? DateTime.Now,
                ShipName = order?.ShipName,
                ShipPhone = order?.ShipPhone?.ToString(),
                ShipEmail = order?.ShipEmail,
                ShipAddress = order?.ShipAddress,
                PaymentMethod = paymentMethod,
                StatusId = order?.StatusId ?? 0,
                Items = new List<InvoiceItem>()
            };

            var items = (from d in db.OrderDetails
                         join p in db.Products on d.ProductId equals p.ProductId
                         where d.OrderId == orderId
                         select new InvoiceItem
                         {
                             ProductId = p.ProductId,
                             Name = p.Name,
                             Photo = p.Photo,
                             Price = (double)d.Price,
                             Quantity = (int)d.Quantity,
                             Discount = (int)p.Discount
                         }).ToList();

            vm.Items = items;
            vm.Total = vm.Items.Sum(i => i.Price * i.Quantity);
            return vm;
        }

        // helper to send invoice email using existing template
        private void SendInvoiceEmail(InvoiceViewModel vm)
        {
            if (vm == null) return;
            // build items html
            var htmldata = "<p><b>STT | Tên | Số lượng | Đơn giá | Khuyến mại</b></p>";
            int idx = 1;
            foreach (var it in vm.Items)
            {
                var line = $"{idx}  |  {HttpUtility.HtmlEncode(it.Name)}  |  {it.Quantity}  |  {string.Format("{0:N0}", it.Price)}  | {it.Discount} %";
                htmldata += $"<p>{line}</p>";
                idx++;
            }

            try
            {
                var templatePath = Server.MapPath("~/Common/neworder.html");
                if (!System.IO.File.Exists(templatePath)) return;
                var content = System.IO.File.ReadAllText(templatePath);

                content = content.Replace("{{id}}", vm.OrderId.ToString());
                content = content.Replace("{{CustomerName}}", HttpUtility.HtmlEncode(vm.ShipName ?? ""));
                content = content.Replace("{{Phone}}", HttpUtility.HtmlEncode(vm.ShipPhone ?? ""));
                content = content.Replace("{{Email}}", HttpUtility.HtmlEncode(vm.ShipEmail ?? ""));
                content = content.Replace("{{Address}}", HttpUtility.HtmlEncode(vm.ShipAddress ?? ""));
                content = content.Replace("{{Total}}", vm.Total.ToString("N0"));
                content = content.Replace("{{data}}", htmldata);

                // send email to customer (best-effort)
                if (!string.IsNullOrEmpty(vm.ShipEmail))
                {
                    new MailHelper().SendMail(vm.ShipEmail, $"Hóa đơn đơn hàng #{vm.OrderId} từ NOITHATGO.VN", content);
                }
            }
            catch
            {
                // swallow to avoid interrupting user flow
            }
        }

        public ActionResult HistoryCart(int? page)
        {
            int pagenumber = (page ?? 1);
            int pagesize = 6;
            var session = (UserLogin)Session[WebsiteNoiThat.Common.Commoncontent.user_sesion];

            var model = (from a in db.OrderDetails
                         join b in db.Orders
                         on a.OrderId equals b.OrderId
                         join c in db.Products
                         on a.ProductId equals c.ProductId
                         join d in db.Status on b.StatusId equals d.StatusId
                         where b.UserId == session.UserId

                         select new HistoryCart
                         {
                             OrderDetaiId = a.OrderDetailId,
                             ProductId = a.ProductId,
                             Name = c.Name,
                             Photo = c.Photo,
                             Price = a.Price,
                             Quantity = a.Quantity,
                             Discount = c.Discount,
                             EndDate = c.EndDate,
                             StatusId = b.StatusId,
                             NameStatus = d.Name
                         }).ToList();

            return View(model.ToPagedList(pagenumber, pagesize));

        }

        public ActionResult Success()
        {
            var session = (UserLogin)Session[WebsiteNoiThat.Common.Commoncontent.user_sesion];
            var cart = Session[CartSession];
            var list = new List<CartItem>();
            if (cart != null)
            {
                list = (List<CartItem>)cart;
                ViewBag.Status = "Đã tiếp nhận";
                ViewBag.ListItem = list;
                Session["CartSession"] = null;
            }
            return View(list);
        }

        public ActionResult Error()
        {
            return View();
        }
        public ActionResult DeleteError()
        {
            var session = (UserLogin)Session[WebsiteNoiThat.Common.Commoncontent.user_sesion];

            var model = (from a in db.OrderDetails
                         join b in db.Orders
                         on a.OrderId equals b.OrderId
                         join c in db.Products
                         on a.ProductId equals c.ProductId
                         join d in db.Status on b.StatusId equals d.StatusId
                         where b.UserId == session.UserId

                         select new HistoryCart
                         {
                             OrderDetaiId = a.OrderDetailId,
                             ProductId = a.ProductId,
                             Name = c.Name,
                             Photo = c.Photo,
                             Price = a.Price,
                             Quantity = a.Quantity,
                             Discount = c.Discount,
                             StatusId = b.StatusId,
                             NameStatus = d.Name
                         }).ToList();

            return View(model);
        }
    }
}