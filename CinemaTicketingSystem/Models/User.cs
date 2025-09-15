using CinemaTicketingSystem.Models;
using System.ComponentModel.DataAnnotations;

public class User
{
    [Key]
    public int UserId { get; set; }

    [Required]
    [StringLength(50)]
    public string Username { get; set; } = string.Empty;

    [Required]
    [StringLength(255)]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [StringLength(100)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [StringLength(20)]
    public string Role { get; set; } = "Customer";

    public bool IsActive { get; set; } = true;

    public int FailedLoginAttempts { get; set; } = 0;

    public DateTime? LastLogin { get; set; }

    public DateTime RegistrationDate { get; set; } = DateTime.Now;

    public string? ResetToken { get; set; }
    public DateTime? ResetTokenExpiry { get; set; }

    public virtual ICollection<Booking>? Bookings { get; set; }
}