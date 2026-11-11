// ----------------------------------------------------------------------------
// DbContext
// ----------------------------------------------------------------------------
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using FoodieAPI.Models;
namespace FoodieAPI.Data
{
   public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
    {
        public DbSet<User> Users => Set<User>();
        public DbSet<Category> Categories => Set<Category>();
        public DbSet<Product> Products => Set<Product>();
        public DbSet<Order> Orders => Set<Order>();
        public DbSet<OrderItem> OrderItems => Set<OrderItem>();

        protected override void ConfigureConventions(ModelConfigurationBuilder builder)
        {
            // SQL Server (datetime2) trả DateTime với Kind = Unspecified. Ép về UTC để JSON có hậu tố "Z"
            // và trình duyệt hiển thị đúng múi giờ người dùng.
            builder.Properties<DateTime>().HaveConversion<UtcDateTimeConverter>();
        }

        protected override void OnModelCreating(ModelBuilder model)
        {
            model.Entity<User>().HasIndex(u => u.Email).IsUnique();

            model.Entity<Category>().HasIndex(c => c.Name).IsUnique();

            model.Entity<Product>(e =>
            {
                e.HasIndex(p => p.CreatedAt);
                // Không cho xóa danh mục khi vẫn còn món thuộc danh mục đó
                e.HasOne(p => p.Category).WithMany(c => c.Products)
                    .HasForeignKey(p => p.CategoryId).OnDelete(DeleteBehavior.Restrict);
            });

            model.Entity<Order>(e =>
            {
                e.HasIndex(o => o.Code).IsUnique();
                e.HasIndex(o => o.Status);
                e.HasIndex(o => o.CreatedAt);
                e.HasMany(o => o.Items).WithOne(i => i.Order)
                    .HasForeignKey(i => i.OrderId).OnDelete(DeleteBehavior.Cascade);
                // Lịch sử đơn hàng phải được giữ lại: không cho xóa user khi user đã có đơn
                e.HasOne(o => o.User).WithMany(u => u.Orders)
                    .HasForeignKey(o => o.UserId).OnDelete(DeleteBehavior.Restrict);
            });

            // Xóa món khỏi thực đơn thì đơn cũ vẫn còn, ProductId chuyển thành NULL
            model.Entity<OrderItem>().HasOne(i => i.Product).WithMany()
                .HasForeignKey(i => i.ProductId).OnDelete(DeleteBehavior.SetNull);
        }
    }
    sealed class UtcDateTimeConverter() : ValueConverter<DateTime, DateTime>(
        v => v.Kind == DateTimeKind.Local ? v.ToUniversalTime() : v,
        v => DateTime.SpecifyKind(v, DateTimeKind.Utc));

}