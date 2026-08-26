using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PharmaCare.Data;
using PharmaCare.Models;
using PharmaCare.Services;
using System.Net;

namespace PharmaCare.Controllers
{
    public class SupportController : Controller
    {
        private readonly DataDbContext _context;
        private readonly IEmailService _emailService;
        private readonly ILogger<SupportController> _logger;

        public SupportController(
            DataDbContext context,
            IEmailService emailService,
            ILogger<SupportController> logger)
        {
            _context = context;
            _emailService = emailService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> Context()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue)
            {
                return Json(new
                {
                    isLoggedIn = false,
                    firstName = "",
                    lastName = "",
                    email = "",
                    emailVerified = false
                });
            }

            var user = await _context.User
                .AsNoTracking()
                .Where(u => u.UserId == userId.Value)
                .Select(u => new
                {
                    u.FirstName,
                    u.LastName,
                    u.Email,
                    u.IsEmailVerified
                })
                .FirstOrDefaultAsync();

            if (user == null)
            {
                return Json(new
                {
                    isLoggedIn = false,
                    firstName = "",
                    lastName = "",
                    email = "",
                    emailVerified = false
                });
            }

            return Json(new
            {
                isLoggedIn = true,
                firstName = user.FirstName ?? "",
                lastName = user.LastName ?? "",
                email = user.Email ?? "",
                emailVerified = user.IsEmailVerified
            });
        }

