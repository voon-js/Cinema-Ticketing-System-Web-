using CinemaTicketingSystem.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CinemaTicketingSystem.Services;

namespace CinemaTicketingSystem.Controllers
{
    public class PaymentController : Controller
    {
        private readonly CinemaDbContext _context;
        private readonly IEmailService _emailService;

        public PaymentController(CinemaDbContext context, IEmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        // GET: Payment/Process/5 (bookingId)
        public async Task<IActionResult> Process(int id)
        {
            if (HttpContext.Session.GetInt32("UserId") == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var booking = await _context.Bookings
                .Include(b => b.Showtime)
                .ThenInclude(s => s.Movie)
                .Include(b => b.Showtime)
                .ThenInclude(s => s.Cinema)
                .FirstOrDefaultAsync(b => b.BookingId == id);

            if (booking == null)
            {
                return NotFound();
            }

            // Check if payment already exists
            var existingPayment = await _context.Payments
                .FirstOrDefaultAsync(p => p.BookingId == id && p.Status == "Completed");

            if (existingPayment != null)
            {
                TempData["ErrorMessage"] = "Payment already processed for this booking.";
                return RedirectToAction("MyBookings", "Booking");
            }

            // Create a view model to pass both booking and payment methods
            var viewModel = new Tuple<Booking, List<string>>(booking, new List<string>
    {
        "Credit Card",
        "Debit Card",
        "PayPal",
        "Online Banking"
    });

            return View(viewModel);
        }

        // POST: Payment/Process/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Process(int id, string paymentMethod)
        {
            if (HttpContext.Session.GetInt32("UserId") == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var booking = await _context.Bookings
                .Include(b => b.Showtime)
                .ThenInclude(s => s.Movie)
                .FirstOrDefaultAsync(b => b.BookingId == id);

            if (booking == null)
            {
                return NotFound();
            }

            // Simulate payment processing
            var payment = new Payment
            {
                BookingId = id,
                Amount = booking.TotalAmount,
                PaymentMethod = paymentMethod,
                TransactionId = Guid.NewGuid().ToString(),
                Status = "Completed",
                PaymentDate = DateTime.Now,
                Notes = "Payment processed successfully"
            };

            // Update booking status
            booking.Status = "Confirmed";

            _context.Payments.Add(payment);
            _context.Bookings.Update(booking);
            await _context.SaveChangesAsync();

            // SEND EMAIL AFTER PAYMENT CONFIRMATION
            try
            {
                var userId = HttpContext.Session.GetInt32("UserId").Value;
                var user = await _context.Users.FindAsync(userId);

                if (user != null && !string.IsNullOrEmpty(user.Email))
                {
                    // Load complete booking details for email
                    var bookingWithDetails = await _context.Bookings
                        .Include(b => b.Showtime)
                        .ThenInclude(s => s.Movie)
                        .Include(b => b.Showtime)
                        .ThenInclude(s => s.Cinema)
                        .FirstOrDefaultAsync(b => b.BookingId == id);

                    if (bookingWithDetails != null)
                    {
                        await _emailService.SendPaymentConfirmationAsync(
                            user.Email,
                            user.Username,
                            bookingWithDetails,
                            payment // Pass the payment information
                        );
                    }
                }
            }
            catch (Exception ex)
            {
                // Log error but don't crash the application or prevent payment confirmation
                Console.WriteLine($"Email sending failed: {ex.Message}");
                // You could add this to a proper logging system
            }

            TempData["SuccessMessage"] = "Payment processed successfully!";
            return RedirectToAction("Confirmation", new { id = payment.PaymentId });
        }

        // GET: Payment/Confirmation/5 (paymentId)
        public async Task<IActionResult> Confirmation(int id)
        {
            var payment = await _context.Payments
                .Include(p => p.Booking)
                .ThenInclude(b => b.Showtime)
                .ThenInclude(s => s.Movie)
                .Include(p => p.Booking)
                .ThenInclude(b => b.Showtime)
                .ThenInclude(s => s.Cinema)
                .FirstOrDefaultAsync(p => p.PaymentId == id);

            if (payment == null)
            {
                return NotFound();
            }

            return View(payment);
        }

        // POST: Payment/Refund/5 (bookingId)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Refund(int id)
        {
            if (HttpContext.Session.GetString("UserRole") != "Admin")
            {
                TempData["ErrorMessage"] = "Admin access required for refunds.";
                return RedirectToAction("Index", "Home");
            }

            var payment = await _context.Payments
                .FirstOrDefaultAsync(p => p.BookingId == id && p.Status == "Completed");

            if (payment == null)
            {
                TempData["ErrorMessage"] = "No completed payment found for this booking.";
                return RedirectToAction("ManageBookings", "Admin");
            }

            // Simulate refund
            payment.Status = "Refunded";
            payment.Notes = $"Refund processed on {DateTime.Now:yyyy-MM-dd HH:mm}";

            var booking = await _context.Bookings.FindAsync(id);
            if (booking != null)
            {
                booking.Status = "Refunded";
            }

            _context.Update(payment);
            if (booking != null)
            {
                _context.Update(booking);
            }
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Refund processed successfully!";
            return RedirectToAction("ManageBookings", "Admin");
        }
    }
}