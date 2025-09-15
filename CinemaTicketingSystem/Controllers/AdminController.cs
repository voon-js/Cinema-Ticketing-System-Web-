using CinemaTicketingSystem.Models;
using CinemaTicketingSystem.Attributes;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CinemaTicketingSystem.ViewModels;

namespace CinemaTicketingSystem.Controllers
{
    [AdminOnly] // This restricts access to users with Admin role
    public class AdminController : Controller
    {
        private readonly CinemaDbContext _context;

        public AdminController(CinemaDbContext context)
        {
            _context = context;
        }

        // GET: Admin/Dashboard
        public async Task<IActionResult> Dashboard()
        {
            var viewModel = new AdminDashboardViewModel
            {
                TotalBookings = await _context.Bookings.CountAsync(),
                TotalRevenue = await _context.Bookings.SumAsync(b => b.TotalAmount),
                UpcomingShowtimes = await _context.Showtimes
                    .Where(s => s.StartTime > DateTime.Now)
                    .CountAsync(),
                TotalUsers = await _context.Users.CountAsync()
            };

            // Get recent bookings
            var recentBookingsData = await _context.Bookings
                .Include(b => b.Showtime)
                .ThenInclude(s => s.Movie)
                .Include(b => b.User)
                .OrderByDescending(b => b.BookingDate)
                .Take(10)
                .ToListAsync();

            viewModel.RecentBookings = recentBookingsData.Select(b => new RecentBooking
            {
                BookingId = b.BookingId,
                MovieTitle = b.Showtime.Movie.Title,
                NumberOfTickets = b.NumberOfTickets,
                TotalAmount = b.TotalAmount,
                BookingDate = b.BookingDate,
                UserName = b.User.Username
            }).ToList();

            // Calculate time-based stats
            var today = DateTime.Today;
            var weekStart = today.AddDays(-(int)today.DayOfWeek);
            var monthStart = new DateTime(today.Year, today.Month, 1);

            ViewBag.TodayRevenue = await _context.Bookings
                .Where(b => b.BookingDate >= today && b.BookingDate < today.AddDays(1))
                .SumAsync(b => (decimal?)b.TotalAmount) ?? 0;

            ViewBag.TodayBookings = await _context.Bookings
                .CountAsync(b => b.BookingDate >= today && b.BookingDate < today.AddDays(1));

            ViewBag.WeekRevenue = await _context.Bookings
                .Where(b => b.BookingDate >= weekStart && b.BookingDate < weekStart.AddDays(7))
                .SumAsync(b => (decimal?)b.TotalAmount) ?? 0;

            ViewBag.WeekBookings = await _context.Bookings
                .CountAsync(b => b.BookingDate >= weekStart && b.BookingDate < weekStart.AddDays(7));

            ViewBag.MonthRevenue = await _context.Bookings
                .Where(b => b.BookingDate >= monthStart && b.BookingDate < monthStart.AddMonths(1))
                .SumAsync(b => (decimal?)b.TotalAmount) ?? 0;

            ViewBag.MonthBookings = await _context.Bookings
                .CountAsync(b => b.BookingDate >= monthStart && b.BookingDate < monthStart.AddMonths(1));

            // Get top movies
            ViewBag.TopMovies = await _context.Bookings
                .Include(b => b.Showtime)
                .ThenInclude(s => s.Movie)
                .GroupBy(b => b.Showtime.Movie.Title)
                .Select(g => new
                {
                    Title = g.Key,
                    Revenue = g.Sum(b => b.TotalAmount),
                    Tickets = g.Sum(b => b.NumberOfTickets)
                })
                .OrderByDescending(x => x.Revenue)
                .Take(5)
                .ToListAsync();

            return View(viewModel);
        }

