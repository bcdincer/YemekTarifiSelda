# Gelecekte Queue Yapısı Eklendiğinde - Örnek Implementasyon

## 📦 Senaryo: Email Bildirimi Eklendiğinde

### Senaryo
Kullanıcı yeni tarif eklediğinde admin'e email gönderilsin. Bu işlem uzun sürebilir, bu yüzden async olmalı.

## 🔧 Hangfire ile Örnek Implementasyon

### 1. Package Installation

```bash
dotnet add package Hangfire.Core
dotnet add package Hangfire.AspNetCore
dotnet add package Hangfire.PostgreSql
```

### 2. Program.cs Configuration

```csharp
using Hangfire;
using Hangfire.PostgreSql;

// Hangfire Configuration
builder.Services.AddHangfire(config => config
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UsePostgreSqlStorage(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddHangfireServer();

var app = builder.Build();

// Hangfire Dashboard (Development only)
if (app.Environment.IsDevelopment())
{
    app.MapHangfireDashboard("/hangfire");
}
```

### 3. Email Service (Background Job)

```csharp
// Application/Services/IEmailService.cs
public interface IEmailService
{
    Task SendRecipeCreatedEmailAsync(int recipeId, string recipeTitle);
}

// Application/Services/EmailService.cs
public class EmailService : IEmailService
{
    private readonly ILogger<EmailService> _logger;
    
    public EmailService(ILogger<EmailService> logger)
    {
        _logger = logger;
    }
    
    public async Task SendRecipeCreatedEmailAsync(int recipeId, string recipeTitle)
    {
        // Email gönderme logic
        _logger.LogInformation("Sending email for recipe {RecipeId}: {RecipeTitle}", recipeId, recipeTitle);
        
        // Simulate email sending
        await Task.Delay(2000);
        
        _logger.LogInformation("Email sent successfully for recipe {RecipeId}", recipeId);
    }
}
```

### 4. RecipeService Güncelleme

```csharp
// Application/Services/RecipeService.cs
public class RecipeService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<RecipeService> _logger;
    private readonly IBackgroundJobClient _backgroundJobClient;
    
    public RecipeService(
        IUnitOfWork unitOfWork, 
        ILogger<RecipeService> logger,
        IBackgroundJobClient backgroundJobClient)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
        _backgroundJobClient = backgroundJobClient;
    }
    
    public async Task<RecipeResponseDto> CreateAsync(Recipe recipe)
    {
        try
        {
            recipe.CreatedAt = DateTime.UtcNow;
            await Repository.AddAsync(recipe);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Recipe '{RecipeTitle}' created with id {RecipeId}", recipe.Title, recipe.Id);

            // Email'i background job olarak queue'ya ekle
            _backgroundJobClient.Enqueue<IEmailService>(x => 
                x.SendRecipeCreatedEmailAsync(recipe.Id, recipe.Title));

            var createdRecipe = await Repository.GetByIdAsync(recipe.Id);
            if (createdRecipe == null)
                throw new InvalidOperationException("Recipe was created but could not be retrieved");
            
            return createdRecipe.ToDto();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating recipe '{RecipeTitle}'", recipe?.Title);
            throw;
        }
    }
}
```

### 5. Delayed Job Örneği

```csharp
// 1 saat sonra çalışacak job
_backgroundJobClient.Schedule<IEmailService>(
    x => x.SendRecipeCreatedEmailAsync(recipe.Id, recipe.Title),
    TimeSpan.FromHours(1));
```

### 6. Recurring Job Örneği

```csharp
// Her gün saat 08:00'da çalışacak job
RecurringJob.AddOrUpdate<IReportService>(
    "daily-recipe-report",
    x => x.GenerateDailyReportAsync(),
    Cron.Daily(8, 0));
```

## 🎯 Avantajlar

1. **Non-blocking**: Email gönderme işlemi API response'unu bloklamaz
2. **Retry**: Başarısız işlemler otomatik retry edilir
3. **Dashboard**: `/hangfire` endpoint'inde tüm job'ları görebilirsiniz
4. **Scheduled Jobs**: Zamanlanmış görevler ekleyebilirsiniz
5. **Monitoring**: Job status'larını takip edebilirsiniz

## 🔍 Hangfire Dashboard

Erişim: `https://localhost:7016/hangfire`

- ✅ Job listesi
- ✅ Job durumları (Pending, Processing, Succeeded, Failed)
- ✅ Retry mekanizması
- ✅ Job detayları
- ✅ Statistics

## ⚠️ Dikkat Edilmesi Gerekenler

1. **Database**: Hangfire kendi tablolarını oluşturur (PostgreSQL'de)
2. **Security**: Dashboard'u production'da koruyun
3. **Connection String**: Default connection string kullanılır
4. **Performance**: Büyük ölçekte Redis backend kullanılabilir

## 📝 Özet

Queue yapısı şu an **GEREKLİ DEĞİL** ama ileride eklenecekse **Hangfire** en basit ve en uygun çözüm olacaktır.

