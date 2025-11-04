using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SalesManager.BusinessObjects.Entities;

namespace SalesManager.Repositories.Persistence
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Product> Products { get; set; }
        public DbSet<Customer> Customers { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderDetail> OrderDetails { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Supplier> Suppliers { get; set; }
        public DbSet<ErrorLog> ErrorLogs { get; set; }
        public DbSet<Employee> Employees { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<OrderDetail>(entity =>
            {
                entity.HasKey(od => new { od.OrderID, od.ProductID });

                entity.Property(od => od.UnitPrice)
                    .HasColumnType("decimal(18,2)");

                entity.ToTable("Order Details");
            });

            modelBuilder.Entity<Order>(entity =>
            {
                entity.Property(o => o.Freight)
                    .HasColumnType("decimal(18,2)");

                entity.Property(o => o.Subtotal)
                    .HasColumnType("decimal(18,2)");

                entity.Property(o => o.VatAmount)
                    .HasColumnType("decimal(18,2)");

                entity.Property(o => o.TotalAmount)
                    .HasColumnType("decimal(18,2)");
            });
            modelBuilder.Entity<Product>(entity =>
            {
                entity.Property(p => p.UnitPrice)
                    .HasColumnType("decimal(18,2)");
            });
            modelBuilder.Entity<ApplicationUser>(entity =>
            {
                entity.HasOne(a => a.Employee)
                      .WithOne(e => e.ApplicationUser)
                      .HasForeignKey<ApplicationUser>(a => a.EmployeeID);
            });

            // Configuración de la tabla Employee (si Northwind la tiene en plural)
            modelBuilder.Entity<Employee>(entity =>
            {
                entity.ToTable("Employees");
                entity.Property(e => e.EmployeeID).ValueGeneratedOnAdd();
            });
        }
    }
}