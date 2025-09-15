using System.ComponentModel.DataAnnotations;

namespace CinemaTicketingSystem.ViewModels
{
    public class ShowtimeViewModel
    {
        [Required(ErrorMessage = "Please select a movie")]
        [Display(Name = "Movie")]
        public int MovieId { get; set; }

        [Required(ErrorMessage = "Please select a cinema")]
        [Display(Name = "Cinema")]
        public int CinemaId { get; set; }

        [Required(ErrorMessage = "Please enter start time")]
        [Display(Name = "Start Time")]
        public DateTime StartTime { get; set; } = DateTime.Now.AddHours(1);

        [Required(ErrorMessage = "Please enter price")]
        [Range(5, 50, ErrorMessage = "Price must be between $5 and $50")]
        [Display(Name = "Price per Ticket")]
        public decimal Price { get; set; } = 12.50m;

        [Required(ErrorMessage = "Please enter total seats")]
        [Range(50, 300, ErrorMessage = "Seats must be between 50 and 300")]
        [Display(Name = "Total Seats")]
        public int TotalSeats { get; set; } = 100;
    }
}