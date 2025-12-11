using Microsoft.EntityFrameworkCore;
using SupplyManagementSystem.Models;

namespace SupplyManagementSystem.Data
{
    public class AppDbContext : DbContext
    {
        public DbSet<Supplier> Suppliers { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
            => optionsBuilder.UseNpgsql("Host=localhost;Port=5432;Database=SupplyManagementSystemDb;Username=postgres;Password=postgre317");
    }
}
