using BackendApi.Application.DTOs;
using BackendApi.Domain.Entities;

namespace BackendApi.Application.Mappings;

public static class RecipeMapper
{
    public static Recipe ToEntity(this CreateRecipeDto dto)
    {
        var recipe = new Recipe
        {
            Title = dto.Title,
            Description = dto.Description,
            PrepTimeMinutes = dto.PrepTimeMinutes,
            CookingTimeMinutes = dto.CookingTimeMinutes,
            Servings = dto.Servings,
            Difficulty = Enum.TryParse<DifficultyLevel>(dto.Difficulty, true, out var difficulty)
                ? difficulty
                : DifficultyLevel.Orta,
            ImageUrl = dto.ImageUrl,
            Tips = dto.Tips,
            AlternativeIngredients = dto.AlternativeIngredients,
            NutritionInfo = dto.NutritionInfo,
            CategoryId = dto.CategoryId,
            AuthorId = dto.AuthorId,
            IsFeatured = dto.IsFeatured
        };

        // Malzemeleri ekle
        if (dto.Ingredients != null && dto.Ingredients.Count > 0)
        {
            recipe.Ingredients = dto.Ingredients
                .Select((name, index) => new RecipeIngredient
                {
                    Name = name.Trim(),
                    Order = index + 1
                })
                .ToList();
        }
        // Backward compatibility: Eğer liste boşsa ama string varsa, string'den parse et
        else if (!string.IsNullOrWhiteSpace(dto.IngredientsString))
        {
            recipe.Ingredients = dto.IngredientsString
                .Split('\n', StringSplitOptions.RemoveEmptyEntries)
                .Select((name, index) => new RecipeIngredient
                {
                    Name = name.Trim(),
                    Order = index + 1
                })
                .ToList();
        }

        // Adımları ekle
        if (dto.Steps != null && dto.Steps.Count > 0)
        {
            recipe.Steps = dto.Steps
                .Select((description, index) => new RecipeStep
                {
                    Description = description.Trim(),
                    Order = index + 1
                })
                .ToList();
        }
        // Backward compatibility: Eğer liste boşsa ama string varsa, string'den parse et
        else if (!string.IsNullOrWhiteSpace(dto.StepsString))
        {
            recipe.Steps = dto.StepsString
                .Split('\n', StringSplitOptions.RemoveEmptyEntries)
                .Select((description, index) => new RecipeStep
                {
                    Description = description.Trim(),
                    Order = index + 1
                })
                .ToList();
        }

        // Çoklu fotoğrafları ekle
        if (dto.ImageUrls != null && dto.ImageUrls.Count > 0)
        {
            var primaryIndex = dto.PrimaryImageIndex ?? 0;
            if (primaryIndex < 0 || primaryIndex >= dto.ImageUrls.Count)
                primaryIndex = 0;

            recipe.Images = dto.ImageUrls
                .Where(url => !string.IsNullOrWhiteSpace(url))
                .Select((url, index) => new RecipeImage
                {
                    ImageUrl = url.Trim(),
                    IsPrimary = index == primaryIndex,
                    DisplayOrder = index
                })
                .ToList();

            // Ana fotoğrafı ImageUrl'e de set et (backward compatibility)
            if (recipe.Images.Any())
            {
                var primaryImage = recipe.Images.FirstOrDefault(img => img.IsPrimary) ?? recipe.Images.First();
                recipe.ImageUrl = primaryImage.ImageUrl;
            }
        }
        // Backward compatibility: Eğer ImageUrls yoksa ama ImageUrl varsa
        else if (!string.IsNullOrWhiteSpace(dto.ImageUrl))
        {
            recipe.Images = new List<RecipeImage>
            {
                new RecipeImage
                {
                    ImageUrl = dto.ImageUrl.Trim(),
                    IsPrimary = true,
                    DisplayOrder = 0
                }
            };
        }

        return recipe;
    }

