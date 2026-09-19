namespace PharmaCare.Controllers
{
    public class AccountController : Controller
    {
        private readonly IUserRepository _userRepository;
        private readonly ICategoryRepository _categoryRepository;
        private readonly IEmailService _emailService;
        private readonly ILogger<AccountController> _logger;

        private static readonly TimeSpan CodeLifetime = TimeSpan.FromMinutes(15);

        public AccountController(
            IUserRepository userRepository,
            ICategoryRepository categoryRepository,
            IEmailService emailService,
            ILogger<AccountController> logger)
        {
            _userRepository = userRepository;
            _categoryRepository = categoryRepository;
            _emailService = emailService;
            _logger = logger;
        }

        private static string GenerateCode()
        {
            return System.Security.Cryptography.RandomNumberGenerator.GetInt32(0, 1_000_000)
                .ToString("D6");
        }

        private void LoadCategories()
        {
            ViewBag.Categories = _categoryRepository.View();
        }

        public IActionResult Login()
        {
            Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
            Response.Headers["Pragma"] = "no-cache";
            Response.Headers["Expires"] = "0";

            var userRole = HttpContext.Session.GetString("UserRole");
            if (!string.IsNullOrEmpty(userRole))
                return userRole == "Admin" || userRole == "Pharmacist"
                    ? Redirect("/Admin/Index")
                    : Redirect("/Marketplace");

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string Email, string Password)
        {
            try
            {
                Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
                Response.Headers["Pragma"] = "no-cache";
                Response.Headers["Expires"] = "0";

                if (string.IsNullOrEmpty(Email) || string.IsNullOrEmpty(Password))
                {
                    ModelState.AddModelError("", "Email and password are required");
                    return View();
                }

                var isValid = await _userRepository.ValidateCredentialsAsync(Email, Password);
                if (isValid)
                {
                    var user = await _userRepository.GetByEmailAsync(Email);
                    if (user != null && user.IsActive && !user.IsEmailVerified)
                    {
                        var code = GenerateCode();
                        await _userRepository.SetEmailVerificationCodeAsync(user.UserId, code, DateTime.UtcNow.Add(CodeLifetime));
                        await _emailService.SendVerificationCodeAsync(user.Email, user.FirstName, code);

                        TempData["PendingEmail"] = user.Email;
                        TempData["InfoMessage"] = "Please verify your email. We've sent you a new code.";
                        return RedirectToAction("VerifyEmail");
                    }

                    if (user != null && user.IsActive)
                    {
                        HttpContext.Session.SetInt32("UserId", user.UserId);
                        HttpContext.Session.SetString("UserName", $"{user.FirstName} {user.LastName}");
                        HttpContext.Session.SetString("UserRole", user.Role);

                        return Redirect(user.Role == "Admin" || user.Role == "Pharmacist"
                            ? "/Admin/Index?loggedIn=true"
                            : "/Marketplace?loggedIn=true");
                    }

                    if (user != null && !user.IsActive)
                    {
                        ModelState.AddModelError("", "Your account is inactive. Please contact an administrator.");
                        return View();
                    }
                }

                ModelState.AddModelError("", "Invalid email or password");
                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Login failed unexpectedly for {Email}", Email);
                ModelState.AddModelError("", "An error occurred during login. Please try again.");
                return View();
            }
        }

        public IActionResult Logout()
        {
            Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
            Response.Headers["Pragma"] = "no-cache";
            Response.Headers["Expires"] = "0";
            HttpContext.Session.Clear();
            return Redirect("/Account/Login");
        }

        public IActionResult Register() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(User user, string confirmPassword)
        {
            try
            {
                if (user.Password != confirmPassword)
                {
                    ModelState.AddModelError("", "Password and confirmation password do not match");
                    return View(user);
                }

                if (!PasswordPolicy.IsValid(user.Password))
                {
                    ModelState.AddModelError("Password", PasswordPolicy.Message);
                    return View(user);
                }

                if (ModelState.IsValid)
                {
                    var existing = await _userRepository.GetByEmailAsync(user.Email);
                    if (existing != null)
                    {
                        // Keep registration behavior generic so this screen cannot be used to
                        // discover whether an email already has an account.
                        if (!existing.IsEmailVerified)
                        {
                            var resendCode = GenerateCode();
                            await _userRepository.SetEmailVerificationCodeAsync(existing.UserId, resendCode, DateTime.UtcNow.Add(CodeLifetime));
                            await _emailService.SendVerificationCodeAsync(existing.Email, existing.FirstName, resendCode);
                        }

                        TempData["PendingEmail"] = user.Email.Trim();
                        TempData["InfoMessage"] = "If this email can be activated, a verification code has been sent. If you already have an account, sign in or use password recovery.";
                        return RedirectToAction("VerifyEmail");
                    }

                    user.Role = "Customer";
                    user.IsEmailVerified = false;

                    var newUser = await _userRepository.CreateAsync(user);
                    if (newUser != null)
                    {
                        var code = GenerateCode();
                        await _userRepository.SetEmailVerificationCodeAsync(newUser.UserId, code, DateTime.UtcNow.Add(CodeLifetime));
                        await _emailService.SendVerificationCodeAsync(newUser.Email, newUser.FirstName, code);

                        TempData["PendingEmail"] = newUser.Email;
                        TempData["InfoMessage"] = "Almost there! Enter the 6-digit code we emailed you to activate your account.";
                        return RedirectToAction("VerifyEmail");
                    }

                    return Redirect("/Account/Login");
                }

                return View(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Registration failed unexpectedly");
                ModelState.AddModelError("", "Registration could not be completed. Please try again.");
                return View(user);
            }
        }

        public IActionResult VerifyEmail()
        {
            var email = TempData["PendingEmail"] as string;
            if (string.IsNullOrEmpty(email)) return RedirectToAction("Login");

            TempData.Keep("PendingEmail");
            ViewBag.Email = email;
            ViewBag.InfoMessage = TempData["InfoMessage"];
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VerifyEmail(string Email, string Code)
        {
            ViewBag.Email = Email;

            if (string.IsNullOrEmpty(Email) || string.IsNullOrEmpty(Code))
            {
                ModelState.AddModelError("", "Please enter the code we emailed you.");
                return View();
            }

            var user = await _userRepository.GetByEmailAsync(Email);
            if (user == null)
            {
                ModelState.AddModelError("", "That code is invalid or has expired.");
                return View();
            }

            var confirmed = await _userRepository.ConfirmEmailAsync(user.UserId, Code.Trim());
            if (!confirmed)
            {
                ModelState.AddModelError("", "That code is invalid or has expired. Request a new one below.");
                return View();
            }

            HttpContext.Session.SetInt32("UserId", user.UserId);
            HttpContext.Session.SetString("UserName", $"{user.FirstName} {user.LastName}");
            HttpContext.Session.SetString("UserRole", user.Role);

            return user.Role == "Admin" || user.Role == "Pharmacist"
                ? Redirect("/Admin/Index")
                : Redirect("/Marketplace?verified=true");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResendVerificationCode(string Email)
        {
            var user = await _userRepository.GetByEmailAsync(Email);
            if (user != null && !user.IsEmailVerified)
            {
                var code = GenerateCode();
                await _userRepository.SetEmailVerificationCodeAsync(user.UserId, code, DateTime.UtcNow.Add(CodeLifetime));
                await _emailService.SendVerificationCodeAsync(user.Email, user.FirstName, code);
            }

            TempData["PendingEmail"] = Email;
            TempData["InfoMessage"] = "A new code is on its way. Check your inbox.";
            return RedirectToAction("VerifyEmail");
        }

        public IActionResult ForgotPassword() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(string Email)
        {
            ViewBag.Email = Email;

            if (string.IsNullOrWhiteSpace(Email))
            {
                ModelState.AddModelError("", "Enter the email address associated with your account.");
                return View();
            }

            var normalizedEmail = Email.Trim();
            var user = await _userRepository.GetByEmailAsync(normalizedEmail);

            if (user != null)
            {
                var code = GenerateCode();
                await _userRepository.SetPasswordResetCodeAsync(user.UserId, code, DateTime.UtcNow.Add(CodeLifetime));
                await _emailService.SendPasswordResetCodeAsync(user.Email, user.FirstName, code);
            }

            // Keep the response generic so the recovery screen does not reveal whether an email is registered.
            TempData["ResetEmail"] = normalizedEmail;
            TempData["InfoMessage"] = "If an account exists for this email, a 6-digit reset code has been sent. The code expires in 15 minutes.";
            return RedirectToAction("ResetPassword");
        }

        public IActionResult ResetPassword()
        {
            var email = TempData["ResetEmail"] as string;
            if (string.IsNullOrEmpty(email)) return RedirectToAction("ForgotPassword");

            TempData.Keep("ResetEmail");
            ViewBag.Email = email;
            ViewBag.InfoMessage = TempData["InfoMessage"];
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResendPasswordResetCode(string Email)
        {
            if (!string.IsNullOrWhiteSpace(Email))
            {
                var normalizedEmail = Email.Trim();
                var user = await _userRepository.GetByEmailAsync(normalizedEmail);
                if (user != null)
                {
                    var code = GenerateCode();
                    await _userRepository.SetPasswordResetCodeAsync(user.UserId, code, DateTime.UtcNow.Add(CodeLifetime));
                    await _emailService.SendPasswordResetCodeAsync(user.Email, user.FirstName, code);
                }

                TempData["ResetEmail"] = normalizedEmail;
                TempData["InfoMessage"] = "If an account exists for this email, a new reset code has been sent.";
            }

            return RedirectToAction("ResetPassword");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(string Email, string Code, string NewPassword, string ConfirmPassword)
        {
            ViewBag.Email = Email;

            if (string.IsNullOrEmpty(Email) || string.IsNullOrEmpty(Code) || string.IsNullOrEmpty(NewPassword))
            {
                ModelState.AddModelError("", "All fields are required.");
                return View();
            }

            if (NewPassword != ConfirmPassword)
            {
                ModelState.AddModelError("", "New password and confirmation do not match.");
                return View();
            }

            if (!PasswordPolicy.IsValid(NewPassword))
            {
                ModelState.AddModelError("", PasswordPolicy.Message);
                return View();
            }

            var user = await _userRepository.GetByEmailAsync(Email);
            if (user == null)
            {
                ModelState.AddModelError("", "That code is invalid or has expired. Request a new code and try again.");
                return View();
            }

            var reset = await _userRepository.ResetPasswordWithCodeAsync(user.UserId, Code.Trim(), NewPassword);
            if (!reset)
            {
                ModelState.AddModelError("", "That code is invalid or has expired. Request a new code and try again.");
                return View();
            }

            TempData["SuccessMessage"] = "Your password has been reset. Please sign in.";
            return RedirectToAction("Login");
        }

        public async Task<IActionResult> Profile()
        {
            LoadCategories();
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return Redirect("/Account/Login");

            var user = await _userRepository.GetByIdAsync(userId.Value);
            return user == null ? NotFound() : View(user);
        }

        public async Task<IActionResult> EditProfile()
        {
            LoadCategories();
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return Redirect("/Account/Login");

            var user = await _userRepository.GetByIdAsync(userId.Value);
            return user == null ? NotFound() : View(user);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditProfile(User user)
        {
            try
            {
                LoadCategories();
                if (ModelState.IsValid)
                {
                    var userId = HttpContext.Session.GetInt32("UserId");
                    if (userId == null) return Redirect("/Account/Login");

                    var currentUser = await _userRepository.GetByIdAsync(userId.Value);
                    if (currentUser == null) return NotFound();

                    var requestedEmail = (user.Email ?? string.Empty).Trim();
                    var emailChanged = !string.Equals(currentUser.Email, requestedEmail, StringComparison.OrdinalIgnoreCase);
                    if (emailChanged)
                    {
                        var duplicate = await _userRepository.GetByEmailAsync(requestedEmail);
                        if (duplicate != null && duplicate.UserId != currentUser.UserId)
                        {
                            ModelState.AddModelError("Email", "This email cannot be used. Try another address or use account recovery.");
                            return View(user);
                        }
                    }

                    currentUser.FirstName = user.FirstName;
                    currentUser.LastName = user.LastName;
                    currentUser.PhoneNumber = user.PhoneNumber;
                    currentUser.Address = user.Address;
                    currentUser.City = user.City;

                    var result = await _userRepository.UpdateAsync(currentUser);
                    if (result != null && emailChanged)
                    {
                        var code = GenerateCode();
                        var changed = await _userRepository.ChangeEmailAndRequireVerificationAsync(currentUser.UserId, requestedEmail, code, DateTime.UtcNow.Add(CodeLifetime));
                        if (!changed)
                        {
                            ModelState.AddModelError("Email", "This email cannot be used. Try another address.");
                            return View(user);
                        }

                        await _emailService.SendVerificationCodeAsync(requestedEmail, currentUser.FirstName, code);
                        HttpContext.Session.Clear();
                        TempData["PendingEmail"] = requestedEmail;
                        TempData["InfoMessage"] = "Email changed. Verify the new address before signing in again.";
                        return RedirectToAction("VerifyEmail");
                    }
                    if (result == null)
                    {
                        TempData["ErrorMessage"] = "Failed to update profile. Please try again.";
                        return View(user);
                    }

                    HttpContext.Session.SetString("UserName", $"{user.FirstName} {user.LastName}");
                    TempData["SuccessMessage"] = "Profile updated successfully!";
                    return Redirect("/Account/Profile");
                }

                return View(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Profile update failed for signed-in user");
                TempData["ErrorMessage"] = "An error occurred while updating your profile. Please try again.";
                return View(user);
            }
        }

        public IActionResult ChangePassword()
        {
            LoadCategories();
            return HttpContext.Session.GetInt32("UserId") == null ? Redirect("/Account/Login") : View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(string currentPassword, string newPassword, string confirmPassword)
        {
            LoadCategories();
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return Redirect("/Account/Login");

            var user = await _userRepository.GetByIdAsync(userId.Value);
            if (user == null) return NotFound();

            if (!await _userRepository.ValidateCredentialsAsync(user.Email, currentPassword))
            {
                TempData["ErrorMessage"] = "Current password is incorrect";
                return View();
            }

            if (newPassword != confirmPassword)
            {
                TempData["ErrorMessage"] = "New password and confirmation do not match.";
                return View();
            }

            if (!PasswordPolicy.IsValid(newPassword))
            {
                TempData["ErrorMessage"] = PasswordPolicy.Message;
                return View();
            }

            if (!await _userRepository.SetPasswordAsync(user.UserId, newPassword))
            {
                TempData["ErrorMessage"] = "Failed to update password. Please try again.";
                return View();
            }

            TempData["SuccessMessage"] = "Password changed successfully!";
            return Redirect("/Account/Profile");
        }

        [HttpGet]
        public JsonResult GetCategories()
        {
            return Json(_categoryRepository.View());
        }
    }
}