        // GET: Admin/BookingReport
        public async Task<IActionResult> BookingReport(DateTime? startDate, DateTime? endDate)
        {
            // Start with all bookings
            var query = _context.Bookings
                .Include(b => b.Showtime)
                .ThenInclude(s => s.Movie)
                .Include(b => b.Showtime)
                .ThenInclude(s => s.Cinema)
                .Include(b => b.User)
                .AsQueryable();

            // Apply date filters if provided
            if (startDate.HasValue)
            {
                query = query.Where(b => b.BookingDate >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                query = query.Where(b => b.BookingDate <= endDate.Value);
            }

            // Order by most recent
            query = query.OrderByDescending(b => b.BookingDate);

            var bookings = await query.ToListAsync();

            // Pass filter values back to view
            ViewBag.StartDate = startDate;
            ViewBag.EndDate = endDate;

            return View(bookings);
        }

        // GET: Admin/RevenueReport
        public async Task<IActionResult> RevenueReport()
        {
            // Get revenue by movie
            var revenueByMovie = await _context.Bookings
                .Include(b => b.Showtime)
                .ThenInclude(s => s.Movie)
                .GroupBy(b => b.Showtime.Movie.Title)
                .Select(g => new
                {
                    Movie = g.Key,
                    TotalRevenue = g.Sum(b => b.TotalAmount),
                    TicketCount = g.Sum(b => b.NumberOfTickets)
                })
                .OrderByDescending(x => x.TotalRevenue)
                .ToListAsync();

            // Get revenue by cinema
            var revenueByCinema = await _context.Bookings
                .Include(b => b.Showtime)
                .ThenInclude(s => s.Cinema)
                .GroupBy(b => b.Showtime.Cinema.Name)
                .Select(g => new
                {
                    Cinema = g.Key,
                    TotalRevenue = g.Sum(b => b.TotalAmount),
                    TicketCount = g.Sum(b => b.NumberOfTickets)
                })
                .OrderByDescending(x => x.TotalRevenue)
                .ToListAsync();

            ViewBag.RevenueByMovie = revenueByMovie;
            ViewBag.RevenueByCinema = revenueByCinema;

            return View();
        }

        // GET: Admin/CreateShowtime
        [AdminOnly]
        public async Task<IActionResult> CreateShowtime()
        {
            var viewModel = new ShowtimeViewModel
            {
                StartTime = DateTime.Today.AddHours(18) // Default to 6 PM today
            };

            ViewBag.Movies = await _context.Movies.ToListAsync();
            ViewBag.Cinemas = await _context.Cinemas.ToListAsync();

            return View(viewModel);
        }

        // POST: Admin/CreateShowtime
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AdminOnly]
        public async Task<IActionResult> CreateShowtime(ShowtimeViewModel model)
        {
            if (ModelState.IsValid)
            {
                // Calculate end time based on movie duration
                var movie = await _context.Movies.FindAsync(model.MovieId);
                if (movie == null)
                {
                    ModelState.AddModelError("", "Selected movie not found.");
                    return View(model);
                }

                var endTime = model.StartTime.AddMinutes(movie.Duration);

                // Check for overlapping showtimes in the same cinema
                var overlappingShowtime = await _context.Showtimes
                    .AnyAsync(s => s.CinemaId == model.CinemaId &&
                                  s.StartTime < endTime &&
                                  s.EndTime > model.StartTime);

                if (overlappingShowtime)
                {
                    ModelState.AddModelError("", "This showtime overlaps with an existing showtime in the same cinema.");
                    ViewBag.Movies = await _context.Movies.ToListAsync();
                    ViewBag.Cinemas = await _context.Cinemas.ToListAsync();
                    return View(model);
                }

                var showtime = new Showtime
                {
                    MovieId = model.MovieId,
                    CinemaId = model.CinemaId,
                    StartTime = model.StartTime,
                    EndTime = endTime,
                    Price = model.Price,
                    TotalSeats = model.TotalSeats,
                    AvailableSeats = model.TotalSeats,
                    SeatMap = new string('0', model.TotalSeats)
                };

                _context.Showtimes.Add(showtime);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Showtime created successfully!";
                return RedirectToAction("ManageShowtimes");
            }

            ViewBag.Movies = await _context.Movies.ToListAsync();
            ViewBag.Cinemas = await _context.Cinemas.ToListAsync();
            return View(model);
        }

