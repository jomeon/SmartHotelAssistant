namespace SmartHotel.Backend.Models
{
    /// <summary>
    /// Reprezentuje regułę cenową dla określonego okresu sezonowego.
    /// </summary>
    public class SeasonalPrice
    {
        /// <summary>
        /// Unikalny identyfikator wpisu cenowego.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Klucz obcy do tabeli Rooms.
        /// </summary>
        public int RoomId { get; set; }

        /// <summary>
        /// Obiekt pokoju powiązany z tą regułą cenową.
        /// </summary>
        public Room Room { get; set; } = null!;

        /// <summary>
        /// Data rozpoczęcia obowiązywania sezonowej ceny.
        /// </summary>
        public DateOnly StartDate { get; set; }

        /// <summary>
        /// Data zakończenia obowiązywania sezonowej ceny.
        /// </summary>
        public DateOnly EndDate { get; set; }

        /// <summary>
        /// Mnożnik ceny bazowej (np. 1.2 oznacza 20% podwyżki).
        /// </summary>
        public decimal PriceMultiplier { get; set; } = 1.0m; // Domyślnie brak zmiany

        /// <summary>
        /// Opcjonalna stała cena za noc w danym sezonie (nadpisuje mnożnik).
        /// </summary>
        public decimal? FixedPrice { get; set; } // Null oznacza, że nie jest używany

        /// <summary>
        /// Nazwa sezonu dla celów informacyjnych (np. "Lato", "Święta").
        /// </summary>
        public string SeasonName { get; set; } = string.Empty;
    }
}