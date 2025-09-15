using System.ComponentModel.DataAnnotations;

namespace CinemaTicketingSystem.Models
{
    public class Cinema
    {
        [Key]
        public int CinemaId { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        public string Address { get; set; } = string.Empty;

        public string? ContactNumber { get; set; }
    }
}