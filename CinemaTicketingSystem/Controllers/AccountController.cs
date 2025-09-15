using CinemaTicketingSystem.Models;
using CinemaTicketingSystem.Services;
using CinemaTicketingSystem.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CinemaTicketingSystem.Controllers
{
    public class AccountController : Controller
    {
        private readonly CinemaDbContext _context;
        private readonly IEmailService _emailService;

        public AccountController(CinemaDbContext context, IEmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        // GET: /Account/Register
        public IActionResult Register()
        {
            return View();
        }

        // POST: /Account/Register
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                // Check if username or email already exists
                if (await _context.Users.AnyAsync(u => u.Username == model.Username))
                {
                    ModelState.AddModelError("", "Username already taken.");
                    return View(model);
                }
                if (await _context.Users.AnyAsync(u => u.Email == model.Email))
                {
                    ModelState.AddModelError("", "Email already registered.");
                    return View(model);
                }

                // Hash the password
                string hashedPassword = BCrypt.Net.BCrypt.HashPassword(model.Password);

                // Create new user
                var user = new User
                {
                    Username = model.Username,
                    Email = model.Email,
                    Password = hashedPassword,
                    Role = "Customer", // Default role
                    RegistrationDate = DateTime.Now
                };

                // Save to database
                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                // Automatically log the user in after registration
                HttpContext.Session.SetInt32("UserId", user.UserId);
                HttpContext.Session.SetString("Username", user.Username);
                HttpContext.Session.SetString("UserRole", user.Role);

                TempData["SuccessMessage"] = "Registration successful! Welcome to Cinema Paradise.";
                return RedirectToAction("Index", "Home");
            }
            return View(model);
        }

        // GET: /Account/Login
        public IActionResult Login()
        {
            return View();
        }

        // POST: /Account/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {

                // 1️⃣ Verify CAPTCHA first
                var captchaResponse = HttpContext.Request.Form["g-recaptcha-response"];
                if (!await VerifyCaptcha(captchaResponse))
                {
                    ModelState.AddModelError("", "CAPTCHA validation failed. Please try again.");
                    return View(model);
                }

                // Find user by username OR email
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == model.Username || u.Email == model.Username);

                // Check if user exists and password is correct
                if (user != null && BCrypt.Net.BCrypt.Verify(model.Password, user.Password))
                {
                    if (!user.IsActive)
                    {
                        ModelState.AddModelError("", "Account is deactivated. Please contact support.");
                        return View(model);
                    }

                    // Login successful! Reset failed attempts
                    user.FailedLoginAttempts = 0;
                    user.LastLogin = DateTime.Now;
                    await _context.SaveChangesAsync();

                    // Create the user's identity and log them in
                    // This is a simplified session-based login for now
                    HttpContext.Session.SetInt32("UserId", user.UserId);
                    HttpContext.Session.SetString("Username", user.Username);
                    HttpContext.Session.SetString("UserRole", user.Role);

                    return RedirectToAction("Index", "Home");
                }
                else
                {
                    // Login failed - increment failed attempts if user exists
                    if (user != null)
                    {
                        user.FailedLoginAttempts++;
                        if (user.FailedLoginAttempts >= 3)
                        {
                            user.IsActive = false; // Lock account
                            ModelState.AddModelError("", "Account locked due to too many failed attempts.");
                        }
                        else
                        {
                            ModelState.AddModelError("", "Invalid login attempt.");
                        }
                        await _context.SaveChangesAsync();
                    }
                    else
                    {
                        ModelState.AddModelError("", "Invalid login attempt.");
                    }
                }
            }
            return View(model);
        }

        // POST: /Account/Logout
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Logout()
        {
            // Clear the session
            HttpContext.Session.Clear();
            return RedirectToAction("Index", "Home");
        }

        // GET: Account/ForgotPassword
        public IActionResult ForgotPassword()
        {
            return View();
        }

        // POST: Account/ForgotPassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == model.Email);
                if (user != null && user.IsActive)
                {
                    // Generate reset token (simplified version - use proper token generation in real applications)
                    var resetToken = Guid.NewGuid().ToString();
                    user.ResetToken = resetToken;
                    user.ResetTokenExpiry = DateTime.Now.AddHours(1);

                    _context.Update(user);
                    await _context.SaveChangesAsync();

                    // Send reset email (simplified)
                    var resetLink = Url.Action("ResetPassword", "Account", new { token = resetToken, email = model.Email }, Request.Scheme);
                    await _emailService.SendPasswordResetAsync(user.Email, user.Username, resetLink);

                    TempData["SuccessMessage"] = "Password reset instructions have been sent to your email.";
                    return RedirectToAction("Login");
                }

                // Always show success message even if email doesn't exist (for security)
                TempData["SuccessMessage"] = "If your email exists in our system, reset instructions have been sent.";
                return RedirectToAction("Login");
            }
            return View(model);
        }

        // GET: Account/ResetPassword
        public IActionResult ResetPassword(string token, string email)
        {
            if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(email))
            {
                return RedirectToAction("Login");
            }

            var model = new ResetPasswordViewModel
            {
                Token = token,
                Email = email
            };

            return View(model);
        }

        // POST: Account/ResetPassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == model.Email && u.ResetToken == model.Token && u.ResetTokenExpiry > DateTime.Now);

                if (user != null)
                {
                    // Reset password
                    user.Password = BCrypt.Net.BCrypt.HashPassword(model.Password);
                    user.ResetToken = null;
                    user.ResetTokenExpiry = null;

                    _context.Update(user);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Password has been reset successfully. Please login with your new password.";
                    return RedirectToAction("Login");
                }

                ModelState.AddModelError("", "Invalid or expired reset token.");
            }
            return View(model);
        }

        // GET: Account/Profile
        public async Task<IActionResult> Profile()
        {
            if (HttpContext.Session.GetInt32("UserId") == null)
            {
                TempData["ErrorMessage"] = "Please log in to access your profile.";
                return RedirectToAction("Login");
            }

            var userId = HttpContext.Session.GetInt32("UserId").Value;
            var user = await _context.Users.FindAsync(userId);

            if (user == null)
            {
                TempData["ErrorMessage"] = "User not found.";
                return RedirectToAction("Login");
            }

            var totalBookings = await _context.Bookings.CountAsync(b => b.UserId == userId);

            var viewModel = new ProfileViewModel
            {
                Username = user.Username,
                Email = user.Email,
                RegistrationDate = user.RegistrationDate,
                TotalBookings = totalBookings
            };

            return View(viewModel);
        }

        // GET: Account/ChangePassword
        public IActionResult ChangePassword()
        {
            if (HttpContext.Session.GetInt32("UserId") == null)
            {
                TempData["ErrorMessage"] = "Please log in to change your password.";
                return RedirectToAction("Login");
            }
            return View();
        }

        // POST: Account/ChangePassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (HttpContext.Session.GetInt32("UserId") == null)
            {
                TempData["ErrorMessage"] = "Please log in to change your password.";
                return RedirectToAction("Login");
            }

            if (ModelState.IsValid)
            {
                var userId = HttpContext.Session.GetInt32("UserId").Value;
                var user = await _context.Users.FindAsync(userId);

                if (user == null)
                {
                    TempData["ErrorMessage"] = "User not found.";
                    return RedirectToAction("Login");
                }

                // Verify current password
                if (!BCrypt.Net.BCrypt.Verify(model.CurrentPassword, user.Password))
                {
                    ModelState.AddModelError("CurrentPassword", "Current password is incorrect.");
                    return View(model);
                }

                // Update password
                user.Password = BCrypt.Net.BCrypt.HashPassword(model.NewPassword);
                _context.Update(user);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Password changed successfully!";
                return RedirectToAction("Profile");
            }

            return View(model);
        }

        // GET: Account/EditProfile
        public async Task<IActionResult> EditProfile()
        {
            if (HttpContext.Session.GetInt32("UserId") == null)
            {
                TempData["ErrorMessage"] = "Please log in to edit your profile.";
                return RedirectToAction("Login");
            }

            var userId = HttpContext.Session.GetInt32("UserId").Value;
            var user = await _context.Users.FindAsync(userId);

            if (user == null)
            {
                TempData["ErrorMessage"] = "User not found.";
                return RedirectToAction("Login");
            }

            var viewModel = new EditProfileViewModel
            {
                Username = user.Username,
                Email = user.Email
            };

            return View(viewModel);
        }

        // POST: Account/EditProfile
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditProfile(EditProfileViewModel model)
        {
            if (HttpContext.Session.GetInt32("UserId") == null)
            {
                TempData["ErrorMessage"] = "Please log in to edit your profile.";
                return RedirectToAction("Login");
            }

            if (ModelState.IsValid)
            {
                var userId = HttpContext.Session.GetInt32("UserId").Value;
                var user = await _context.Users.FindAsync(userId);

                if (user == null)
                {
                    TempData["ErrorMessage"] = "User not found.";
                    return RedirectToAction("Login");
                }

                // Check if email is already taken by another user
                if (await _context.Users.AnyAsync(u => u.Email == model.Email && u.UserId != userId))
                {
                    ModelState.AddModelError("Email", "Email is already registered.");
                    return View(model);
                }

                // Check if username is already taken by another user
                if (await _context.Users.AnyAsync(u => u.Username == model.Username && u.UserId != userId))
                {
                    ModelState.AddModelError("Username", "Username is already taken.");
                    return View(model);
                }

                // Update user profile
                user.Username = model.Username;
                user.Email = model.Email;

                _context.Update(user);
                await _context.SaveChangesAsync();

                // Update session with new username
                HttpContext.Session.SetString("Username", model.Username);

                TempData["SuccessMessage"] = "Profile updated successfully!";
                return RedirectToAction("Profile");
            }

            return View(model);
        }

        // Helper method for CAPTCHA verification
        private async Task<bool> VerifyCaptcha(string captchaResponse)
        {
            if (string.IsNullOrEmpty(captchaResponse))
                return false;

            var secret = "6Len98ErAAAAAB1DGP1LyANiXoAj-EuTEi3wdLFZ"; // replace with your secret key
            using var client = new HttpClient();
            var response = await client.PostAsync(
                $"https://www.google.com/recaptcha/api/siteverify?secret={secret}&response={captchaResponse}", null);
            var json = await response.Content.ReadAsStringAsync();
            dynamic result = Newtonsoft.Json.JsonConvert.DeserializeObject(json);
            return result.success == true;
        }

    }
}