    public static void UpdateEntity(this Recipe existing, CreateRecipeDto dto)
    {
        existing.Title = dto.Title;
        existing.Description = dto.Description;
        existing.PrepTimeMinutes = dto.PrepTimeMinutes;
        existing.CookingTimeMinutes = dto.CookingTimeMinutes;
        existing.Servings = dto.Servings;
        existing.Difficulty = Enum.TryParse<DifficultyLevel>(dto.Difficulty, true, out var difficulty)
            ? difficulty
            : DifficultyLevel.Orta;
        existing.ImageUrl = dto.ImageUrl;
        existing.Tips = dto.Tips;
        existing.AlternativeIngredients = dto.AlternativeIngredients;
        existing.NutritionInfo = dto.NutritionInfo;
        existing.CategoryId = dto.CategoryId;
        existing.AuthorId = dto.AuthorId;
        existing.IsFeatured = dto.IsFeatured;
        existing.UpdatedAt = DateTime.UtcNow;

        // Mevcut malzemeleri ve adımları temizle
        existing.Ingredients.Clear();
        existing.Steps.Clear();

        // Yeni malzemeleri ekle
        if (dto.Ingredients != null && dto.Ingredients.Count > 0)
        {
            existing.Ingredients = dto.Ingredients
                .Select((name, index) => new RecipeIngredient
                {
                    RecipeId = existing.Id,
                    Name = name.Trim(),
                    Order = index + 1
                })
                .ToList();
        }
        // Backward compatibility
        else if (!string.IsNullOrWhiteSpace(dto.IngredientsString))
        {
            existing.Ingredients = dto.IngredientsString
                .Split('\n', StringSplitOptions.RemoveEmptyEntries)
                .Select((name, index) => new RecipeIngredient
                {
                    RecipeId = existing.Id,
                    Name = name.Trim(),
                    Order = index + 1
                })
                .ToList();
        }

        // Yeni adımları ekle
        if (dto.Steps != null && dto.Steps.Count > 0)
        {
            existing.Steps = dto.Steps
                .Select((description, index) => new RecipeStep
                {
                    RecipeId = existing.Id,
                    Description = description.Trim(),
                    Order = index + 1
                })
                .ToList();
        }
        // Backward compatibility
        else if (!string.IsNullOrWhiteSpace(dto.StepsString))
        {
            existing.Steps = dto.StepsString
                .Split('\n', StringSplitOptions.RemoveEmptyEntries)
                .Select((description, index) => new RecipeStep
                {
                    RecipeId = existing.Id,
                    Description = description.Trim(),
                    Order = index + 1
                })
                .ToList();
        }

        // Çoklu fotoğrafları güncelle
        if (dto.ImageUrls != null && dto.ImageUrls.Count > 0)
        {
            // Mevcut fotoğrafları temizle
            existing.Images.Clear();

            var primaryIndex = dto.PrimaryImageIndex ?? 0;
            if (primaryIndex < 0 || primaryIndex >= dto.ImageUrls.Count)
                primaryIndex = 0;

            existing.Images = dto.ImageUrls
                .Where(url => !string.IsNullOrWhiteSpace(url))
                .Select((url, index) => new RecipeImage
                {
                    RecipeId = existing.Id,
                    ImageUrl = url.Trim(),
                    IsPrimary = index == primaryIndex,
                    DisplayOrder = index
                })
                .ToList();

            // Ana fotoğrafı ImageUrl'e de set et (backward compatibility)
            if (existing.Images.Any())
            {
                var primaryImage = existing.Images.FirstOrDefault(img => img.IsPrimary) ?? existing.Images.First();
                existing.ImageUrl = primaryImage.ImageUrl;
            }
        }
        // Backward compatibility: Eğer ImageUrls yoksa ama ImageUrl varsa
        else if (!string.IsNullOrWhiteSpace(dto.ImageUrl))
        {
            // Mevcut fotoğrafları temizle ve yeni ekle
            existing.Images.Clear();
            existing.Images.Add(new RecipeImage
            {
                RecipeId = existing.Id,
                ImageUrl = dto.ImageUrl.Trim(),
                IsPrimary = true,
                DisplayOrder = 0
            });
        }
    }

