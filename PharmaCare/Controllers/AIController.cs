using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace PharmaCare.Controllers
{
    public class AIController : Controller
    {
        private readonly IAIService _aiService;
        private readonly DataDbContext _db;
        private readonly ILogger<AIController> _logger;
        private readonly AISettings _aiSettings;

        private const int MaxMessageLength = 1200;
        private const int MaxRequestsPerWindow = 20;
        private static readonly TimeSpan RateWindow = TimeSpan.FromMinutes(10);

        public AIController(
            IAIService aiService,
            DataDbContext db,
            ILogger<AIController> logger,
            IOptions<AISettings> aiOptions)
        {
            _aiService = aiService;
            _db = db;
            _logger = logger;
            _aiSettings = aiOptions.Value;
        }

        [HttpGet]
        public IActionResult Status()
        {
            return Json(new
            {
                enabled = _aiService.IsConfigured,
                enabledSetting = _aiSettings.Enabled,
                hasApiKey = !string.IsNullOrWhiteSpace(_aiSettings.ApiKey),
                model = _aiSettings.Model
            });
        }

        [HttpPost]
        public async Task<IActionResult> Chat(
            string message,
            string? pagePath,
            string? pageTitle,
            string? historyJson,
            CancellationToken cancellationToken)
        {
            if (!_aiService.IsConfigured)
            {
                return StatusCode(StatusCodes.Status503ServiceUnavailable, new
                {
                    success = false,
                    message = "PharmaCare AI is not configured yet."
                });
            }

            message = (message ?? string.Empty).Trim();
            if (message.Length == 0)
                return BadRequest(new { success = false, message = "Please enter a question." });

            if (message.Length > MaxMessageLength)
                return BadRequest(new { success = false, message = $"Please keep your message under {MaxMessageLength} characters." });

            if (!AllowRequest())
            {
                return StatusCode(StatusCodes.Status429TooManyRequests, new
                {
                    success = false,
                    message = "You've sent several AI requests in a short time. Please wait a few minutes and try again."
                });
            }

            var history = ParseHistory(historyJson);
            var siteContext = await BuildSiteContextAsync(pagePath, pageTitle, cancellationToken);
            var userContext = await BuildUserContextAsync(cancellationToken);

            var result = await _aiService.AskAsync(new AIRequest
            {
                Message = message,
                SiteContext = siteContext,
                UserContext = userContext,
                History = history
            }, cancellationToken);

            if (!result.Success)
            {
                var statusCode = result.Error == "AI_NOT_CONFIGURED"
                    ? StatusCodes.Status503ServiceUnavailable
                    : StatusCodes.Status502BadGateway;

                return StatusCode(statusCode, new { success = false, message = result.Message });
            }

            return Json(new { success = true, message = result.Message });
        }

        private async Task<string> BuildSiteContextAsync(string? pagePath, string? pageTitle, CancellationToken cancellationToken)
        {
            var builder = new StringBuilder();
            builder.AppendLine("PharmaCare website capabilities:");
            builder.AppendLine("- Customers can browse the store, search/filter products, add non-prescription products to cart, checkout, and track their own orders.");
            builder.AppendLine("- Prescription-required medicines are reserved for in-person pickup and require pharmacy verification; they are not completed as an online medicine sale.");
            builder.AppendLine("- Customers can view their prescription reservations, account profile, order history, and contact/support options.");
            builder.AppendLine("- Admin and pharmacist functions are private and must never be exposed to customers.");

            if (!string.IsNullOrWhiteSpace(pageTitle) || !string.IsNullOrWhiteSpace(pagePath))
            {
                builder.AppendLine();
                builder.AppendLine($"Current page: {Sanitize(pageTitle, 120) ?? "Unknown"} ({Sanitize(pagePath, 220) ?? "unknown path"})");
            }

            var products = await _db.Product
                .AsNoTracking()
                .Include(p => p.Category)
                .Where(p => p.IsActive)
                .OrderBy(p => p.ProductName)
                .Take(80)
                .Select(p => new
                {
                    p.ProductId,
                    p.ProductName,
                    p.Description,
                    p.Price,
                    p.Stock,
                    p.RequiresPrescription,
                    CategoryName = p.Category.CategoryName
                })
                .ToListAsync(cancellationToken);

            builder.AppendLine();
            builder.AppendLine("Current customer-facing catalog (authoritative for price/stock/prescription status):");
            foreach (var product in products)
            {
                var description = Sanitize(product.Description, 220);
                builder.Append("- ")
                    .Append(product.ProductName)
                    .Append(" | Category: ").Append(product.CategoryName)
                    .Append(" | Price: $").Append(product.Price.ToString("0.00"))
                    .Append(" | Stock: ").Append(product.Stock)
                    .Append(" | Prescription required: ").Append(product.RequiresPrescription ? "Yes" : "No")
                    .Append(" | Product page: /FrontEnd/ShopSingle/").Append(product.ProductId);

                if (!string.IsNullOrWhiteSpace(description))
                    builder.Append(" | Description: ").Append(description);

                builder.AppendLine();
            }

            return builder.ToString();
        }

        private async Task<string?> BuildUserContextAsync(CancellationToken cancellationToken)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue) return null;

            var builder = new StringBuilder();
            var displayName = HttpContext.Session.GetString("UserName");
            if (!string.IsNullOrWhiteSpace(displayName))
                builder.AppendLine($"Customer name: {Sanitize(displayName, 100)}");

            var orders = await _db.Orders
                .AsNoTracking()
                .Where(o => o.UserId == userId.Value)
                .OrderByDescending(o => o.OrderDate)
                .Take(5)
                .Select(o => new { o.OrderNumber, o.Status, o.TotalAmount, o.OrderDate, o.IsPaid })
                .ToListAsync(cancellationToken);

            if (orders.Count > 0)
            {
                builder.AppendLine("Recent customer orders:");
                foreach (var order in orders)
                {
                    builder.AppendLine($"- {order.OrderNumber}: {order.Status}, total ${order.TotalAmount:0.00}, placed {order.OrderDate:MMM d, yyyy}, payment {(order.IsPaid ? "paid" : "not paid")}.");
                }
            }

            var reservations = await _db.PrescriptionReservations
                .AsNoTracking()
                .Include(r => r.Product)
                .Where(r => r.UserId == userId.Value)
                .OrderByDescending(r => r.ReservationDate)
                .Take(5)
                .Select(r => new { r.ReservationNumber, r.Status, r.ExpiryDate, ProductName = r.Product.ProductName })
                .ToListAsync(cancellationToken);

            if (reservations.Count > 0)
            {
                builder.AppendLine("Recent prescription reservations:");
                foreach (var reservation in reservations)
                {
                    builder.AppendLine($"- {reservation.ReservationNumber}: {reservation.ProductName}, status {reservation.Status}, reservation expires {reservation.ExpiryDate:MMM d, yyyy}.");
                }
            }

            return builder.Length == 0 ? null : builder.ToString();
        }

        private bool AllowRequest()
        {
            var now = DateTimeOffset.UtcNow;
            var rawStart = HttpContext.Session.GetString("AI_RateWindowStart");
            var count = HttpContext.Session.GetInt32("AI_RateCount") ?? 0;

            if (!DateTimeOffset.TryParse(rawStart, out var windowStart) || now - windowStart >= RateWindow)
            {
                HttpContext.Session.SetString("AI_RateWindowStart", now.ToString("O"));
                HttpContext.Session.SetInt32("AI_RateCount", 1);
                return true;
            }

            if (count >= MaxRequestsPerWindow) return false;

            HttpContext.Session.SetInt32("AI_RateCount", count + 1);
            return true;
        }

        private static IReadOnlyList<AIChatMessage> ParseHistory(string? historyJson)
        {
            if (string.IsNullOrWhiteSpace(historyJson)) return Array.Empty<AIChatMessage>();

            try
            {
                var history = JsonSerializer.Deserialize<List<AIChatMessage>>(historyJson, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                }) ?? new List<AIChatMessage>();

                return history
                    .Where(x => !string.IsNullOrWhiteSpace(x.Content))
                    .TakeLast(8)
                    .Select(x => new AIChatMessage
                    {
                        Role = string.Equals(x.Role, "assistant", StringComparison.OrdinalIgnoreCase) ? "assistant" : "user",
                        Content = x.Content.Trim().Length > 800 ? x.Content.Trim()[..800] : x.Content.Trim()
                    })
                    .ToList();
            }
            catch (JsonException)
            {
                return Array.Empty<AIChatMessage>();
            }
        }

        private static string? Sanitize(string? value, int maxLength)
        {
            if (string.IsNullOrWhiteSpace(value)) return null;
            var cleaned = value.Replace("\r", " ").Replace("\n", " ").Trim();
            return cleaned.Length <= maxLength ? cleaned : cleaned[..maxLength] + "…";
        }
    }
}
