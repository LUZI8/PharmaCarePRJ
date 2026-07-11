namespace PharmaCare.Controllers
{
    /* Controller handling user authentication, registration, and profile management */
    public class AccountController : Controller
    {
        private readonly IUserRepository _userRepository;
        private readonly ICategoryRepository _categoryRepository;
        private readonly IEmailService _emailService;

        /* Verification and reset codes stay valid for 15 minutes after being issued */
        private static readonly TimeSpan CodeLifetime = TimeSpan.FromMinutes(15);

        /* Constructor with dependency injection for user, category, and email operations */
        public AccountController(
            IUserRepository userRepository,
            ICategoryRepository categoryRepository,
            IEmailService emailService)
        {
            _userRepository = userRepository;
            _categoryRepository = categoryRepository;
            _emailService = emailService;
        }

        /* Generate a random 6-digit numeric code for email verification / password reset */
        private static string GenerateCode()
        {
            return System.Security.Cryptography.RandomNumberGenerator.GetInt32(0, 1_000_000)
                .ToString("D6");
        }

        /* Helper method to load categories into ViewBag for navigation display */
        private void LoadCategories()
        {
            var categories = _categoryRepository.View();
            ViewBag.Categories = categories;
        }

        /* GET: Login page with cache prevention and session validation */
        public IActionResult Login()
        {
            /* Prevent browser caching of login page for security */
            Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
            Response.Headers["Pragma"] = "no-cache";
            Response.Headers["Expires"] = "0";

            /* Check if user is already logged in and redirect appropriately */
            var userRole = HttpContext.Session.GetString("UserRole");
            if (!string.IsNullOrEmpty(userRole))
            {
                if (userRole == "Admin")
                {
                    return Redirect("/Admin/Index");
                }
                else
                {
                    return Redirect("/FrontEnd/Index");
                }
            }

            Console.WriteLine("GET Login page accessed");

            return View();
        }

        /* POST: Process login form submission with validation and session management */
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string Email, string Password)
        {
            try
            {
                /* Prevent caching for security */
                Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
                Response.Headers["Pragma"] = "no-cache";
                Response.Headers["Expires"] = "0";

                /* Basic input validation */
                if (string.IsNullOrEmpty(Email) || string.IsNullOrEmpty(Password))
                {
                    ModelState.AddModelError("", "Email and password are required");
                    return View();
                }

                /* Validate credentials using repository */
                var isValid = await _userRepository.ValidateCredentialsAsync(Email, Password);

                if (isValid)
                {
                    var user = await _userRepository.GetByEmailAsync(Email);

                    /* Require a verified email before allowing sign-in. Re-issue a fresh code and
                       send the user to the verification screen if they haven't confirmed yet. */
                    if (user != null && user.IsActive && !user.IsEmailVerified)
                    {
                        var code = GenerateCode();
                        await _userRepository.SetEmailVerificationCodeAsync(
                            user.UserId, code, DateTime.UtcNow.Add(CodeLifetime));
                        await _emailService.SendVerificationCodeAsync(user.Email, user.FirstName, code);

                        TempData["PendingEmail"] = user.Email;
                        TempData["InfoMessage"] = "Please verify your email. We've sent you a new code.";
                        return RedirectToAction("VerifyEmail");
                    }

                    if (user != null && user.IsActive)
                    {
                        /* Store user authentication data in session */
                        HttpContext.Session.SetInt32("UserId", user.UserId);
                        HttpContext.Session.SetString("UserName", $"{user.FirstName} {user.LastName}");
                        HttpContext.Session.SetString("UserRole", user.Role);

                        /* Role-based redirection after successful login */
                        string redirectUrl;
                        if (user.Role == "Admin" || user.Role == "Pharmacist")
                        {
                            redirectUrl = "/Admin/Index?loggedIn=true";
                        }
                        else
                        {
                            redirectUrl = "/FrontEnd/Index?loggedIn=true";
                        }
                        return Redirect(redirectUrl);
                    }
                    else if (user != null && !user.IsActive)
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
                Console.WriteLine($"Exception during login: {ex.Message}");
                ModelState.AddModelError("", "An error occurred during login. Please try again.");
                return View();
            }
        }

        /* Logout action with session clearing and cache prevention */
        public IActionResult Logout()
        {
            Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
            Response.Headers["Pragma"] = "no-cache";
            Response.Headers["Expires"] = "0";

            /* Clear all session data */
            HttpContext.Session.Clear();

            return Redirect("/Account/Login");
        }

        /* GET: Registration form display */
        public IActionResult Register()
        {
            return View();
        }

        /* POST: Process user registration with comprehensive validation */
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(User user, string confirmPassword)
        {
            try
            {
                /* Password confirmation validation */
                if (user.Password != confirmPassword)
                {
                    ModelState.AddModelError("", "Password and confirmation password do not match");
                    return View(user);
                }

                /* Password complexity validation rules */
                if (user.Password.Length < 8)
                {
                    ModelState.AddModelError("Password", "Password must be at least 8 characters long");
                    return View(user);
                }

                if (!user.Password.Any(char.IsUpper))
                {
                    ModelState.AddModelError("Password", "Password must contain at least one uppercase letter");
                    return View(user);
                }

                if (!user.Password.Any(c => !char.IsLetterOrDigit(c)))
                {
                    ModelState.AddModelError("Password", "Password must contain at least one special character");
                    return View(user);
                }

                if (ModelState.IsValid)
                {
                    /* Check for existing email to prevent duplicates */
                    if (await _userRepository.UserExistsAsync(user.Email))
                    {
                        ModelState.AddModelError("Email", "Email already exists");
                        return View(user);
                    }

                    /* Never trust a client-submitted role: self-registration is always a Customer.
                       (Elevated roles are assigned by an admin, not chosen at sign-up.) */
                    user.Role = "Customer";

                    /* New accounts start unverified until they confirm their email with a code */
                    user.IsEmailVerified = false;

                    var newUser = await _userRepository.CreateAsync(user);

                    if (newUser != null)
                    {
                        /* Issue and email a verification code, then send the user to the verify screen */
                        var code = GenerateCode();
                        await _userRepository.SetEmailVerificationCodeAsync(
                            newUser.UserId, code, DateTime.UtcNow.Add(CodeLifetime));
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
                Console.WriteLine($"Registration error: {ex.Message}");
                ModelState.AddModelError("", $"Registration error: {ex.Message}");
                return View(user);
            }
        }

        /* GET: Email verification screen. Email is carried from registration/login via TempData. */
        public IActionResult VerifyEmail()
        {
            var email = TempData["PendingEmail"] as string;
            if (string.IsNullOrEmpty(email))
            {
                /* No pending verification in flight - fall back to login */
                return RedirectToAction("Login");
            }

            /* Keep values available across the render and a potential resend */
            TempData.Keep("PendingEmail");
            ViewBag.Email = email;
            ViewBag.InfoMessage = TempData["InfoMessage"];
            return View();
        }

        /* POST: Confirm the emailed verification code and, on success, sign the user in. */
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
                ModelState.AddModelError("", "Account not found.");
                return View();
            }

            var confirmed = await _userRepository.ConfirmEmailAsync(user.UserId, Code.Trim());
            if (!confirmed)
            {
                ModelState.AddModelError("", "That code is invalid or has expired. Request a new one below.");
                return View();
            }

            /* Verified - establish the session and send them on their way */
            HttpContext.Session.SetInt32("UserId", user.UserId);
            HttpContext.Session.SetString("UserName", $"{user.FirstName} {user.LastName}");
            HttpContext.Session.SetString("UserRole", user.Role);

            if (user.Role == "Admin" || user.Role == "Pharmacist")
            {
                return Redirect("/Admin/Index");
            }
            return Redirect("/FrontEnd/Index?verified=true");
        }

        /* POST: Re-issue and resend a verification code for a pending account. */
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResendVerificationCode(string Email)
        {
            var user = await _userRepository.GetByEmailAsync(Email);
            if (user != null && !user.IsEmailVerified)
            {
                var code = GenerateCode();
                await _userRepository.SetEmailVerificationCodeAsync(
                    user.UserId, code, DateTime.UtcNow.Add(CodeLifetime));
                await _emailService.SendVerificationCodeAsync(user.Email, user.FirstName, code);
            }

            /* Always report success to avoid revealing which emails are registered */
            TempData["PendingEmail"] = Email;
            TempData["InfoMessage"] = "A new code is on its way. Check your inbox.";
            return RedirectToAction("VerifyEmail");
        }

        /* GET: Forgot-password form. Collects email plus the last remembered password. */
        public IActionResult ForgotPassword()
        {
            return View();
        }

        /* POST: Verify identity (email + last remembered password), then email a reset code. */
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(string Email, string CurrentPassword)
        {
            ViewBag.Email = Email;

            if (string.IsNullOrEmpty(Email) || string.IsNullOrEmpty(CurrentPassword))
            {
                ModelState.AddModelError("", "Enter your email and your last remembered password.");
                return View();
            }

            /* Confirm the email + old password combination before issuing a reset code */
            var isValid = await _userRepository.ValidateCredentialsAsync(Email, CurrentPassword);
            if (!isValid)
            {
                ModelState.AddModelError("", "The email and password you entered don't match our records.");
                return View();
            }

            var user = await _userRepository.GetByEmailAsync(Email);
            var code = GenerateCode();
            await _userRepository.SetPasswordResetCodeAsync(
                user.UserId, code, DateTime.UtcNow.Add(CodeLifetime));
            await _emailService.SendPasswordResetCodeAsync(user.Email, user.FirstName, code);

            TempData["ResetEmail"] = user.Email;
            TempData["InfoMessage"] = "We've emailed you a reset code. Enter it below with your new password.";
            return RedirectToAction("ResetPassword");
        }

        /* GET: Reset-password form (code + new password). Email carried via TempData. */
        public IActionResult ResetPassword()
        {
            var email = TempData["ResetEmail"] as string;
            if (string.IsNullOrEmpty(email))
            {
                return RedirectToAction("ForgotPassword");
            }

            TempData.Keep("ResetEmail");
            ViewBag.Email = email;
            ViewBag.InfoMessage = TempData["InfoMessage"];
            return View();
        }

        /* POST: Validate the reset code and password rules, then set the new password. */
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(string Email, string Code, string NewPassword, string ConfirmPassword)
        {
            ViewBag.Email = Email;

            if (string.IsNullOrEmpty(Email) || string.IsNullOrEmpty(Code) ||
                string.IsNullOrEmpty(NewPassword))
            {
                ModelState.AddModelError("", "All fields are required.");
                return View();
            }

            if (NewPassword != ConfirmPassword)
            {
                ModelState.AddModelError("", "New password and confirmation do not match.");
                return View();
            }

            /* Enforce the same password complexity rules used at registration */
            if (NewPassword.Length < 8 || !NewPassword.Any(char.IsUpper) ||
                !NewPassword.Any(c => !char.IsLetterOrDigit(c)))
            {
                ModelState.AddModelError("", "Password must be at least 8 characters and include an uppercase letter and a special character.");
                return View();
            }

            var user = await _userRepository.GetByEmailAsync(Email);
            if (user == null)
            {
                ModelState.AddModelError("", "Account not found.");
                return View();
            }

            var reset = await _userRepository.ResetPasswordWithCodeAsync(user.UserId, Code.Trim(), NewPassword);
            if (!reset)
            {
                ModelState.AddModelError("", "That code is invalid or has expired. Start over to get a new one.");
                return View();
            }

            TempData["SuccessMessage"] = "Your password has been reset. Please sign in.";
            return RedirectToAction("Login");
        }

        /* Display user profile information with authentication check */
        public async Task<IActionResult> Profile()
        {
            LoadCategories();

            /* Authentication validation */
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return Redirect("/Account/Login");
            }

            var user = await _userRepository.GetByIdAsync(userId.Value);
            if (user == null)
            {
                return NotFound();
            }

            return View(user);
        }

        /* GET: Edit profile form with current user data */
        public async Task<IActionResult> EditProfile()
        {
            LoadCategories();

            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return Redirect("/Account/Login");
            }

            var user = await _userRepository.GetByIdAsync(userId.Value);
            if (user == null)
            {
                return NotFound();
            }

            return View(user);
        }

        /* POST: Process profile update with selective field updating */
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
                    if (userId == null)
                    {
                        return Redirect("/Account/Login");
                    }

                    var currentUser = await _userRepository.GetByIdAsync(userId.Value);
                    if (currentUser == null)
                    {
                        return NotFound();
                    }

                    /* Update only editable fields to preserve sensitive data */
                    currentUser.FirstName = user.FirstName;
                    currentUser.LastName = user.LastName;
                    currentUser.Email = user.Email;
                    currentUser.PhoneNumber = user.PhoneNumber;
                    currentUser.Address = user.Address;
                    currentUser.City = user.City;

                    var result = await _userRepository.UpdateAsync(currentUser);

                    if (result == null)
                    {
                        TempData["ErrorMessage"] = "Failed to update profile. Please try again.";
                        return View(user);
                    }

                    /* Update session data with new name */
                    HttpContext.Session.SetString("UserName", $"{user.FirstName} {user.LastName}");

                    TempData["SuccessMessage"] = "Profile updated successfully!";
                    return Redirect("/Account/Profile");
                }
                return View(user);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"EditProfile error: {ex.Message}");
                TempData["ErrorMessage"] = "An error occurred while updating your profile. Please try again.";
                return View(user);
            }
        }

        /* GET: Change password form display */
        public IActionResult ChangePassword()
        {
            LoadCategories();

            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return Redirect("/Account/Login");
            }

            return View();
        }

        /* POST: Process password change with current password verification */
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(string currentPassword, string newPassword, string confirmPassword)
        {
            LoadCategories();

            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return Redirect("/Account/Login");
            }

            var user = await _userRepository.GetByIdAsync(userId.Value);
            if (user == null)
            {
                return NotFound();
            }

            /* Verify current password before allowing change */
            var isValid = await _userRepository.ValidateCredentialsAsync(user.Email, currentPassword);
            if (!isValid)
            {
                TempData["ErrorMessage"] = "Current password is incorrect";
                return View();
            }

            /* Force password rehash with special prefix to trigger repository update */
            user.Password = "RESET_PASSWORD_" + newPassword;
            var result = await _userRepository.UpdateAsync(user);

            if (result == null)
            {
                TempData["ErrorMessage"] = "Failed to update password. Please try again.";
                return View();
            }

            TempData["SuccessMessage"] = "Password changed successfully!";
            return Redirect("/Account/Profile");
        }

        /* API endpoint to return categories as JSON for frontend consumption */
        [HttpGet]
        public JsonResult GetCategories()
        {
            var categories = _categoryRepository.View();
            return Json(categories);
        }
    }
}