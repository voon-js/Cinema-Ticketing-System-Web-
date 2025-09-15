using Microsoft.EntityFrameworkCore;

namespace CinemaTicketingSystem.Models
{
    public class CinemaDbContext : DbContext
    {
        // Constructor that accepts options, passed from dependency injection
        public CinemaDbContext(DbContextOptions<CinemaDbContext> options) : base(options)
        {
        }

        // Define your DbSets (Database Tables) here
        public DbSet<Movie> Movies { get; set; } = null!;
        public DbSet<User> Users { get; set; } = null!;

        public DbSet<Cinema> Cinemas { get; set; } = null!;
        public DbSet<Showtime> Showtimes { get; set; } = null!;
        public DbSet<Booking> Bookings { get; set; } = null!;

        // You will add DbSet for Cinema, Screen, etc. here later
        // public DbSet<Cinema> Cinemas { get; set; } = null!;

        public DbSet<Payment> Payments { get; set; } = null!;

        public DbSet<Concession> Concessions { get; set; } = null!;
        public DbSet<ConcessionOrder> ConcessionOrders { get; set; } = null!;
        public DbSet<ConcessionOrderItem> ConcessionOrderItems { get; set; } = null!;
    }
}