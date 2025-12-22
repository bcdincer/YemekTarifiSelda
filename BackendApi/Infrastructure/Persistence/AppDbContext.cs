using BackendApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BackendApi.Infrastructure.Persistence;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Recipe> Recipes => Set<Recipe>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<RecipeRating> RecipeRatings => Set<RecipeRating>();
    public DbSet<RecipeLike> RecipeLikes => Set<RecipeLike>();
    public DbSet<Collection> Collections => Set<Collection>();
    public DbSet<CollectionRecipe> CollectionRecipes => Set<CollectionRecipe>();
    public DbSet<RecipeComment> RecipeComments => Set<RecipeComment>();
    public DbSet<CommentLike> CommentLikes => Set<CommentLike>();
    public DbSet<MealPlan> MealPlans => Set<MealPlan>();
    public DbSet<MealPlanItem> MealPlanItems => Set<MealPlanItem>();
    public DbSet<ShoppingList> ShoppingLists => Set<ShoppingList>();
    public DbSet<ShoppingListItem> ShoppingListItems => Set<ShoppingListItem>();
    public DbSet<RecipeIngredient> RecipeIngredients => Set<RecipeIngredient>();
    public DbSet<RecipeStep> RecipeSteps => Set<RecipeStep>();

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

        // Collection ilişkileri
        modelBuilder.Entity<Collection>()
            .HasMany(c => c.CollectionRecipes)
            .WithOne(cr => cr.Collection)
            .HasForeignKey(cr => cr.CollectionId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<CollectionRecipe>()
            .HasOne(cr => cr.Recipe)
            .WithMany()
            .HasForeignKey(cr => cr.RecipeId)
            .OnDelete(DeleteBehavior.Cascade);

        // Index'ler (performans için)
        modelBuilder.Entity<Collection>()
            .HasIndex(c => c.UserId);

        modelBuilder.Entity<CollectionRecipe>()
            .HasIndex(cr => new { cr.CollectionId, cr.RecipeId })
            .IsUnique(); // Bir tarif bir koleksiyonda sadece bir kez olabilir

        modelBuilder.Entity<CollectionRecipe>()
            .HasIndex(cr => cr.CollectionId);

        modelBuilder.Entity<CollectionRecipe>()
            .HasIndex(cr => cr.RecipeId);

        // RecipeComment ilişkileri
        modelBuilder.Entity<RecipeComment>()
            .HasOne(rc => rc.Recipe)
            .WithMany()
            .HasForeignKey(rc => rc.RecipeId)
            .OnDelete(DeleteBehavior.Cascade);

        // Yorum yanıtlama - self-referencing relationship
        modelBuilder.Entity<RecipeComment>()
            .HasOne(rc => rc.ParentComment)
            .WithMany(rc => rc.Replies)
            .HasForeignKey(rc => rc.ParentCommentId)
            .OnDelete(DeleteBehavior.Restrict); // Ana yorum silinirse yanıtlar da silinir (cascade yerine restrict kullanıyoruz)

        modelBuilder.Entity<RecipeComment>()
            .HasMany(rc => rc.CommentLikes)
            .WithOne(cl => cl.Comment)
            .HasForeignKey(cl => cl.CommentId)
            .OnDelete(DeleteBehavior.Cascade);

        // CommentLike ilişkileri
        modelBuilder.Entity<CommentLike>()
            .HasOne(cl => cl.Comment)
            .WithMany(rc => rc.CommentLikes)
            .HasForeignKey(cl => cl.CommentId)
            .OnDelete(DeleteBehavior.Cascade);

        // Index'ler (performans için)
        modelBuilder.Entity<RecipeComment>()
            .HasIndex(rc => rc.RecipeId);

        modelBuilder.Entity<RecipeComment>()
            .HasIndex(rc => rc.UserId);

        modelBuilder.Entity<RecipeComment>()
            .HasIndex(rc => rc.CreatedAt);

        modelBuilder.Entity<RecipeComment>()
            .HasIndex(rc => rc.ParentCommentId); // Yanıtları hızlı bulmak için

        modelBuilder.Entity<CommentLike>()
            .HasIndex(cl => new { cl.CommentId, cl.UserId })
            .IsUnique(); // Bir kullanıcı bir yorumu sadece bir kez beğenebilir

        modelBuilder.Entity<CommentLike>()
            .HasIndex(cl => cl.CommentId);

        // MealPlan ilişkileri
        modelBuilder.Entity<MealPlan>()
            .HasMany(mp => mp.Items)
            .WithOne(mpi => mpi.MealPlan)
            .HasForeignKey(mpi => mpi.MealPlanId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<MealPlanItem>()
            .HasOne(mpi => mpi.Recipe)
            .WithMany()
            .HasForeignKey(mpi => mpi.RecipeId)
            .OnDelete(DeleteBehavior.Restrict); // Tarif silinirse plan öğesi de silinmez, ama tarif silinemez

        // MealType enum'ını string olarak sakla
        modelBuilder.Entity<MealPlanItem>()
            .Property(mpi => mpi.MealType)
            .HasConversion<string>();

        // Index'ler
        modelBuilder.Entity<MealPlan>()
            .HasIndex(mp => mp.UserId);

        modelBuilder.Entity<MealPlan>()
            .HasIndex(mp => mp.StartDate);

        modelBuilder.Entity<MealPlanItem>()
            .HasIndex(mpi => mpi.MealPlanId);

        modelBuilder.Entity<MealPlanItem>()
            .HasIndex(mpi => mpi.RecipeId);

        modelBuilder.Entity<MealPlanItem>()
            .HasIndex(mpi => mpi.Date);

        // ShoppingList ilişkileri
        modelBuilder.Entity<ShoppingList>()
            .HasOne(sl => sl.MealPlan)
            .WithMany()
            .HasForeignKey(sl => sl.MealPlanId)
            .OnDelete(DeleteBehavior.SetNull); // Meal plan silinirse shopping list kalır

        modelBuilder.Entity<ShoppingList>()
            .HasMany(sl => sl.Items)
            .WithOne(sli => sli.ShoppingList)
            .HasForeignKey(sli => sli.ShoppingListId)
            .OnDelete(DeleteBehavior.Cascade);

        // Index'ler
        modelBuilder.Entity<ShoppingList>()
            .HasIndex(sl => sl.UserId);

        modelBuilder.Entity<ShoppingList>()
            .HasIndex(sl => sl.MealPlanId);

        modelBuilder.Entity<ShoppingListItem>()
            .HasIndex(sli => sli.ShoppingListId);

        // RecipeIngredient ilişkileri
        modelBuilder.Entity<RecipeIngredient>()
            .HasOne(ri => ri.Recipe)
            .WithMany(r => r.Ingredients)
            .HasForeignKey(ri => ri.RecipeId)
            .OnDelete(DeleteBehavior.Cascade);

        // RecipeStep ilişkileri
        modelBuilder.Entity<RecipeStep>()
            .HasOne(rs => rs.Recipe)
            .WithMany(r => r.Steps)
            .HasForeignKey(rs => rs.RecipeId)
            .OnDelete(DeleteBehavior.Cascade);

        // Index'ler (performans için)
        modelBuilder.Entity<RecipeIngredient>()
            .HasIndex(ri => ri.RecipeId);

        modelBuilder.Entity<RecipeIngredient>()
            .HasIndex(ri => new { ri.RecipeId, ri.Order });

        modelBuilder.Entity<RecipeStep>()
            .HasIndex(rs => rs.RecipeId);

        modelBuilder.Entity<RecipeStep>()
            .HasIndex(rs => new { rs.RecipeId, rs.Order });
    }
}


