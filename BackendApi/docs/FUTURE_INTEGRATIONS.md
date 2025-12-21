# Gelecekteki Entegrasyonlar için Hazırlık

## 📋 Özet

Projenize şu özellikler eklendi:
1. ✅ **Hangfire** - Background jobs ve queue yapısı
2. ✅ **Email Service** - Email gönderme için interface ve temel implementasyon
3. ✅ **Domain Events** - Event-driven architecture için temel yapı
4. ✅ **External Service Interface** - Harici sistemlerle entegrasyon için abstraction
5. ✅ **AI Agent Interface** - Yapay zeka agentları için hazır yapı

## 🔧 Kullanım Senaryoları

### 1. Email Entegrasyonu

#### Şu Anki Durum
- ✅ `IEmailService` interface tanımlandı
- ✅ `EmailService` temel implementasyon (şu an sadece log yazıyor)
- ✅ Event-driven: Tarif oluşturulduğunda otomatik email gönderimi

#### Yapılacaklar

**SMTP ile Email Gönderme:**
```bash
dotnet add package MailKit
```

**SendGrid ile Email Gönderme:**
```bash
dotnet add package SendGrid
```

**AWS SES ile Email Gönderme:**
```bash
dotnet add package AWSSDK.SimpleEmail
```

**Örnek SMTP Implementasyonu:**
```csharp
// Infrastructure/Services/EmailService.cs
public async Task SendNotificationEmailAsync(string to, string subject, string body, bool isHtml = true)
{
    using var client = new SmtpClient();
    await client.ConnectAsync(_configuration["Email:SmtpHost"], 
        int.Parse(_configuration["Email:SmtpPort"]), 
        SecureSocketOptions.StartTls);
    
    await client.AuthenticateAsync(_configuration["Email:Username"], 
        _configuration["Email:Password"]);
    
    var message = new MimeMessage();
    message.From.Add(new MailboxAddress("Recipe Site", _configuration["Email:From"]));
    message.To.Add(new MailboxAddress("", to));
    message.Subject = subject;
    message.Body = new TextPart(isHtml ? "html" : "plain") { Text = body };
    
    await client.SendAsync(message);
    await client.DisconnectAsync(true);
}
```

**appsettings.json'a ekle:**
```json
{
  "Email": {
    "SmtpHost": "smtp.gmail.com",
    "SmtpPort": "587",
    "Username": "your-email@gmail.com",
    "Password": "your-app-password",
    "From": "noreply@recipesite.com"
  }
}
```

### 2. External Service Entegrasyonları

#### Senaryolar
- ✅ Payment gateway (Stripe, PayPal)
- ✅ Social media APIs (Facebook, Instagram, Twitter)
- ✅ Recipe data providers
- ✅ Image storage services (Azure Blob, AWS S3, Cloudinary)

#### Örnek: Cloudinary Image Service
```csharp
// Infrastructure/Services/CloudinaryImageService.cs
public class CloudinaryImageService : IExternalService, IImageStorageService
{
    public string ServiceName => "Cloudinary";
    
    public async Task<bool> IsAvailableAsync()
    {
        // Health check
        return true;
    }
    
    public async Task<string> UploadImageAsync(Stream imageStream, string fileName)
    {
        // Upload to Cloudinary
        // Return image URL
    }
}
```

### 3. AI Agent Entegrasyonları

#### Senaryolar

**1. Recipe Content Analysis**
```csharp
// AI ile tarif içeriğini analiz et
var analysis = await _aiAgentService.AnalyzeRecipeAsync(recipeId, recipeContent);
// Score: 0-100, Suggestions, Warnings
```

**2. Recipe Suggestions**
```csharp
// Kullanıcı tercihlerine göre AI önerileri
var suggestions = await _aiAgentService.GetRecipeSuggestionsAsync(userId, preferences);
```

**3. Content Generation**
```csharp
// AI ile tarif açıklaması oluştur
var description = await _aiAgentService.GenerateRecipeContentAsync(
    "Create a description for a pasta recipe", 
    RecipeContentType.Description
);
```

#### Örnek: OpenAI GPT Entegrasyonu
```bash
dotnet add package OpenAI
```

```csharp
// Infrastructure/Services/OpenAiAgentService.cs
public class OpenAiAgentService : IAiAgentService
{
    private readonly OpenAIClient _openAiClient;
    
    public async Task<RecipeAnalysisResult> AnalyzeRecipeAsync(int recipeId, string recipeContent)
    {
        var prompt = $"Analyze this recipe and provide a score and suggestions: {recipeContent}";
        var response = await _openAiClient.ChatEndpoint.GetCompletionAsync(prompt);
        
        // Parse response and return
        return new RecipeAnalysisResult { ... };
    }
}
```

