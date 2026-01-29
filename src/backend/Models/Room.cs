using System.Collections.Generic; // Potrzebne dla ICollection
using System.ComponentModel.DataAnnotations; // Zachowujemy Key
using System.ComponentModel.DataAnnotations.Schema; // Potrzebne dla [NotMapped] jeśli potrzebne

namespace SmartHotel.Backend.Models
{
    public class Room
    {
        [Key]
        public int Id { get; set; }
        public string RoomNumber { get; set; } = string.Empty;
        /// <summary>
        /// Typ pokoju (np. Standard, Deluxe, Suite).
        /// </summary>
        public string RoomType { get; set; } = string.Empty; // Zmieniono nazwę z 'Type'
        public int Capacity { get; set; }

        /// <summary>
        /// Bazowa cena za noc, niezależna od sezonu.
        /// </summary>
        public decimal BasePrice { get; set; } // Zastępuje PricePerNight

        /// <summary>
        /// Kolekcja sezonowych reguł cenowych dla tego pokoju.
        /// </summary>
        public ICollection<SeasonalPrice> SeasonalPrices { get; set; } = new List<SeasonalPrice>(); // Dodano kolekcję
    }
}