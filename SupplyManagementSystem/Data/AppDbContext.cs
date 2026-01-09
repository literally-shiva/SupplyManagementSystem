using Microsoft.EntityFrameworkCore;
using SupplyManagementSystem.Models;

namespace SupplyManagementSystem.Data
{
    public class AppDbContext : DbContext
    {
        public DbSet<Supplier> Suppliers { get; set; }
        
        public DbSet<Warehouse> Warehouses { get; set; }
        
        public DbSet<Project> Projects { get; set; }
        
        public DbSet<ProjectTask> ProjectTasks { get; set; }
        
        public DbSet<TransportationCost> TransportationCosts { get; set; }
        
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
            => optionsBuilder.UseNpgsql("Host=localhost;Port=5432;Database=SupplyManagementSystemDb;Username=postgres;Password=postgre317");
    }
}
