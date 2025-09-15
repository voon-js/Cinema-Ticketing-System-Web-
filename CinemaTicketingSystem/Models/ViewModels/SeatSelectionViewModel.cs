using System.ComponentModel.DataAnnotations;

namespace CinemaTicketingSystem.ViewModels
{
    public class SeatSelectionViewModel
    {
        public int ShowtimeId { get; set; }
        public string MovieTitle { get; set; } = string.Empty;
        public string CinemaName { get; set; } = string.Empty;
        public DateTime Showtime { get; set; }
        public decimal Price { get; set; }

        [Required(ErrorMessage = "Please select at least one seat")]
        public List<int> SelectedSeats { get; set; } = new List<int>();

        public string SeatMap { get; set; } = string.Empty;
        public int TotalSeats { get; set; } = 100;
        public int SeatsPerRow { get; set; } = 10;
    }
}