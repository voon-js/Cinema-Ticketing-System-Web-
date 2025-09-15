using System;
using System.Collections.Generic;

namespace CinemaTicketingSystem.ViewModels
{
    public class AdminDashboardViewModel
    {
        public int TotalBookings { get; set; }
        public decimal TotalRevenue { get; set; }
        public int UpcomingShowtimes { get; set; }
        public int TotalUsers { get; set; }
        public List<RecentBooking> RecentBookings { get; set; } = new List<RecentBooking>();
    }

    public class RecentBooking
    {
        public int BookingId { get; set; }
        public string MovieTitle { get; set; } = string.Empty;
        public int NumberOfTickets { get; set; }
        public decimal TotalAmount { get; set; }
        public DateTime BookingDate { get; set; }
        public string UserName { get; set; } = string.Empty;
    }
}