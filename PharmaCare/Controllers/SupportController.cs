using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PharmaCare.Data;
using PharmaCare.Models;

namespace PharmaCare.Controllers
{
    public class SupportController : Controller
    {
        private readonly DataDbContext _context;
        private readonly ILogger<SupportController> _logger;

        public SupportController(DataDbContext context, ILogger<SupportController> logger)
        {
            _context = context;
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
                    email = ""
                });
            }

            var user = await _context.User
                .AsNoTracking()
                .Where(u => u.UserId == userId.Value)
                .Select(u => new
                {
                    u.FirstName,
                    u.LastName,
                    u.Email
                })
                .FirstOrDefaultAsync();

            if (user == null)
            {
                return Json(new
                {
                    isLoggedIn = false,
                    firstName = "",
                    lastName = "",
                    email = ""
                });
            }

            return Json(new
            {
                isLoggedIn = true,
                firstName = user.FirstName ?? "",
                lastName = user.LastName ?? "",
                email = user.Email ?? ""
            });
        }

        [HttpPost]
        public async Task<IActionResult> Send([FromBody] QuickSupportRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Message))
            {
                return BadRequest(new { success = false, message = "Please enter a message." });
            }

            if (string.IsNullOrWhiteSpace(request.Email))
            {
                return BadRequest(new { success = false, message = "Please enter your email address." });
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

                var email = !string.IsNullOrWhiteSpace(request.Email)
                    ? request.Email.Trim()
                    : user?.Email ?? "";

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

                return Json(new
                {
                    success = true,
                    message = "Your message was sent to the PharmaCare team."
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
