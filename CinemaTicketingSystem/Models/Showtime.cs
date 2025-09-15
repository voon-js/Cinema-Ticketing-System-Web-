using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CinemaTicketingSystem.Models
{
    public class Showtime
    {
        [Key]
        public int ShowtimeId { get; set; }

        [Required]
        [ForeignKey("Movie")]
        public int MovieId { get; set; }
        public Movie? Movie { get; set; }

        [Required]
        [ForeignKey("Cinema")]
        public int CinemaId { get; set; }
        public Cinema? Cinema { get; set; }

        [Required]
        public DateTime StartTime { get; set; }

        [Required]
        public DateTime EndTime { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }

        [Required]
        public int TotalSeats { get; set; } = 100; // Default cinema size

        [Required]
        public int AvailableSeats { get; set; }

        public string SeatMap { get; set; } = "0000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000"; // 100 seats (0=available, 1=taken)
    }
}