    public static RecipeResponseDto ToDto(this Recipe recipe)
    {
        var dto = new RecipeResponseDto
        {
            Id = recipe.Id,
            Title = recipe.Title,
            Description = recipe.Description,
            PrepTimeMinutes = recipe.PrepTimeMinutes,
            CookingTimeMinutes = recipe.CookingTimeMinutes,
            Servings = recipe.Servings,
            Difficulty = recipe.Difficulty.ToString(),
            ImageUrl = recipe.ImageUrl,
            Tips = recipe.Tips,
            AlternativeIngredients = recipe.AlternativeIngredients,
            NutritionInfo = recipe.NutritionInfo,
            ViewCount = recipe.ViewCount,
            AverageRating = recipe.AverageRating,
            RatingCount = recipe.RatingCount,
            LikeCount = recipe.LikeCount,
            IsFeatured = recipe.IsFeatured,
            CategoryId = recipe.CategoryId,
            Category = recipe.Category != null ? new CategoryDto
            {
                Id = recipe.Category.Id,
                Name = recipe.Category.Name,
                Description = recipe.Category.Description,
                Icon = recipe.Category.Icon
            } : null,
            AuthorId = recipe.AuthorId,
            Author = recipe.Author != null ? new AuthorDto
            {
                Id = recipe.Author.Id,
                UserId = recipe.Author.UserId,
                DisplayName = recipe.Author.DisplayName,
                Bio = recipe.Author.Bio,
                ProfileImageUrl = recipe.Author.ProfileImageUrl
            } : null,
            CreatedAt = recipe.CreatedAt,
            UpdatedAt = recipe.UpdatedAt
        };

        // Fotoğrafları DTO'ya çevir
        if (recipe.Images != null && recipe.Images.Any())
        {
            dto.Images = recipe.Images
                .OrderBy(img => img.DisplayOrder)
                .Select(img => new RecipeImageDto
                {
                    Id = img.Id,
                    RecipeId = img.RecipeId,
                    ImageUrl = img.ImageUrl,
                    IsPrimary = img.IsPrimary,
                    DisplayOrder = img.DisplayOrder,
                    CreatedAt = img.CreatedAt
                })
                .ToList();

            // Ana fotoğrafı ImageUrl'e set et (backward compatibility)
            var primaryImage = dto.Images.FirstOrDefault(img => img.IsPrimary) ?? dto.Images.First();
            if (primaryImage != null)
            {
                dto.ImageUrl = primaryImage.ImageUrl;
            }
        }
        // Backward compatibility: Eğer Images yoksa ama ImageUrl varsa
        else if (!string.IsNullOrWhiteSpace(recipe.ImageUrl))
        {
            dto.ImageUrl = recipe.ImageUrl;
            dto.Images = new List<RecipeImageDto>
            {
                new RecipeImageDto
                {
                    RecipeId = recipe.Id,
                    ImageUrl = recipe.ImageUrl,
                    IsPrimary = true,
                    DisplayOrder = 0,
                    CreatedAt = recipe.CreatedAt
                }
            };
        }

        // Malzemeleri DTO'ya çevir
        if (recipe.Ingredients != null && recipe.Ingredients.Any())
        {
            dto.Ingredients = recipe.Ingredients
                .OrderBy(i => i.Order)
                .Select(i => new IngredientDto
                {
                    Id = i.Id,
                    Name = i.Name,
                    Order = i.Order
                })
                .ToList();
            
            // Backward compatibility için string formatı
            dto.IngredientsString = string.Join("\n", dto.Ingredients.Select(i => i.Name));
        }
        // Backward compatibility: Eğer collection boşsa ama string varsa
        else if (!string.IsNullOrWhiteSpace(recipe.IngredientsString))
        {
            dto.IngredientsString = recipe.IngredientsString;
            dto.Ingredients = recipe.IngredientsString
                .Split('\n', StringSplitOptions.RemoveEmptyEntries)
                .Select((name, index) => new IngredientDto
                {
                    Name = name.Trim(),
                    Order = index + 1
                })
                .ToList();
        }

        // Adımları DTO'ya çevir
        if (recipe.Steps != null && recipe.Steps.Any())
        {
            dto.Steps = recipe.Steps
                .OrderBy(s => s.Order)
                .Select(s => new StepDto
                {
                    Id = s.Id,
                    Description = s.Description,
                    Order = s.Order
                })
                .ToList();
            
            // Backward compatibility için string formatı
            dto.StepsString = string.Join("\n", dto.Steps.Select(s => s.Description));
        }
        // Backward compatibility: Eğer collection boşsa ama string varsa
        else if (!string.IsNullOrWhiteSpace(recipe.StepsString))
        {
            dto.StepsString = recipe.StepsString;
            dto.Steps = recipe.StepsString
                .Split('\n', StringSplitOptions.RemoveEmptyEntries)
                .Select((description, index) => new StepDto
                {
                    Description = description.Trim(),
                    Order = index + 1
                })
                .ToList();
        }

        return dto;
    }
}

