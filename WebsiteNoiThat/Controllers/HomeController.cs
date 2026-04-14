using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using WebsiteNoiThat.Common;
using WebsiteNoiThat.Models;

namespace WebsiteNoiThat.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            return View();
        }
        [ChildActionOnly]
        public PartialViewResult HeaderCart()
        {
            var cart = Session[Commoncontent.CartSession];
            var list = new List<CartItem>();
            if (cart != null)
            {
                list = (List<CartItem>)cart;
            }

            return PartialView(list);
        }

        [HttpPost]
        public JsonResult Chat(ChatRequest req)
        {
            var msg = (req.Message ?? "").ToLower().Trim();
            string reply = "";

            // ===== GREETING =====
            if (msg.Contains("hi") || msg.Contains("hello") || msg.Contains("xin chào"))
            {
                reply = "👋 Xin chào! Mình là trợ lý nội thất 🤖\n" +
                        "Bạn đang tìm gì? (bàn, ghế, sofa, phòng khách...)";
            }

            // ===== BÀN =====
            else if (msg.Contains("bàn"))
            {
                reply = "🪑 Bên mình có nhiều loại bàn:\n" +
                        "- Bàn ăn: ~5tr - 15tr\n" +
                        "- Bàn làm việc: ~2tr - 8tr\n" +
                        "🔥 Đang có mẫu giảm 20% còn ~4.480.000đ\n" +
                        "👉 Xem: /Product/ShowProduct/";
            }

            // ===== GHẾ =====
            else if (msg.Contains("ghế"))
            {
                reply = "🪑 Ghế bên mình gồm:\n" +
                        "- Ghế ăn\n- Ghế sofa\n- Ghế làm việc\n" +
                        "💰 Giá từ 500k - 5tr\n" +
                        "Bạn muốn loại nào?";
            }

            // ===== SOFA =====
            else if (msg.Contains("sofa"))
            {
                reply = "🛋️ Sofa bên mình có:\n" +
                        "- Sofa da cao cấp\n- Sofa vải\n" +
                        "💰 Giá từ 7tr - 30tr\n" +
                        "👉 Gợi ý phòng khách rất đẹp 😄";
            }

            // ===== GIÁ =====
            else if (msg.Contains("giá"))
            {
                reply = "💰 Giá sản phẩm:\n" +
                        "- Bàn: 2tr - 15tr\n" +
                        "- Ghế: 500k - 5tr\n" +
                        "- Sofa: 7tr - 30tr\n" +
                        "👉 Bạn muốn tìm theo ngân sách bao nhiêu?";
            }

            // ===== KHUYẾN MÃI =====
            else if (msg.Contains("khuyến mãi") || msg.Contains("sale") || msg.Contains("giảm"))
            {
                reply = "🔥 Hiện đang giảm giá 20% nhiều sản phẩm!\n" +
                        "👉 Bạn vào mục HOT NHẤT hoặc KHUYẾN MÃI nhé!";
            }

            // ===== PHÒNG KHÁCH =====
            else if (msg.Contains("phòng khách"))
            {
                reply = "🛋️ Nội thất phòng khách gồm:\n" +
                        "- Sofa\n- Bàn trà\n- Kệ TV\n" +
                        "👉 Bạn muốn mình gợi ý combo luôn không?";
            }

            // ===== PHÒNG NGỦ =====
            else if (msg.Contains("phòng ngủ"))
            {
                reply = "🛏️ Phòng ngủ gồm:\n" +
                        "- Giường\n- Tủ\n- Nệm\n" +
                        "💰 Combo từ ~10tr 😄";
            }

            // ===== DANH MỤC =====
            else if (msg.Contains("danh mục") || msg.Contains("category"))
            {
                reply = "📂 Danh mục bên mình:\n" +
                        "- Phòng khách\n- Phòng ngủ\n- Phòng ăn\n" +
                        "- Bàn\n- Ghế\n- Kệ\n- Tủ";
            }

            // ===== LIÊN HỆ =====
            else if (msg.Contains("liên hệ"))
            {
                reply = "📞 Liên hệ:\n" +
                        "- Hotline: 0123 456 789\n" +
                        "- Email: support@noithat.com";
            }

            // ===== NAVIGATION =====
            else if (msg.Contains("xem") || msg.Contains("sản phẩm"))
            {
                reply = "👉 Bạn có thể xem tất cả sản phẩm tại đây:\n/Product/ShowProduct/";
            }

            // ===== PRICE RANGE (fake smart) =====
            else if (msg.Contains("5") && msg.Contains("triệu"))
            {
                reply = "💡 Tầm 5 triệu bạn có thể mua:\n" +
                        "- Bàn ăn gỗ (~5.600.000đ)\n" +
                        "🔥 Đang giảm còn ~4.480.000đ\n" +
                        "👉 /Product/ShowProduct/";
            }

            // ===== FALLBACK =====
            else
            {
                reply = GetRandomReply();
            }

            return Json(new { reply });
        }

        private string GetRandomReply()
        {
            var list = new List<string>
            {
                "🤖 Mình chưa hiểu rõ lắm 😅 Bạn nói rõ hơn được không?",
                "Bạn đang tìm nội thất gì? (bàn, ghế, sofa...)",
                "Mình có thể gợi ý theo giá hoặc danh mục nhé!",
                "Bạn muốn xem sản phẩm HOT hay đang giảm giá?",
                "Bạn cần tư vấn phòng nào? (phòng khách, phòng ngủ...)"
            };

            var rnd = new Random();
            return list[rnd.Next(list.Count)];
        }
    }
}