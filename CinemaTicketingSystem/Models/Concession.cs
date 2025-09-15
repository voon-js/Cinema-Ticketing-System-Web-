using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CinemaTicketingSystem.Models
{
    public class Concession
    {
        [Key]
        public int ConcessionId { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }

        [Required]
        public string Category { get; set; } = string.Empty; // Popcorn, Drinks, Candy, Combo

        public string ImageUrl { get; set; } = string.Empty;
        public bool IsAvailable { get; set; } = true;
        public int StockQuantity { get; set; } = 100;
        public bool IsVegetarian { get; set; }
        public bool IsVegan { get; set; }
        public bool ContainsNuts { get; set; }
        public bool ContainsDairy { get; set; }
    }

    public class ConcessionOrder
    {
        [Key]
        public int ConcessionOrderId { get; set; }

        [Required]
        [ForeignKey("Booking")]
        public int BookingId { get; set; }
        public Booking? Booking { get; set; }

        public DateTime OrderDate { get; set; } = DateTime.Now;

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; }

        [StringLength(20)]
        public string Status { get; set; } = "Pending"; // Pending, Prepared, Completed

        public virtual ICollection<ConcessionOrderItem> OrderItems { get; set; } = new List<ConcessionOrderItem>();
    }

    public class ConcessionOrderItem
    {
        [Key]
        public int OrderItemId { get; set; }

        [Required]
        [ForeignKey("ConcessionOrder")]
        public int ConcessionOrderId { get; set; }
        public ConcessionOrder? ConcessionOrder { get; set; }

        [Required]
        [ForeignKey("Concession")]
        public int ConcessionId { get; set; }
        public Concession? Concession { get; set; }

        [Required]
        public int Quantity { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal UnitPrice { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalPrice { get; set; }
    }
}