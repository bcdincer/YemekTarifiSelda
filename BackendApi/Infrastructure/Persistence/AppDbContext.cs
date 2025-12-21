using BackendApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BackendApi.Infrastructure.Persistence;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Recipe> Recipes => Set<Recipe>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<RecipeRating> RecipeRatings => Set<RecipeRating>();
    public DbSet<RecipeLike> RecipeLikes => Set<RecipeLike>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Recipe - Category ilişkisi
        modelBuilder.Entity<Recipe>()
            .HasOne(r => r.Category)
            .WithMany(c => c.Recipes)
            .HasForeignKey(r => r.CategoryId)
            .OnDelete(DeleteBehavior.SetNull); // Kategori silinirse tarifler null olur

        // Difficulty enum'ını string olarak sakla
        modelBuilder.Entity<Recipe>()
            .Property(r => r.Difficulty)
            .HasConversion<string>();

        // Index'ler (performans için)
        modelBuilder.Entity<Recipe>()
            .HasIndex(r => r.CategoryId);
        
        modelBuilder.Entity<Recipe>()
            .HasIndex(r => r.IsFeatured);
        
        modelBuilder.Entity<Recipe>()
            .HasIndex(r => r.CreatedAt);

        // RecipeRating ilişkileri
        modelBuilder.Entity<RecipeRating>()
            .HasOne(rr => rr.Recipe)
            .WithMany()
            .HasForeignKey(rr => rr.RecipeId)
            .OnDelete(DeleteBehavior.Cascade);

        // RecipeLike ilişkileri
        modelBuilder.Entity<RecipeLike>()
            .HasOne(rl => rl.Recipe)
            .WithMany()
            .HasForeignKey(rl => rl.RecipeId)
            .OnDelete(DeleteBehavior.Cascade);

        // Index'ler (performans için)
        modelBuilder.Entity<RecipeRating>()
            .HasIndex(rr => new { rr.RecipeId, rr.UserId })
            .IsUnique(); // Bir kullanıcı bir tarifi sadece bir kez puanlayabilir

        modelBuilder.Entity<RecipeLike>()
            .HasIndex(rl => new { rl.RecipeId, rl.UserId })
            .IsUnique(); // Bir kullanıcı bir tarifi sadece bir kez beğenebilir

        modelBuilder.Entity<RecipeRating>()
            .HasIndex(rr => rr.RecipeId);

        modelBuilder.Entity<RecipeLike>()
            .HasIndex(rl => rl.RecipeId);
    }
}


