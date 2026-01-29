using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SmartHotel.Backend.Models
{
    public class Reservation
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();
        
        public int RoomId { get; set; } // Klucz obcy
        
        [ForeignKey("RoomId")]
        public Room? Room { get; set; } // Nawigacja (opcjonalna)

        public string GuestName { get; set; } = string.Empty;
        public string GuestEmail { get; set; } = string.Empty;
        public DateTime CheckInDate { get; set; }
        public DateTime CheckOutDate { get; set; }
        public decimal TotalPrice { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}