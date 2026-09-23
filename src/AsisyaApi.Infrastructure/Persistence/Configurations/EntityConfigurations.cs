using AsisyaApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AsisyaApi.Infrastructure.Persistence.Configurations;

public class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.ToTable("Categories");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Name).HasMaxLength(100).IsRequired();
        builder.Property(c => c.PhotoUrl).HasMaxLength(500).IsRequired();
        builder.HasIndex(c => c.Name).IsUnique();

        builder.HasMany(c => c.Products)
            .WithOne(p => p.Category)
            .HasForeignKey(p => p.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    /// <summary>Nombre de tabla y columnas: el COPY binario de BulkProductWriter depende de ellos.</summary>
    public const string TableName = "Products";

    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable(TableName);
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Name).HasMaxLength(200).IsRequired();
        builder.Property(p => p.Description).HasMaxLength(2000).IsRequired();
        builder.Property(p => p.Price).HasPrecision(18, 2);
        builder.Property(p => p.CreatedAt).HasDefaultValueSql("now()");

        // Índices para los filtros y ordenamientos más comunes de GET /Products.
        builder.HasIndex(p => p.CategoryId);
        builder.HasIndex(p => p.Price);
        builder.HasIndex(p => p.Name);
    }
}

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");
        builder.HasKey(u => u.Id);
        builder.Property(u => u.Username).HasMaxLength(100).IsRequired();
        builder.Property(u => u.PasswordHash).HasMaxLength(500).IsRequired();
        builder.HasIndex(u => u.Username).IsUnique();
    }
}

public class BulkJobConfiguration : IEntityTypeConfiguration<BulkJob>
{
    public void Configure(EntityTypeBuilder<BulkJob> builder)
    {
        builder.ToTable("BulkJobs");
        builder.HasKey(j => j.Id);
        builder.Property(j => j.Id).ValueGeneratedNever();
        builder.Property(j => j.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(j => j.ErrorMessage).HasMaxLength(2000);
    }
}
