using Microsoft.EntityFrameworkCore;
using SmartHotel.Backend.Models;

namespace SmartHotel.Backend.Data
{
    public class HotelDbContext : DbContext
    {
        public HotelDbContext(DbContextOptions<HotelDbContext> options) : base(options) { }

        public DbSet<Reservation> Reservations { get; set; }
        public DbSet<Room> Rooms { get; set; } 

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Reservation>().Property(r => r.TotalPrice).HasColumnType("decimal(18,2)");
            modelBuilder.Entity<Room>().Property(r => r.PricePerNight).HasColumnType("decimal(18,2)");
        }
    }
}