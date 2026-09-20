using Microsoft.EntityFrameworkCore;
using App.Models;

namespace App.Database;


public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<SellerProfile> SellerProfiles => Set<SellerProfile>();
    public DbSet<Cart> Carts => Set<Cart>();
    public DbSet<CartItem> cartItems => Set<CartItem>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<Payment> Payments => Set<Payment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // User
        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("users");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.UserName)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(x => x.Email)
                .IsRequired()
                .HasMaxLength(255);

            entity.Property(x => x.Password)
                .IsRequired();

            entity.Property(x => x.Roles)
                .IsRequired();

            entity.Property(x => x.isActive)
                .IsRequired();

            entity.HasIndex(x => x.UserName)
                .IsUnique();

            entity.HasIndex(x => x.Email)
                .IsUnique();
        });

        // Category
        modelBuilder.Entity<Category>(entity =>
        {
            entity.ToTable("categories");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.Title)
                .IsRequired()
                .HasMaxLength(150);

            entity.Property(x => x.Slug)
                .IsRequired()
                .HasMaxLength(150);

            entity.HasIndex(x => x.Title)
                .IsUnique();

            entity.HasIndex(x => x.Slug)
                .IsUnique();
        });

        // Product
        modelBuilder.Entity<Product>(entity =>
        {
            entity.ToTable("products");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.Name)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(x => x.Description)
                .HasMaxLength(2000);

            entity.Property(x => x.Price)
                .HasPrecision(18, 2)
                .IsRequired();

            entity.Property(x => x.SKU)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(x => x.Stock)
                .IsRequired();

            // Product -> Seller
            entity.HasOne(x => x.SellerProfile)
                .WithMany(x => x.Products)
                .HasForeignKey(x => x.SellerProfileId)
                .OnDelete(DeleteBehavior.Restrict);

            // Product -> Category
            entity.HasOne(x => x.Category)
                .WithMany(x => x.Products)
                .HasForeignKey(x => x.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(x => x.SKU)
                .IsUnique();

            entity.HasIndex(x => x.SellerProfileId);
            entity.HasIndex(x => x.CategoryId);
        });

        // SellerProfile
        modelBuilder.Entity<SellerProfile>(entity =>
        {
            entity.ToTable("seller_profiles");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.StoreName)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(x => x.Description)
                .HasMaxLength(2000);

            entity.Property(x => x.IsApproved)
                .IsRequired();

            // SellerProfile -> User
            entity.HasOne(x => x.User)
                .WithOne()
                .HasForeignKey<SellerProfile>(x => x.userId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(x => x.userId)
                .IsUnique();

            entity.HasIndex(x => x.StoreName)
                .IsUnique();
        });

        modelBuilder.Entity<Cart>(entity =>
        {
            entity.HasIndex(c => c.UserId);

            entity.HasMany(c => c.Items)
                  .WithOne(i => i.Cart)
                  .HasForeignKey(i => i.CartId)
                  .OnDelete(DeleteBehavior.Cascade); 
        });

        modelBuilder.Entity<CartItem>(entity =>
        {
            entity.HasIndex(i => i.ProductId);
        });

        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasIndex(o => o.userId);

            entity.Property(o => o.TotalAmount)
                .HasPrecision(18, 2);

            entity.HasMany(o => o.orderItems)
                .WithOne(i => i.Order)
                .HasForeignKey(i => i.orderId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<OrderItem>(entity =>
        {
            entity.HasIndex(i => i.productId);

            entity.Property(i => i.Price)
                .HasPrecision(18, 2);
        });

        modelBuilder.Entity<Payment>(entity =>
        {
            entity.HasIndex(p => p.OrderId);

            entity.Property(p => p.Amount)
                .HasPrecision(18, 2);
        });
    }
}