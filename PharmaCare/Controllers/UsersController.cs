namespace PharmaCare.Controllers
{
    public class UsersController : Controller
    {
        private readonly IUserRepository _userRepository;

        public UsersController(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            base.OnActionExecuting(context);
            var userRole = HttpContext.Session.GetString("UserRole");
            if (string.IsNullOrEmpty(userRole) || userRole != "Admin")
                context.Result = Redirect("/Account/Login");
        }

        public async Task<ActionResult> Index()
        {
            ViewBag.AdminName = HttpContext.Session.GetString("UserName") ?? "Admin";
            return View(await _userRepository.GetAllAsync());
        }

        public async Task<ActionResult> Details(int id)
        {
            ViewBag.AdminName = HttpContext.Session.GetString("UserName") ?? "Admin";
            var user = await _userRepository.GetByIdAsync(id);
            return user == null ? NotFound() : View(user);
        }

        public ActionResult Create()
        {
            ViewBag.AdminName = HttpContext.Session.GetString("UserName") ?? "Admin";
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create(User user)
        {
            try
            {
                ViewBag.AdminName = HttpContext.Session.GetString("UserName") ?? "Admin";
                if (ModelState.IsValid)
                {
                    if (await _userRepository.UserExistsAsync(user.Email))
                    {
                        ModelState.AddModelError("Email", "Email already exists");
                        return View(user);
                    }

                    if (string.IsNullOrEmpty(user.Role)) user.Role = "Customer";
                    if (user.DateCreated == DateTime.MinValue) user.DateCreated = DateTime.UtcNow;
                    user.IsActive = true;
                    await _userRepository.CreateAsync(user);
                    TempData["SuccessMessage"] = "User created successfully!";
                    return Redirect("/Users/Index");
                }
                return View(user);
            }
            catch
            {
                ModelState.AddModelError("", "An error occurred while creating the user.");
                return View(user);
            }
        }

        public async Task<ActionResult> Edit(int id)
        {
            ViewBag.AdminName = HttpContext.Session.GetString("UserName") ?? "Admin";
            var user = await _userRepository.GetByIdAsync(id);
            return user == null ? NotFound() : View(user);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(int id, User user)
        {
            try
            {
                ViewBag.AdminName = HttpContext.Session.GetString("UserName") ?? "Admin";
                var existingUser = await _userRepository.GetByIdAsync(id);
                if (existingUser == null) return NotFound();

                user.Password = existingUser.Password;
                ModelState.Remove("Password");

                if (ModelState.IsValid)
                {
                    user.UserId = id;
                    var result = await _userRepository.UpdateAsync(user);
                    if (result == null) return NotFound();
                    TempData["SuccessMessage"] = "User updated successfully!";
                    return RedirectToAction("Index");
                }
                return View(user);
            }
            catch
            {
                ModelState.AddModelError("", "An error occurred while updating the user.");
                return View(user);
            }
        }

        public async Task<ActionResult> Delete(int id)
        {
            ViewBag.AdminName = HttpContext.Session.GetString("UserName") ?? "Admin";
            var user = await _userRepository.GetByIdAsync(id);
            return user == null ? NotFound() : View(user);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DeleteConfirmed(int id)
        {
            try
            {
                if (!await _userRepository.DeleteAsync(id)) return NotFound();
                TempData["SuccessMessage"] = "User deleted successfully!";
                return Redirect("/Users/Index");
            }
            catch
            {
                TempData["ErrorMessage"] = "An error occurred while deleting the user.";
                return Redirect("/Users/Index");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            var user = await _userRepository.GetByIdAsync(id);
            if (user == null) return Json(new { success = false, message = "User not found" });

            user.IsActive = !user.IsActive;
            var result = await _userRepository.UpdateAsync(user);
            if (result != null)
            {
                string statusText = user.IsActive ? "activated" : "deactivated";
                return Json(new { success = true, message = $"User {statusText} successfully", isActive = user.IsActive });
            }
            return Json(new { success = false, message = "Failed to update user status" });
        }

        [HttpGet]
        public async Task<IActionResult> Search(string searchTerm, string role)
        {
            ViewBag.AdminName = HttpContext.Session.GetString("UserName") ?? "Admin";
            var usersList = (await _userRepository.GetAllAsync()).ToList();

            if (!string.IsNullOrEmpty(searchTerm))
            {
                searchTerm = searchTerm.ToLower();
                usersList = usersList.Where(u =>
                    u.FirstName?.ToLower().Contains(searchTerm) == true ||
                    u.LastName?.ToLower().Contains(searchTerm) == true ||
                    u.Email?.ToLower().Contains(searchTerm) == true ||
                    u.PhoneNumber?.ToLower().Contains(searchTerm) == true).ToList();
            }

            if (!string.IsNullOrEmpty(role) && role != "all")
                usersList = usersList.Where(u => u.Role == role).ToList();

            return PartialView("_UserList", usersList);
        }

        public async Task<ActionResult> ResetPassword(int id)
        {
            ViewBag.AdminName = HttpContext.Session.GetString("UserName") ?? "Admin";
            var user = await _userRepository.GetByIdAsync(id);
            return user == null ? NotFound() : View(user);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ResetPassword(int id, string newPassword, string confirmPassword)
        {
            ViewBag.AdminName = HttpContext.Session.GetString("UserName") ?? "Admin";
            try
            {
                if (string.IsNullOrWhiteSpace(newPassword) || string.IsNullOrWhiteSpace(confirmPassword))
                {
                    TempData["ErrorMessage"] = "Both password fields are required";
                    return RedirectToAction(nameof(ResetPassword), new { id });
                }
                if (newPassword != confirmPassword)
                {
                    TempData["ErrorMessage"] = "Passwords do not match";
                    return RedirectToAction(nameof(ResetPassword), new { id });
                }
                if (newPassword.Length < 8 || !newPassword.Any(char.IsUpper) || !newPassword.Any(c => !char.IsLetterOrDigit(c)))
                {
                    TempData["ErrorMessage"] = "Password must be at least 8 characters and include an uppercase letter and a special character.";
                    return RedirectToAction(nameof(ResetPassword), new { id });
                }

                if (!await _userRepository.SetPasswordAsync(id, newPassword))
                {
                    TempData["ErrorMessage"] = "Failed to reset password. Please try again.";
                    return RedirectToAction(nameof(ResetPassword), new { id });
                }

                TempData["SuccessMessage"] = "Password reset successfully!";
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                TempData["ErrorMessage"] = "An error occurred while resetting the password.";
                return RedirectToAction(nameof(ResetPassword), new { id });
            }
        }
    }
}
