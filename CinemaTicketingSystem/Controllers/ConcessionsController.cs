using CinemaTicketingSystem.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CinemaTicketingSystem.Controllers
{
    public class ConcessionsController : Controller
    {
        private readonly CinemaDbContext _context;

        public ConcessionsController(CinemaDbContext context)
        {
            _context = context;
        }

        // GET: Concessions
        public async Task<IActionResult> Index()
        {
            var concessions = await _context.Concessions
                .Where(c => c.IsAvailable)
                .OrderBy(c => c.Category)
                .ThenBy(c => c.Name)
                .ToListAsync();

            return View(concessions);
        }

        // GET: Concessions/AddToOrder/5
        public async Task<IActionResult> AddToOrder(int bookingId)
        {
            var booking = await _context.Bookings
                .Include(b => b.Showtime)
                .ThenInclude(s => s.Movie)
                .FirstOrDefaultAsync(b => b.BookingId == bookingId);

            if (booking == null)
            {
                return NotFound();
            }

            var concessions = await _context.Concessions
                .Where(c => c.IsAvailable)
                .OrderBy(c => c.Category)
                .ThenBy(c => c.Name)
                .ToListAsync();

            ViewBag.Booking = booking;
            return View(concessions);
        }

        // POST: Concessions/CreateOrder
        [HttpPost]
        public async Task<IActionResult> CreateOrder(int bookingId, Dictionary<int, int> concessions)
        {
            if (concessions == null || !concessions.Any())
            {
                TempData["ErrorMessage"] = "Please select at least one concession item.";
                return RedirectToAction("AddToOrder", new { bookingId });
            }

            // Check if order already exists
            var existingOrder = await _context.ConcessionOrders
                .FirstOrDefaultAsync(o => o.BookingId == bookingId);

            if (existingOrder != null)
            {
                // Remove existing order items
                var existingItems = _context.ConcessionOrderItems
                    .Where(i => i.ConcessionOrderId == existingOrder.ConcessionOrderId);
                _context.ConcessionOrderItems.RemoveRange(existingItems);
                _context.ConcessionOrders.Remove(existingOrder);
            }

            var order = new ConcessionOrder
            {
                BookingId = bookingId,
                OrderDate = DateTime.Now,
                Status = "Pending"
            };

            decimal totalAmount = 0;

            foreach (var item in concessions)
            {
                if (item.Value > 0)
                {
                    var concession = await _context.Concessions.FindAsync(item.Key);
                    if (concession != null)
                    {
                        var orderItem = new ConcessionOrderItem
                        {
                            Concession = concession,
                            Quantity = item.Value,
                            UnitPrice = concession.Price,
                            TotalPrice = concession.Price * item.Value
                        };

                        totalAmount += orderItem.TotalPrice;
                        order.OrderItems.Add(orderItem);

                        // Update stock
                        concession.StockQuantity -= item.Value;
                        _context.Update(concession);
                    }
                }
            }

            order.TotalAmount = totalAmount;
            _context.ConcessionOrders.Add(order);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Concession order added successfully!";
            return RedirectToAction("OrderConfirmation", new { orderId = order.ConcessionOrderId });
        }

        // GET: Concessions/OrderConfirmation/5
        public async Task<IActionResult> OrderConfirmation(int orderId)
        {
            var order = await _context.ConcessionOrders
                .Include(o => o.OrderItems)
                .ThenInclude(i => i.Concession)
                .Include(o => o.Booking)
                .ThenInclude(b => b.Showtime)
                .ThenInclude(s => s.Movie)
                .FirstOrDefaultAsync(o => o.ConcessionOrderId == orderId);

            if (order == null)
            {
                return NotFound();
            }

            return View(order);
        }

        // GET: Concessions/MyOrders
        public async Task<IActionResult> MyOrders()
        {
            if (HttpContext.Session.GetInt32("UserId") == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var userId = HttpContext.Session.GetInt32("UserId").Value;

            var orders = await _context.ConcessionOrders
                .Include(o => o.OrderItems)
                .ThenInclude(i => i.Concession)
                .Include(o => o.Booking)
                .ThenInclude(b => b.Showtime)
                .ThenInclude(s => s.Movie)
                .Where(o => o.Booking.UserId == userId)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            return View(orders);
        }
    }
}