        // GET: Admin/ManageShowtimes
        [AdminOnly]
        public async Task<IActionResult> ManageShowtimes(string searchString, DateTime? dateFilter)
        {
            var query = _context.Showtimes
                .Include(s => s.Movie)
                .Include(s => s.Cinema)
                .OrderBy(s => s.StartTime)
                .AsQueryable();

            // Search filter
            if (!string.IsNullOrEmpty(searchString))
            {
                query = query.Where(s => s.Movie.Title.Contains(searchString) ||
                                        s.Cinema.Name.Contains(searchString));
            }

            // Date filter
            if (dateFilter.HasValue)
            {
                query = query.Where(s => s.StartTime.Date == dateFilter.Value.Date);
            }

            var showtimes = await query.ToListAsync();

            ViewBag.SearchString = searchString;
            ViewBag.DateFilter = dateFilter;

            return View(showtimes);
        }

        // GET: Admin/EditShowtime/5
        [AdminOnly]
        public async Task<IActionResult> EditShowtime(int id)
        {
            var showtime = await _context.Showtimes
                .Include(s => s.Movie)
                .Include(s => s.Cinema)
                .FirstOrDefaultAsync(s => s.ShowtimeId == id);

            if (showtime == null)
            {
                return NotFound();
            }

            var viewModel = new ShowtimeViewModel
            {
                MovieId = showtime.MovieId,
                CinemaId = showtime.CinemaId,
                StartTime = showtime.StartTime,
                Price = showtime.Price,
                TotalSeats = showtime.TotalSeats
            };

            ViewBag.Movies = await _context.Movies.ToListAsync();
            ViewBag.Cinemas = await _context.Cinemas.ToListAsync();
            ViewBag.ShowtimeId = id;

            return View(viewModel);
        }

        // POST: Admin/EditShowtime/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AdminOnly]
        public async Task<IActionResult> EditShowtime(int id, ShowtimeViewModel model)
        {
            if (ModelState.IsValid)
            {
                var showtime = await _context.Showtimes.FindAsync(id);
                if (showtime == null)
                {
                    return NotFound();
                }

                var movie = await _context.Movies.FindAsync(model.MovieId);
                if (movie == null)
                {
                    ModelState.AddModelError("", "Selected movie not found.");
                    return View(model);
                }

                var endTime = model.StartTime.AddMinutes(movie.Duration);

                // Check for overlapping showtimes (excluding current showtime)
                var overlappingShowtime = await _context.Showtimes
                    .AnyAsync(s => s.CinemaId == model.CinemaId &&
                                  s.ShowtimeId != id &&
                                  s.StartTime < endTime &&
                                  s.EndTime > model.StartTime);

                if (overlappingShowtime)
                {
                    ModelState.AddModelError("", "This showtime overlaps with an existing showtime in the same cinema.");
                    ViewBag.Movies = await _context.Movies.ToListAsync();
                    ViewBag.Cinemas = await _context.Cinemas.ToListAsync();
                    ViewBag.ShowtimeId = id;
                    return View(model);
                }

                showtime.MovieId = model.MovieId;
                showtime.CinemaId = model.CinemaId;
                showtime.StartTime = model.StartTime;
                showtime.EndTime = endTime;
                showtime.Price = model.Price;

                _context.Update(showtime);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Showtime updated successfully!";
                return RedirectToAction("ManageShowtimes");
            }

            ViewBag.Movies = await _context.Movies.ToListAsync();
            ViewBag.Cinemas = await _context.Cinemas.ToListAsync();
            ViewBag.ShowtimeId = id;
            return View(model);
        }

        // POST: Admin/DeleteShowtime/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AdminOnly]
        public async Task<IActionResult> DeleteShowtime(int id)
        {
            var showtime = await _context.Showtimes
                .Include(s => s.Movie)
                .FirstOrDefaultAsync(s => s.ShowtimeId == id);

            if (showtime == null)
            {
                return NotFound();
            }

            // Check if there are any bookings for this showtime
            var hasBookings = await _context.Bookings.AnyAsync(b => b.ShowtimeId == id);

            if (hasBookings)
            {
                TempData["ErrorMessage"] = $"Cannot delete showtime for {showtime.Movie.Title} because there are existing bookings.";
                return RedirectToAction("ManageShowtimes");
            }

            _context.Showtimes.Remove(showtime);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Showtime deleted successfully!";
            return RedirectToAction("ManageShowtimes");
        }

    }
}