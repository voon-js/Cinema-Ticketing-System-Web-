using CinemaTicketingSystem.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CinemaTicketingSystem.Attributes;
using CinemaTicketingSystem.ViewModels;
using CinemaTicketingSystem.Services;

namespace CinemaTicketingSystem.Controllers
{
    public class BookingController : Controller
    {
        private readonly CinemaDbContext _context;
        private readonly IEmailService _emailService;

        public BookingController(CinemaDbContext context, IEmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        // GET: Booking/SelectMovie
        public async Task<IActionResult> SelectMovie()
        {
            var movies = await _context.Movies.ToListAsync();
            return View(movies);
        }

        // GET: Booking/SelectShowtime/5
        public async Task<IActionResult> SelectShowtime(int id)
        {
            var movie = await _context.Movies.FindAsync(id);
            if (movie == null) return NotFound();

            var showtimes = await _context.Showtimes
                .Where(s => s.MovieId == id && s.StartTime > DateTime.Now)
                .Include(s => s.Movie)
                .Include(s => s.Cinema)
                .ToListAsync();

            ViewBag.MovieTitle = movie.Title;
            return View(showtimes);
        }

        // GET: Booking/BookTickets/5
        // Change the BookTickets action to redirect to seat selection
        public async Task<IActionResult> BookTickets(int id)
        {
            return RedirectToAction("SelectSeats", new { id = id });
        }

        // POST: Booking/BookTickets/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BookTickets(int id, int numberOfTickets)
        {
            if (HttpContext.Session.GetInt32("UserId") == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var userId = HttpContext.Session.GetInt32("UserId").Value;
            var showtime = await _context.Showtimes.FindAsync(id);

            if (showtime == null) return NotFound();

            if (numberOfTickets > showtime.AvailableSeats)
            {
                ModelState.AddModelError("", $"Only {showtime.AvailableSeats} seats available.");
                return View(await _context.Showtimes
                    .Include(s => s.Movie)
                    .Include(s => s.Cinema)
                    .FirstOrDefaultAsync(s => s.ShowtimeId == id));
            }

            // Create the booking
            var booking = new Booking
            {
                UserId = userId,
                ShowtimeId = id,
                NumberOfTickets = numberOfTickets,
                TotalAmount = numberOfTickets * showtime.Price,
                Status = "Confirmed"
            };

            // Update available seats
            showtime.AvailableSeats -= numberOfTickets;

            _context.Bookings.Add(booking);
            _context.Showtimes.Update(showtime);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Successfully booked {numberOfTickets} ticket(s) for {showtime.Movie?.Title}!";
            return RedirectToAction("MyBookings");
        }

        // GET: Booking/MyBookings
        public async Task<IActionResult> MyBookings()
        {
            if (HttpContext.Session.GetInt32("UserId") == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var userId = HttpContext.Session.GetInt32("UserId").Value;

            var bookings = await _context.Bookings
                .Where(b => b.UserId == userId)
                .Include(b => b.Showtime)
                .ThenInclude(s => s.Movie)
                .Include(b => b.Showtime)
                .ThenInclude(s => s.Cinema)
                .OrderByDescending(b => b.BookingDate)
                .ToListAsync();

            return View(bookings);
        }

        // GET: Booking/SelectSeats/5
        public async Task<IActionResult> SelectSeats(int id)
        {
            var showtime = await _context.Showtimes
                .Include(s => s.Movie)
                .Include(s => s.Cinema)
                .FirstOrDefaultAsync(s => s.ShowtimeId == id);

            if (showtime == null) return NotFound();

            var viewModel = new SeatSelectionViewModel
            {
                ShowtimeId = id,
                MovieTitle = showtime.Movie?.Title ?? "",
                CinemaName = showtime.Cinema?.Name ?? "",
                Showtime = showtime.StartTime,
                Price = showtime.Price,
                SeatMap = showtime.SeatMap ?? new string('0', 100),
                TotalSeats = showtime.TotalSeats,
                SeatsPerRow = 10
            };

            return View(viewModel);
        }

        // POST: Booking/SelectSeats
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SelectSeats(SeatSelectionViewModel model)
        {
            if (HttpContext.Session.GetInt32("UserId") == null)
            {
                return RedirectToAction("Login", "Account");
            }

            if (model.SelectedSeats == null || model.SelectedSeats.Count == 0)
            {
                ModelState.AddModelError("", "Please select at least one seat.");
            }

            var showtime = await _context.Showtimes.FindAsync(model.ShowtimeId);
            if (showtime == null) return NotFound();

            // Update seat map
            var seatMapArray = showtime.SeatMap.ToCharArray();
            foreach (var seatNumber in model.SelectedSeats)
            {
                if (seatNumber < seatMapArray.Length)
                {
                    seatMapArray[seatNumber] = '1'; // Mark seat as taken
                }
            }
            showtime.SeatMap = new string(seatMapArray);
            showtime.AvailableSeats -= model.SelectedSeats.Count;

            // Create booking
            var userId = HttpContext.Session.GetInt32("UserId").Value;
            var booking = new Booking
            {
                UserId = userId,
                ShowtimeId = model.ShowtimeId,
                NumberOfTickets = model.SelectedSeats.Count,
                TotalAmount = model.SelectedSeats.Count * showtime.Price,
                Status = "Confirmed",
                SeatNumbers = string.Join(",", model.SelectedSeats) // Store selected seats
            };

            _context.Bookings.Add(booking);
            _context.Showtimes.Update(showtime);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Successfully booked {model.SelectedSeats.Count} seat(s)! Seats: {string.Join(", ", model.SelectedSeats.Select(s => (s + 1).ToString()))}";
            //return RedirectToAction("BookingConfirmation", new { id = booking.BookingId });
            return RedirectToAction("Process", "Payment", new { id = booking.BookingId });
        }

        // GET: Booking/BookingConfirmation/5
        public async Task<IActionResult> BookingConfirmation(int id)
        {
            var booking = await _context.Bookings
                .Include(b => b.Showtime)
                .ThenInclude(s => s.Movie)
                .Include(b => b.Showtime)
                .ThenInclude(s => s.Cinema)
                .FirstOrDefaultAsync(b => b.BookingId == id);

            if (booking == null) return NotFound();

            return View(booking);
        }

        // GET: Booking/GetSeatAvailability/5
        [HttpGet]
        public async Task<JsonResult> GetSeatAvailability(int id)
        {
            var showtime = await _context.Showtimes.FindAsync(id);
            if (showtime == null) return Json(new { error = "Showtime not found" });

            return Json(new
            {
                availableSeats = showtime.AvailableSeats,
                seatMap = showtime.SeatMap
            });
        }

        // POST: Booking/CancelBooking/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelBooking(int id)
        {
            if (HttpContext.Session.GetInt32("UserId") == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var userId = HttpContext.Session.GetInt32("UserId").Value;
            var booking = await _context.Bookings
                .Include(b => b.Showtime)
                .FirstOrDefaultAsync(b => b.BookingId == id && b.UserId == userId);

            if (booking == null) return NotFound();

            if (booking.Status == "Confirmed" && booking.Showtime.StartTime > DateTime.Now.AddHours(1))
            {
                // Free up the seats
                var seatMapArray = booking.Showtime.SeatMap.ToCharArray();
                foreach (var seatStr in booking.SeatNumbers.Split(','))
                {
                    if (int.TryParse(seatStr, out int seatNumber) && seatNumber < seatMapArray.Length)
                    {
                        seatMapArray[seatNumber] = '0';
                    }
                }
                booking.Showtime.SeatMap = new string(seatMapArray);
                booking.Showtime.AvailableSeats += booking.NumberOfTickets;

                booking.Status = "Cancelled";

                _context.Update(booking.Showtime);
                _context.Update(booking);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Booking cancelled successfully.";
            }
            else
            {
                TempData["ErrorMessage"] = "Cannot cancel booking. Too close to showtime or already cancelled.";
            }

            return RedirectToAction("MyBookings");
        }

        // GET: Booking/ExportBookings
        public async Task<IActionResult> ExportBookings(string format = "csv")
        {
            if (HttpContext.Session.GetInt32("UserId") == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var userId = HttpContext.Session.GetInt32("UserId").Value;

            var bookings = await _context.Bookings
                .Where(b => b.UserId == userId)
                .Include(b => b.Showtime)
                .ThenInclude(s => s.Movie)
                .Include(b => b.Showtime)
                .ThenInclude(s => s.Cinema)
                .OrderByDescending(b => b.BookingDate)
                .ToListAsync();

            return format.ToLower() switch
            {
                "json" => ExportAsJson(bookings),
                //"xml" => ExportAsXml(bookings),
                _ => ExportAsCsv(bookings)
            };
        }

        private IActionResult ExportAsCsv(List<Booking> bookings)
        {
            var csv = new System.Text.StringBuilder();
            csv.AppendLine("BookingID,Movie,Cinema,Showtime,Seats,Tickets,TotalAmount,Status,BookingDate");

            foreach (var booking in bookings)
            {
                csv.AppendLine($"#{booking.BookingId},\"{booking.Showtime?.Movie?.Title}\",\"{booking.Showtime?.Cinema?.Name}\",\"{booking.Showtime?.StartTime:yyyy-MM-dd HH:mm}\",\"{booking.SeatNumbers}\",{booking.NumberOfTickets},{booking.TotalAmount},{booking.Status},{booking.BookingDate:yyyy-MM-dd}");
            }

            return File(System.Text.Encoding.UTF8.GetBytes(csv.ToString()), "text/csv", "my-bookings.csv");
        }

        private IActionResult ExportAsJson(List<Booking> bookings)
        {
            var jsonData = bookings.Select(b => new
            {
                b.BookingId,
                Movie = b.Showtime?.Movie?.Title,
                Cinema = b.Showtime?.Cinema?.Name,
                Showtime = b.Showtime?.StartTime,
                b.SeatNumbers,
                b.NumberOfTickets,
                b.TotalAmount,
                b.Status,
                b.BookingDate
            });

            var json = System.Text.Json.JsonSerializer.Serialize(jsonData, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
            return File(System.Text.Encoding.UTF8.GetBytes(json), "application/json", "my-bookings.json");
        }

    }
}