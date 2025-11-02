using Microsoft.EntityFrameworkCore;
using Rently.Api.Models;

namespace Rently.Api.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        public DbSet<InventoryItem> InventoryItems { get; set; }
        public DbSet<Tenant> Tenants { get; set; }
        public DbSet<User> Users { get; set; }

        public DbSet<Rental> Rentals { get; set; }
        public DbSet<Payment> Payments { get; set; }

        public DbSet<Booking> Bookings { get; set; }
        public DbSet<AnalyticsLog> AnalyticsLogs { get; set; }

        public DbSet<Customer> Customers { get; set; }

        // ✅ Fixed: Corrected property name to Alerts (plural, matching model & migration)
        public DbSet<Alert> Alerts { get; set; }
    }
}
