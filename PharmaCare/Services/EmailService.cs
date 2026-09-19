using System.Net;
using System.Net.Mail;
using System.Net.Mime;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Options;

namespace PharmaCare.Services
{
    /* SMTP-backed email sender with a development fallback. */
    public class EmailService : IEmailService
    {
        private readonly EmailSettings _settings;
        private readonly ILogger<EmailService> _logger;
        private readonly IWebHostEnvironment _environment;

        public EmailService(
            IOptions<EmailSettings> settings,
            ILogger<EmailService> logger,
            IWebHostEnvironment environment)
        {
            _settings = settings.Value;
            _logger = logger;
            _environment = environment;
        }

        private bool IsConfigured =>
            !string.IsNullOrWhiteSpace(_settings.Host) &&
            !string.IsNullOrWhiteSpace(_settings.Username) &&
            !string.IsNullOrWhiteSpace(_settings.Password);

        public async Task<bool> SendEmailAsync(string toAddress, string subject, string htmlBody)
        {
            if (!IsConfigured)
            {
                _logger.LogWarning(
                    "[EMAIL FALLBACK] SMTP not configured. Would send to {To}\nSubject: {Subject}\nBody:\n{Body}",
                    toAddress, subject, htmlBody);
                return true;
            }

            try
            {
                using var message = CreateMessage(toAddress, subject);
                message.Body = htmlBody;
                message.IsBodyHtml = true;

                using var client = CreateClient();
                await client.SendMailAsync(message);
                _logger.LogInformation("Email sent to {To} (subject: {Subject})", toAddress, subject);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email to {To}", toAddress);
                return false;
            }
        }

        public Task SendVerificationCodeAsync(string toAddress, string firstName, string code)
        {
            var body = BuildCodeEmail(
                greeting: $"Hi {WebUtility.HtmlEncode(firstName)},",
                intro: "Thanks for creating a PharmaCare account. Use the code below to verify your email address:",
                code: code,
                note: "This code expires in 15 minutes. If you didn't sign up, you can safely ignore this email.");
            return SendEmailAsync(toAddress, "Verify your PharmaCare email", body);
        }

        public Task SendPasswordResetCodeAsync(string toAddress, string firstName, string code)
        {
            var body = BuildCodeEmail(
                greeting: $"Hi {WebUtility.HtmlEncode(firstName)},",
                intro: "We received a request to reset your PharmaCare password. Use the code below to continue:",
                code: code,
                note: "This code expires in 15 minutes. If you didn't request this, please ignore this email and your password will stay unchanged.");
            return SendEmailAsync(toAddress, "Reset your PharmaCare password", body);
        }