        [HttpPost]
        public async Task<IActionResult> Send([FromBody] QuickSupportRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Message))
            {
                return BadRequest(new { success = false, message = "Please enter a message." });
            }

            try
            {
                var userId = HttpContext.Session.GetInt32("UserId");
                User? user = null;

                if (userId.HasValue)
                {
                    user = await _context.User
                        .AsNoTracking()
                        .FirstOrDefaultAsync(u => u.UserId == userId.Value);
                }

                var firstName = !string.IsNullOrWhiteSpace(request.FirstName)
                    ? request.FirstName.Trim()
                    : user?.FirstName ?? "Customer";

                var lastName = !string.IsNullOrWhiteSpace(request.LastName)
                    ? request.LastName.Trim()
                    : user?.LastName ?? "";

                // A signed-in customer always uses the email that was verified on the account.
                // This prevents the support form from being used with a different/unverified address.
                var email = user != null && user.IsEmailVerified && !string.IsNullOrWhiteSpace(user.Email)
                    ? user.Email.Trim()
                    : request.Email?.Trim() ?? "";

                if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
                {
                    return BadRequest(new { success = false, message = "Please enter a valid email address." });
                }

                var supportType = string.IsNullOrWhiteSpace(request.SupportType)
                    ? "General support"
                    : request.SupportType.Trim();

                var pageUrl = string.IsNullOrWhiteSpace(request.PageUrl)
                    ? "Unknown page"
                    : request.PageUrl.Trim();

                var messageText = request.Message.Trim();
                if (messageText.Length > 4000)
                {
                    messageText = messageText[..4000];
                }

                var contactMessage = new ContactMessage
                {
                    FirstName = firstName,
                    LastName = lastName,
                    Email = email,
                    Subject = $"Quick Support - {supportType}",
                    Message = $"{messageText}\n\nPage: {pageUrl}",
                    DateSubmitted = DateTime.UtcNow,
                    UserId = userId,
                    UserType = userId.HasValue ? "User" : "non-user"
                };

                _context.ContactMessages.Add(contactMessage);
                await _context.SaveChangesAsync();

                // Email is intentionally sent after the database save. A temporary SMTP issue
                // must never lose the customer's support request.
                try
                {
                    var encodedName = WebUtility.HtmlEncode($"{firstName} {lastName}".Trim());
                    var encodedEmail = WebUtility.HtmlEncode(email);
                    var encodedType = WebUtility.HtmlEncode(supportType);
                    var encodedMessage = WebUtility.HtmlEncode(messageText).Replace("\n", "<br>");
                    var encodedPage = WebUtility.HtmlEncode(pageUrl);
                    var ticketReference = $"SUP-{contactMessage.DateSubmitted:yyyyMMdd}-{contactMessage.GetHashCode():X4}";

                    var customerHtml = $@"
<!doctype html>
<html><body style='margin:0;background:#f4f8f7;font-family:Arial,sans-serif;color:#173035'>
<table width='100%' cellpadding='0' cellspacing='0' style='padding:28px 12px;background:#f4f8f7'><tr><td align='center'>
<table width='620' cellpadding='0' cellspacing='0' style='max-width:620px;width:100%;background:#ffffff;border-radius:18px;overflow:hidden;border:1px solid #dce9e6'>
<tr><td style='padding:24px 28px;background:#0d766f;color:#fff'><div style='font-size:12px;letter-spacing:1px;font-weight:700'>PHARMACARE SUPPORT</div><h1 style='margin:8px 0 0;font-size:25px'>We received your message.</h1></td></tr>
<tr><td style='padding:28px'>
<p style='margin:0 0 14px;font-size:16px'>Hi {encodedName},</p>
<p style='margin:0 0 20px;line-height:1.7;color:#52656a'>Thank you for contacting PharmaCare. Your request has been received successfully and our team will review it as soon as possible.</p>
<div style='padding:16px 18px;background:#edf8f6;border-radius:12px;border-left:4px solid #0d9488'>
<div style='font-size:12px;color:#6f8085;margin-bottom:5px'>SUPPORT TOPIC</div><strong>{encodedType}</strong>
<div style='font-size:12px;color:#6f8085;margin:14px 0 5px'>YOUR MESSAGE</div><div style='line-height:1.6'>{encodedMessage}</div>
</div>
<p style='margin:20px 0 0;font-size:13px;line-height:1.6;color:#738388'>You do not need to submit the same request again. If we need additional information, the team can follow up using this email address.</p>
</td></tr>
<tr><td style='padding:18px 28px;background:#0a3031;color:#b8d0ce;font-size:12px'>PharmaCare · Amman, Jordan</td></tr>
</table></td></tr></table></body></html>";

                    await _emailService.SendEmailAsync(
                        email,
                        $"We received your PharmaCare support request - {supportType}",
                        customerHtml);

                    var staffRecipients = await _context.User
                        .AsNoTracking()
                        .Where(u => u.IsActive &&
                                    (u.Role == "Admin" || u.Role == "Pharmacist") &&
                                    !string.IsNullOrEmpty(u.Email))
                        .Select(u => u.Email)
                        .Distinct()
                        .ToListAsync();

                    var staffHtml = $@"
<!doctype html>
<html><body style='margin:0;background:#f4f8f7;font-family:Arial,sans-serif;color:#173035'>
<table width='100%' cellpadding='0' cellspacing='0' style='padding:28px 12px;background:#f4f8f7'><tr><td align='center'>
<table width='650' cellpadding='0' cellspacing='0' style='max-width:650px;width:100%;background:#fff;border-radius:18px;overflow:hidden;border:1px solid #dce9e6'>
<tr><td style='padding:24px 28px;background:#083f40;color:#fff'><div style='font-size:12px;letter-spacing:1px;font-weight:700'>NEW SUPPORT REQUEST</div><h1 style='margin:8px 0 0;font-size:24px'>{encodedType}</h1></td></tr>
<tr><td style='padding:26px 28px'>
<table width='100%' cellpadding='0' cellspacing='0' style='font-size:14px'>
<tr><td style='padding:8px 0;color:#75868a;width:130px'>Customer</td><td style='padding:8px 0;font-weight:700'>{encodedName}</td></tr>
<tr><td style='padding:8px 0;color:#75868a'>Email</td><td style='padding:8px 0'>{encodedEmail}</td></tr>
<tr><td style='padding:8px 0;color:#75868a'>Account</td><td style='padding:8px 0'>{(userId.HasValue ? "Signed-in customer" : "Guest")}</td></tr>
<tr><td style='padding:8px 0;color:#75868a'>Page</td><td style='padding:8px 0'>{encodedPage}</td></tr>
<tr><td style='padding:8px 0;color:#75868a'>Submitted</td><td style='padding:8px 0'>{contactMessage.DateSubmitted:MMM dd, yyyy HH:mm} UTC</td></tr>
</table>
<div style='margin-top:18px;padding:17px 18px;background:#f3f7f7;border-radius:12px;border:1px solid #e0e9e7'><div style='font-size:12px;color:#718186;margin-bottom:7px;font-weight:700'>MESSAGE</div><div style='line-height:1.65'>{encodedMessage}</div></div>
<p style='margin:18px 0 0;color:#718186;font-size:13px'>The request is also saved in the PharmaCare Feedback / Contact administration page.</p>
</td></tr>
</table></td></tr></table></body></html>";

                    foreach (var staffEmail in staffRecipients.Where(e => !string.IsNullOrWhiteSpace(e)))
                    {
                        await _emailService.SendEmailAsync(
                            staffEmail!,
                            $"New PharmaCare support request: {supportType} - {firstName} {lastName}".Trim(),
                            staffHtml);
                    }
                }
                catch (Exception emailException)
                {
                    _logger.LogError(emailException, "Support request #{MessageId} saved but email notification failed", contactMessage.GetHashCode());
                }

                return Json(new
                {
                    success = true,
                    message = "Message received. We also sent a confirmation to your email."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending quick support message");
                return StatusCode(500, new
                {
                    success = false,
                    message = "We could not send your message. Please try again."
                });
            }
        }
    }

    public class QuickSupportRequest
    {
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Email { get; set; }
        public string? SupportType { get; set; }
        public string? Message { get; set; }
        public string? PageUrl { get; set; }
    }
}
