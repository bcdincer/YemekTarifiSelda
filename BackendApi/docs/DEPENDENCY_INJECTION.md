# Dependency Injection (IoC Container) Kullanımı

## 📦 Mevcut Durum

Projede **ASP.NET Core'un built-in Dependency Injection Container** kullanılıyor. Bu, Microsoft.Extensions.DependencyInjection paketidir ve .NET Core ile birlikte gelir.

## ✅ Kullanılan IoC Container: **Microsoft.Extensions.DependencyInjection**

### Neden Bu Container?

1. **Built-in**: .NET Core ile birlikte gelir, ekstra paket gerekmez
2. **Performans**: Yüksek performanslı ve optimize edilmiş
3. **Basitlik**: Kolay kullanım ve yapılandırma
4. **Yeterlilik**: Çoğu enterprise uygulama için yeterli özelliklere sahip
5. **Resmi Destek**: Microsoft tarafından resmi olarak desteklenir

## 🏗️ Mevcut DI Yapılandırması

### Service Lifetime'ları

- **Scoped**: Her HTTP request için bir instance (DbContext, UnitOfWork, Services, Repositories)
- **Singleton**: Uygulama boyunca tek instance (genellikle kullanılmıyor)
- **Transient**: Her injection'da yeni instance (genellikle kullanılmıyor)

### Kayıtlı Servisler

```csharp
// Infrastructure Layer
- IUnitOfWork → UnitOfWork (Scoped)
- IRecipeRepository → RecipeRepository (Scoped)
- ICategoryRepository → CategoryRepository (Scoped)
- IRatingRepository → RatingRepository (Scoped)
- ILikeRepository → LikeRepository (Scoped)

// Application Layer
- IRecipeService → RecipeService (Scoped)
- ICategoryService → CategoryService (Scoped)
- IRatingService → RatingService (Scoped)
- ILikeService → LikeService (Scoped)

// Validation
- IValidator<CreateRecipeDto> → CreateRecipeDtoValidator (Scoped)
- IValidator<CreateCategoryDto> → CategoryDtoValidator (Scoped)
```

## 📁 Organizasyon

Tüm DI kayıtları `ServiceCollectionExtensions.cs` dosyasında merkezi olarak yönetiliyor:

```csharp
builder.Services.AddApplicationServices();
```

Bu yaklaşım:
- ✅ **SRP**: DI yapılandırması tek bir yerde
- ✅ **Maintainability**: Yeni servis eklemek kolay
- ✅ **Testability**: Test'lerde mock'lanabilir
- ✅ **Clean Code**: Program.cs daha temiz

## 🔄 Alternatif Container'lar (Gerekirse)

Eğer daha gelişmiş özellikler gerekiyorsa:

### 1. **AutoFac** (En Popüler)
```csharp
// Özellikler:
- Property injection
- Module-based configuration
- Advanced lifetime management
- Interception support
```

### 2. **Simple Injector**
```csharp
// Özellikler:
- Compile-time verification
- Lifestyle management
- Decorator pattern support
```

### 3. **Ninject**
```csharp
// Özellikler:
- Convention-based binding
- Conditional binding
```

## 💡 Ne Zaman Alternatif Container Gerekir?

### Built-in Container Yeterli Olduğunda:
- ✅ Constructor injection
- ✅ Scoped/Singleton/Transient lifetime
- ✅ Interface-based DI
- ✅ Factory pattern
- ✅ Service locator pattern (önerilmez ama mümkün)

### Alternatif Container Gerekli Olduğunda:
- ❌ Property injection (built-in desteklemez)
- ❌ Decorator pattern (zor)
- ❌ Interception/AOP (aspect-oriented programming)
- ❌ Convention-based registration (çok sayıda servis için)
- ❌ Advanced lifetime management

## 🎯 Mevcut Proje İçin Değerlendirme

### ✅ **Built-in Container YETERLİ**

Projede şu özellikler kullanılıyor:
- Constructor injection ✅
- Interface-based DI ✅
- Scoped lifetime ✅
- Service registration ✅
- FluentValidation integration ✅

**Sonuç**: Mevcut built-in DI container projenin ihtiyaçlarını karşılıyor. Alternatif bir container'a geçmeye gerek yok.

## 📝 Best Practices (Mevcut Implementasyon)

1. ✅ **Extension Methods**: DI kayıtları extension method'larda
2. ✅ **Scoped Lifetime**: DbContext ve UnitOfWork için doğru lifetime
3. ✅ **Interface Segregation**: Her servis için interface
4. ✅ **Dependency Inversion**: Concrete class'lara değil interface'lere bağımlılık
5. ✅ **Single Responsibility**: Her servis tek sorumluluğa sahip

## 🔧 Gelecekte İyileştirme Önerileri

Eğer proje büyürse ve daha gelişmiş özellikler gerekiyorsa:

1. **AutoFac Module Pattern**:
```csharp
public class ApplicationModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        builder.RegisterType<RecipeService>().As<IRecipeService>();
        // ...
    }
}
```

2. **Convention-based Registration**:
```csharp
// Tüm I*Service interface'lerini otomatik kaydet
builder.RegisterAssemblyTypes(assembly)
    .Where(t => t.Name.EndsWith("Service"))
    .AsImplementedInterfaces();
```

## 📊 Genel Değerlendirme

**Mevcut DI Container: 10/10** ✅

- ASP.NET Core built-in DI container kullanılıyor
- Tüm best practice'lere uygun
- Merkezi yapılandırma
- Clean Architecture ile uyumlu
- SOLID principles'e uygun

**Sonuç**: Mevcut DI yapılandırması mükemmel durumda. Alternatif container'a geçmeye gerek yok.