        public async Task SendOrderPlacedNotificationsAsync(Order order, User customer, IEnumerable<User> staffRecipients)
        {
            var products = order.OrderItems ?? new List<OrderItem>();
            var inlineImages = BuildInlineImages(products.Select(x => x.Product));
            var itemRows = BuildOrderItemRows(products, inlineImages);
            var customerName = Html($"{customer.FirstName} {customer.LastName}".Trim());
            var orderNumber = Html(order.OrderNumber);

            var customerBody = BuildShell($@"
                <div style='display:inline-block;background:#e8f7f4;color:#0a8176;border-radius:999px;padding:7px 12px;font-size:12px;font-weight:700;margin-bottom:14px'>ORDER RECEIVED</div>
                <h1 style='margin:0 0 10px;font-size:28px;color:#10252a'>Thanks, {customerName}. We received your order.</h1>
                <p style='margin:0 0 24px;color:#617379;line-height:1.7'>Your order <strong>{orderNumber}</strong> is now in our pharmacy queue. We will keep your order status updated in your PharmaCare account.</p>
                {BuildOrderSummaryCard(order, false)}
                <h2 style='font-size:18px;color:#10252a;margin:28px 0 12px'>Items in your order</h2>
                <table role='presentation' width='100%' cellspacing='0' cellpadding='0' style='border-collapse:collapse'>{itemRows}</table>
                <div style='margin-top:24px;padding:18px;border-radius:14px;background:#f4f9f8;border:1px solid #dce9e7'>
                  <strong style='display:block;color:#123b3b;margin-bottom:6px'>Delivery details</strong>
                  <div style='color:#617379;font-size:14px;line-height:1.7'>{Html(order.ShippingAddress)}, {Html(order.City)}<br>{Html(order.PhoneNumber)}<br>{Html(order.PaymentMethod)}</div>
                </div>
                <p style='margin:24px 0 0;color:#7b8a8f;font-size:13px'>Keep this email for your records. If you need help, contact PharmaCare and include your order number.</p>");

            await SendRichEmailAsync(customer.Email, $"Order received - {order.OrderNumber}", customerBody, inlineImages);

            var staffBody = BuildShell($@"
                <div style='display:inline-block;background:#fff2d9;color:#986300;border-radius:999px;padding:7px 12px;font-size:12px;font-weight:700;margin-bottom:14px'>NEW ORDER</div>
                <h1 style='margin:0 0 10px;font-size:28px;color:#10252a'>New order {orderNumber}</h1>
                <p style='margin:0 0 24px;color:#617379;line-height:1.7'>A customer has placed a new order and it is ready for pharmacy review.</p>
                {BuildCustomerCard(customer, order.ShippingAddress, order.City, order.PhoneNumber)}
                {BuildOrderSummaryCard(order, true)}
                <h2 style='font-size:18px;color:#10252a;margin:28px 0 12px'>Ordered products</h2>
                <table role='presentation' width='100%' cellspacing='0' cellpadding='0' style='border-collapse:collapse'>{itemRows}</table>
                <p style='margin:24px 0 0;color:#7b8a8f;font-size:13px'>Open PharmaCare Order Management to process this order.</p>");

            foreach (var staff in NormalizeStaffRecipients(staffRecipients))
            {
                await SendRichEmailAsync(staff.Email, $"New order - {order.OrderNumber}", staffBody, inlineImages);
            }
        }

        public async Task SendPrescriptionReservationNotificationsAsync(PrescriptionReservation reservation, User customer, IEnumerable<User> staffRecipients)
        {
            var product = reservation.Product;
            var inlineImages = BuildInlineImages(new[] { product });
            var imageMarkup = BuildProductImage(product, inlineImages);
            var customerName = Html($"{customer.FirstName} {customer.LastName}".Trim());
            var reservationNumber = Html(reservation.ReservationNumber);

            var customerBody = BuildShell($@"
                <div style='display:inline-block;background:#e8f7f4;color:#0a8176;border-radius:999px;padding:7px 12px;font-size:12px;font-weight:700;margin-bottom:14px'>RESERVATION CONFIRMED</div>
                <h1 style='margin:0 0 10px;font-size:28px;color:#10252a'>Your medicine is reserved, {customerName}.</h1>
                <p style='margin:0 0 24px;color:#617379;line-height:1.7'>We received reservation <strong>{reservationNumber}</strong>. Please bring a valid prescription when you visit the pharmacy.</p>
                {BuildReservationProductCard(reservation, imageMarkup)}
                <div style='margin-top:20px;padding:18px;border-radius:14px;background:#fff8e8;border:1px solid #f3d99d;color:#765500;line-height:1.7'>
                  <strong>Pickup reminder</strong><br>Your reservation expires on {reservation.ExpiryDate:MMM d, yyyy 'at' h:mm tt}. Payment is completed at the pharmacy after prescription verification.
                </div>
                <p style='margin:24px 0 0;color:#7b8a8f;font-size:13px'>Reservation reference: {reservationNumber}</p>");

            await SendRichEmailAsync(customer.Email, $"Prescription reservation received - {reservation.ReservationNumber}", customerBody, inlineImages);

            var staffBody = BuildShell($@"
                <div style='display:inline-block;background:#fff2d9;color:#986300;border-radius:999px;padding:7px 12px;font-size:12px;font-weight:700;margin-bottom:14px'>NEW PRESCRIPTION PICKUP</div>
                <h1 style='margin:0 0 10px;font-size:28px;color:#10252a'>New reservation {reservationNumber}</h1>
                <p style='margin:0 0 24px;color:#617379;line-height:1.7'>A prescription medicine has been reserved and needs pharmacy verification at pickup.</p>
                {BuildCustomerCard(customer, customer.Address, customer.City, customer.PhoneNumber)}
                {BuildReservationProductCard(reservation, imageMarkup)}
                <div style='margin-top:20px;padding:18px;border-radius:14px;background:#f4f9f8;border:1px solid #dce9e7;color:#43575d;line-height:1.7'>
                  <strong style='color:#123b3b'>Reservation details</strong><br>Reserved: {reservation.ReservationDate:MMM d, yyyy h:mm tt}<br>Expires: {reservation.ExpiryDate:MMM d, yyyy h:mm tt}<br>Status: {Html(reservation.Status)}
                </div>
                <p style='margin:24px 0 0;color:#7b8a8f;font-size:13px'>Open PharmaCare Prescription Pickups to review this reservation.</p>");

            foreach (var staff in NormalizeStaffRecipients(staffRecipients))
            {
                await SendRichEmailAsync(staff.Email, $"New prescription reservation - {reservation.ReservationNumber}", staffBody, inlineImages);
            }
        }

        private async Task<bool> SendRichEmailAsync(string toAddress, string subject, string htmlBody, IReadOnlyDictionary<string, string> inlineImages)
        {
            if (!IsConfigured)
            {
                _logger.LogWarning("[EMAIL FALLBACK] Would send rich email to {To}. Subject: {Subject}", toAddress, subject);
                return true;
            }

            try
            {
                using var message = CreateMessage(toAddress, subject);
                var view = AlternateView.CreateAlternateViewFromString(htmlBody, null, MediaTypeNames.Text.Html);

                foreach (var image in inlineImages)
                {
                    if (!System.IO.File.Exists(image.Value)) continue;
                    var linked = new LinkedResource(image.Value)
                    {
                        ContentId = image.Key,
                        TransferEncoding = System.Net.Mime.TransferEncoding.Base64
                    };
                    linked.ContentType.Name = Path.GetFileName(image.Value);
                    view.LinkedResources.Add(linked);
                }

                message.AlternateViews.Add(view);
                using var client = CreateClient();
                await client.SendMailAsync(message);
                _logger.LogInformation("Rich email sent to {To} (subject: {Subject})", toAddress, subject);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send rich email to {To}", toAddress);
                return false;
            }
        }

        private MailMessage CreateMessage(string toAddress, string subject)
        {
            var fromAddress = string.IsNullOrWhiteSpace(_settings.FromAddress) ? _settings.Username : _settings.FromAddress;
            var message = new MailMessage
            {
                From = new MailAddress(fromAddress, _settings.FromName),
                Subject = subject,
                IsBodyHtml = true
            };
            message.To.Add(toAddress);
            return message;
        }

        private SmtpClient CreateClient() => new(_settings.Host, _settings.Port)
        {
            EnableSsl = _settings.EnableSsl,
            Credentials = new NetworkCredential(_settings.Username, _settings.Password)
        };

        private IReadOnlyDictionary<string, string> BuildInlineImages(IEnumerable<Product?> products)
        {
            var result = new Dictionary<string, string>();
            var index = 0;
            foreach (var product in products.Where(p => p != null))
            {
                var webPath = product!.ImageUrl;
                if (string.IsNullOrWhiteSpace(webPath)) continue;

                var local = ResolveWebRootFile(webPath);
                if (local == null) continue;

                var cid = $"product-{product.ProductId}-{index++}@pharmacare";
                if (!result.Values.Contains(local, StringComparer.OrdinalIgnoreCase))
                    result[cid] = local;
            }
            return result;
        }

        private string? ResolveWebRootFile(string webPath)
        {
            if (string.IsNullOrWhiteSpace(webPath) || webPath.StartsWith("http", StringComparison.OrdinalIgnoreCase)) return null;
            var normalized = webPath.Replace('\\', '/').TrimStart('~').TrimStart('/');
            var fullPath = Path.GetFullPath(Path.Combine(_environment.WebRootPath, normalized.Replace('/', Path.DirectorySeparatorChar)));
            var webRoot = Path.GetFullPath(_environment.WebRootPath) + Path.DirectorySeparatorChar;
            return fullPath.StartsWith(webRoot, StringComparison.OrdinalIgnoreCase) && System.IO.File.Exists(fullPath) ? fullPath : null;
        }

        private static string BuildOrderItemRows(IEnumerable<OrderItem> items, IReadOnlyDictionary<string, string> inlineImages)
        {
            return string.Join("", items.Select(item => $@"
              <tr>
                <td style='padding:14px 0;border-bottom:1px solid #e8eeee;width:68px'>{BuildProductImage(item.Product, inlineImages)}</td>
                <td style='padding:14px 12px;border-bottom:1px solid #e8eeee'>
                  <strong style='display:block;color:#173036'>{Html(item.ProductName)}</strong>
                  <span style='font-size:13px;color:#7a898e'>Qty {item.Quantity} x {item.Price:C}</span>
                </td>
                <td align='right' style='padding:14px 0;border-bottom:1px solid #e8eeee;font-weight:700;color:#173036'>{(item.Price * item.Quantity):C}</td>
              </tr>"));
        }

        private static string BuildProductImage(Product? product, IReadOnlyDictionary<string, string> inlineImages)
        {
            if (product == null) return "<div style='width:58px;height:58px;border-radius:10px;background:#f0f5f4'></div>";
            var cid = inlineImages.Keys.FirstOrDefault(x => x.StartsWith($"product-{product.ProductId}-", StringComparison.Ordinal));
            return cid == null
                ? "<div style='width:58px;height:58px;border-radius:10px;background:#f0f5f4'></div>"
                : $"<img src='cid:{cid}' width='58' height='58' alt='{Html(product.ProductName)}' style='display:block;width:58px;height:58px;object-fit:contain;border-radius:10px;background:#f6f9f9;border:1px solid #e4eceb'>";
        }

        private static string BuildOrderSummaryCard(Order order, bool staffView) => $@"
          <table role='presentation' width='100%' cellspacing='0' cellpadding='0' style='background:#0b4f4b;border-radius:16px;color:white;padding:4px 0'>
            <tr><td style='padding:18px 20px'><span style='color:#a9d7d2;font-size:12px'>ORDER NUMBER</span><br><strong>{Html(order.OrderNumber)}</strong></td><td align='right' style='padding:18px 20px'><span style='color:#a9d7d2;font-size:12px'>TOTAL</span><br><strong style='font-size:20px;color:#58e1d2'>{order.TotalAmount:C}</strong></td></tr>
            <tr><td style='padding:0 20px 18px;color:#d4e7e5;font-size:13px'>Placed {order.OrderDate:MMM d, yyyy h:mm tt}</td><td align='right' style='padding:0 20px 18px;color:#d4e7e5;font-size:13px'>{Html(order.Status)}{(staffView ? $" - {Html(order.PaymentMethod)}" : "")}</td></tr>
          </table>";

        private static string BuildCustomerCard(User customer, string? address, string? city, string? phone) => $@"
          <div style='margin-bottom:20px;padding:18px;border-radius:14px;background:#f4f9f8;border:1px solid #dce9e7'>
            <strong style='display:block;color:#123b3b;margin-bottom:8px'>Customer</strong>
            <div style='color:#53676d;font-size:14px;line-height:1.75'>{Html($"{customer.FirstName} {customer.LastName}".Trim())}<br>{Html(customer.Email)}<br>{Html(phone)}<br>{Html(address)}, {Html(city)}</div>
          </div>";

        private static string BuildReservationProductCard(PrescriptionReservation reservation, string imageMarkup) => $@"
          <table role='presentation' width='100%' cellspacing='0' cellpadding='0' style='border:1px solid #dfe9e7;border-radius:16px;background:#fbfdfd'>
            <tr>
              <td style='padding:18px;width:74px'>{imageMarkup}</td>
              <td style='padding:18px 10px'><span style='font-size:11px;font-weight:700;color:#0b9185'>PRESCRIPTION MEDICINE</span><br><strong style='font-size:17px;color:#173036'>{Html(reservation.Product?.ProductName)}</strong><br><span style='font-size:13px;color:#75868b'>Quantity: {reservation.Quantity}</span></td>
              <td align='right' style='padding:18px;font-weight:800;color:#0b9185'>{((reservation.Product?.Price ?? 0m) * reservation.Quantity):C}</td>
            </tr>
          </table>";

        private static IEnumerable<User> NormalizeStaffRecipients(IEnumerable<User> staffRecipients) =>
            staffRecipients
                .Where(u => u != null && u.IsActive && !string.IsNullOrWhiteSpace(u.Email) && (u.Role == "Admin" || u.Role == "Pharmacist"))
                .GroupBy(u => u.Email.Trim().ToLowerInvariant())
                .Select(g => g.First());

        private static string BuildShell(string content) => $@"
<!doctype html>
<html><body style='margin:0;padding:0;background:#eef5f4'>
<table role='presentation' width='100%' cellspacing='0' cellpadding='0' style='background:#eef5f4;padding:28px 12px'>
<tr><td align='center'>
<table role='presentation' width='100%' cellspacing='0' cellpadding='0' style='max-width:660px;background:#ffffff;border-radius:20px;overflow:hidden;border:1px solid #dce8e6;box-shadow:0 12px 32px rgba(15,70,68,.08)'>
<tr><td style='padding:24px 28px;background:#073b3c;color:#fff'><span style='display:inline-flex;align-items:center;font-size:22px;font-weight:800;letter-spacing:.3px'>PHARMACARE</span><div style='font-size:12px;color:#a9d6d1;margin-top:4px'>Trusted pharmacy care in Amman</div></td></tr>
<tr><td style='padding:30px 28px;font-family:Arial,Helvetica,sans-serif'>{content}</td></tr>
<tr><td style='padding:18px 28px;background:#f6faf9;border-top:1px solid #e2ecea;font-family:Arial,Helvetica,sans-serif;color:#809095;font-size:12px'>PharmaCare - Amman, Jordan &nbsp; | &nbsp; pharmacare@info.com &nbsp; | &nbsp; +962 7 9999 8888</td></tr>
</table>
</td></tr></table>
</body></html>";

        private static string BuildCodeEmail(string greeting, string intro, string code, string note)
        {
            return BuildShell($@"
              <h2 style='color:#123b3b;margin:0 0 14px'>{greeting}</h2>
              <p style='color:#5e7076;line-height:1.7'>{intro}</p>
              <div style='font-size:32px;font-weight:bold;letter-spacing:8px;text-align:center;background:#eaf7f4;color:#087e74;border-radius:12px;padding:16px;margin:20px 0'>{Html(code)}</div>
              <p style='color:#78888d;font-size:13px'>{note}</p>");
        }

        private static string Html(string? value) => WebUtility.HtmlEncode(value ?? string.Empty);
    }
}
