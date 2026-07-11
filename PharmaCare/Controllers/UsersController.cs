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

            // Enforce Admin-only access control for all user management operations
            var userRole = HttpContext.Session.GetString("UserRole");
            Console.WriteLine($"Users controller access check: UserRole = {userRole}");

            if (string.IsNullOrEmpty(userRole) || userRole != "Admin")
            {
                Console.WriteLine("Access denied to Users controller - redirecting to login");
                context.Result = Redirect("/Account/Login");
            }
        }

        public async Task<ActionResult> Index()
        {
            ViewBag.AdminName = HttpContext.Session.GetString("UserName") ?? "Admin";
            var users = await _userRepository.GetAllAsync();
            return View(users);
        }

        public async Task<ActionResult> Details(int id)
        {
            ViewBag.AdminName = HttpContext.Session.GetString("UserName") ?? "Admin";
            var user = await _userRepository.GetByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }
            return View(user);
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

                    // Set default role to "User" if no role is specified during user creation
                    if (string.IsNullOrEmpty(user.Role))
                    {
                        user.Role = "User";
                    }

                    if (user.DateCreated == DateTime.MinValue)
                    {
                        user.DateCreated = DateTime.Now;
                    }

                    user.IsActive = true;

                    await _userRepository.CreateAsync(user);
                    TempData["SuccessMessage"] = "User created successfully!";

                    return Redirect("/Users/Index");
                }
                return View(user);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating user: {ex.Message}");
                ModelState.AddModelError("", "An error occurred while creating the user.");
                return View(user);
            }
        }

        public async Task<ActionResult> Edit(int id)
        {
            ViewBag.AdminName = HttpContext.Session.GetString("UserName") ?? "Admin";
            var user = await _userRepository.GetByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }
            return View(user);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(int id, User user)
        {
            try
            {
                ViewBag.AdminName = HttpContext.Session.GetString("UserName") ?? "Admin";

                // Preserve existing password from database to prevent accidental password overwrites during edit
                var existingUser = await _userRepository.GetByIdAsync(id);
                if (existingUser == null)
                {
                    return NotFound();
                }
                Console.WriteLine($"Loading Edit view for user {user.FirstName} with role: {user.Role}");

                user.Password = existingUser.Password;

                // Remove password validation errors since we're preserving the existing password
                if (ModelState.ContainsKey("Password"))
                {
                    ModelState.Remove("Password");
                }

                if (ModelState.IsValid)
                {
                    user.UserId = id;
                    var result = await _userRepository.UpdateAsync(user);
                    if (result == null)
                    {
                        return NotFound();
                    }

                    TempData["SuccessMessage"] = "User updated successfully!";
                    return RedirectToAction("Index");
                }
                else
                {
                    var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                    foreach (var error in errors)
                    {
                        Console.WriteLine($"Validation error: {error}");
                    }
                }
                return View(user);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating user: {ex.Message}");
                ModelState.AddModelError("", "An error occurred while updating the user.");
                return View(user);
            }
        }

        public async Task<ActionResult> Delete(int id)
        {
            ViewBag.AdminName = HttpContext.Session.GetString("UserName") ?? "Admin";
            var user = await _userRepository.GetByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }
            return View(user);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DeleteConfirmed(int id)
        {
            try
            {
                var result = await _userRepository.DeleteAsync(id);
                if (!result)
                {
                    return NotFound();
                }

                TempData["SuccessMessage"] = "User deleted successfully!";
                return Redirect("/Users/Index");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting user: {ex.Message}");
                TempData["ErrorMessage"] = "An error occurred while deleting the user.";
                return Redirect("/Users/Index");
            }
        }

        [HttpPost]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            var user = await _userRepository.GetByIdAsync(id);
            if (user == null)
            {
                return Json(new { success = false, message = "User not found" });
            }

            // Toggle user active status for account management without full deletion
            user.IsActive = !user.IsActive;
            var result = await _userRepository.UpdateAsync(user);

            if (result != null)
            {
                string statusText = user.IsActive ? "activated" : "deactivated";
                return Json(new { success = true, message = $"User {statusText} successfully", isActive = user.IsActive });
            }
            else
            {
                return Json(new { success = false, message = "Failed to update user status" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> Search(string searchTerm, string role)
        {
            ViewBag.AdminName = HttpContext.Session.GetString("UserName") ?? "Admin";

            var users = await _userRepository.GetAllAsync();
            var usersList = users.ToList();

            if (!string.IsNullOrEmpty(searchTerm))
            {
                // Perform case-insensitive search across multiple user fields for comprehensive filtering
                searchTerm = searchTerm.ToLower();
                usersList = usersList.Where(u =>
                    u.FirstName?.ToLower().Contains(searchTerm) == true ||
                    u.LastName?.ToLower().Contains(searchTerm) == true ||
                    u.Email?.ToLower().Contains(searchTerm) == true ||
                    u.PhoneNumber?.ToLower().Contains(searchTerm) == true)
                    .ToList();
            }

            if (!string.IsNullOrEmpty(role) && role != "all")
            {
                usersList = usersList.Where(u => u.Role == role).ToList();
            }

            return PartialView("_UserList", usersList);
        }

        public async Task<ActionResult> ResetPassword(int id)
        {
            ViewBag.AdminName = HttpContext.Session.GetString("UserName") ?? "Admin";

            var user = await _userRepository.GetByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            return View(user);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ResetPassword(int id, string newPassword, string confirmPassword)
        {
            ViewBag.AdminName = HttpContext.Session.GetString("UserName") ?? "Admin";

            try
            {
                if (string.IsNullOrEmpty(newPassword) || string.IsNullOrEmpty(confirmPassword))
                {
                    TempData["ErrorMessage"] = "Both password fields are required";
                    return RedirectToAction(nameof(ResetPassword), new { id });
                }

                if (newPassword != confirmPassword)
                {
                    TempData["ErrorMessage"] = "Passwords do not match";
                    return RedirectToAction(nameof(ResetPassword), new { id });
                }

                var user = await _userRepository.GetByIdAsync(id);
                if (user == null)
                {
                    return NotFound();
                }

                // Force password rehash by adding special prefix to trigger password update logic
                user.Password = "RESET_PASSWORD_" + newPassword;

                var result = await _userRepository.UpdateAsync(user);

                if (result == null)
                {
                    TempData["ErrorMessage"] = "Failed to reset password. Please try again.";
                    return RedirectToAction(nameof(ResetPassword), new { id });
                }

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error resetting password: {ex.Message}");
                TempData["ErrorMessage"] = "An error occurred while resetting the password.";
                return RedirectToAction(nameof(ResetPassword), new { id });
            }
        }
    }
}