#### Örnek: Azure OpenAI
```bash
dotnet add package Azure.AI.OpenAI
```

## 🚀 Migration Adımları

### 1. Hangfire Tables Oluşturulması

Hangfire otomatik olarak PostgreSQL'de kendi tablolarını oluşturur. İlk çalıştırmada:
- `hangfire.schema`
- `hangfire.counter`
- `hangfire.hash`
- `hangfire.job`
- `hangfire.list`
- `hangfire.set`
- `hangfire.state`

### 2. Hangfire Dashboard'a Erişim

Development: `https://localhost:7016/hangfire`

Production için authorization ekleyin:
```csharp
// Infrastructure/Hangfire/HangfireDashboardAuthorizationFilter.cs
public bool Authorize(DashboardContext context)
{
    var httpContext = context.GetHttpContext();
    return httpContext.User.Identity?.IsAuthenticated == true 
        && httpContext.User.IsInRole("Admin");
}
```

## 📊 Queue Yapısı

Hangfire 3 queue tanımlandı:
1. **default** - Genel işler
2. **emails** - Email gönderme işleri
3. **ai-processing** - AI işlemleri (daha yüksek öncelik)

Örnek kullanım:
```csharp
// Email için özel queue
BackgroundJob.Enqueue(() => SendEmailAsync(), "emails");

// AI processing için özel queue
BackgroundJob.Enqueue(() => ProcessWithAIAsync(), "ai-processing");
```

## 🔐 Güvenlik Notları

1. **Email Service**: SMTP credentials'ları `appsettings.json`'da saklamayın, User Secrets veya Azure Key Vault kullanın
2. **AI API Keys**: Environment variables veya secure configuration kullanın
3. **External APIs**: Rate limiting ve retry logic ekleyin
4. **Hangfire Dashboard**: Production'da mutlaka authentication ekleyin

## 📝 Örnek Kullanım Senaryoları

### Senaryo 1: Yeni Kullanıcı Kaydı → Email Doğrulama

```csharp
// AccountController.cs
public async Task<IActionResult> Register(RegisterViewModel model)
{
    var user = new ApplicationUser { ... };
    var result = await _userManager.CreateAsync(user, model.Password);
    
    if (result.Succeeded)
    {
        var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
        
        // Email'i background job olarak gönder
        BackgroundJob.Enqueue<IEmailService>(x => 
            x.SendVerificationEmailAsync(user.Email, token, user.UserName));
        
        return Ok();
    }
}
```

### Senaryo 2: Tarif Oluşturuldu → AI Analiz + Admin Bildirimi

```csharp
// RecipeService.cs - CreateAsync içinde
var recipeCreatedEvent = new RecipeCreatedEvent(...);
await _eventPublisher.PublishAsync(recipeCreatedEvent);

// Event handler'da:
// 1. Email gönder (emails queue)
BackgroundJob.Enqueue(() => SendAdminNotificationAsync(...), "emails");

// 2. AI analiz et (ai-processing queue)
BackgroundJob.Enqueue(() => AnalyzeRecipeWithAIAsync(recipeId), "ai-processing");
```

### Senaryo 3: Scheduled Job - Günlük Rapor

```csharp
// Program.cs veya bir startup service'te
RecurringJob.AddOrUpdate<IReportService>(
    "daily-recipe-report",
    x => x.GenerateDailyReportAsync(),
    Cron.Daily(8, 0)); // Her gün saat 08:00
```

## 🔄 İleride Eklenebilecekler

1. **MediatR** - CQRS pattern için
2. **SignalR** - Real-time notifications
3. **Redis Cache** - Performance için
4. **Elasticsearch** - Gelişmiş search
5. **RabbitMQ** - Daha advanced message queue (şu an Hangfire yeterli)

## ✅ Sonuç

Projeniz artık şu özelliklere hazır:
- ✅ Email entegrasyonu (SMTP, SendGrid, AWS SES)
- ✅ Background jobs (Hangfire)
- ✅ Event-driven architecture
- ✅ External service entegrasyonları
- ✅ AI agent entegrasyonları

Tüm yapı SOLID prensiplere uygun, test edilebilir ve genişletilebilir şekilde tasarlandı.

