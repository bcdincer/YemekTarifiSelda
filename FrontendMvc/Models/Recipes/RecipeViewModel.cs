using System.Text.Json.Serialization;

namespace FrontendMvc.Models.Recipes;

public class RecipeViewModel
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Ingredients { get; set; } = string.Empty;
    public string Steps { get; set; } = string.Empty;
    public int PrepTimeMinutes { get; set; }
    public int CookingTimeMinutes { get; set; }
    public int Servings { get; set; }
    
    // Difficulty hem string hem int olarak gelebilir, bu yüzden JsonElement kullanıyoruz
    [JsonIgnore]
    public object? DifficultyRaw { get; set; }
    
    // Difficulty property - JSON'dan gelen değeri string'e çevirir
    [JsonPropertyName("difficulty")]
    [JsonConverter(typeof(DifficultyJsonConverter))]
    public string Difficulty { get; set; } = "Orta";
    
    public string? ImageUrl { get; set; }
    public string? Tips { get; set; }
    public string? AlternativeIngredients { get; set; }
    public string? NutritionInfo { get; set; }
    public int ViewCount { get; set; }
    public double? AverageRating { get; set; }
    public int RatingCount { get; set; }
    public int LikeCount { get; set; }
    public bool IsFeatured { get; set; }
    public int? CategoryId { get; set; }
    public CategoryViewModel? Category { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

// Custom JSON converter for Difficulty
public class DifficultyJsonConverter : JsonConverter<string>
{
    public override string Read(ref System.Text.Json.Utf8JsonReader reader, Type typeToConvert, System.Text.Json.JsonSerializerOptions options)
    {
        if (reader.TokenType == System.Text.Json.JsonTokenType.String)
        {
            return reader.GetString() ?? "Orta";
        }
        else if (reader.TokenType == System.Text.Json.JsonTokenType.Number)
        {
            var value = reader.GetInt32();
            return value switch
            {
                1 => "Kolay",
                2 => "Orta",
                3 => "Zor",
                _ => "Orta"
            };
        }
        return "Orta";
    }

    public override void Write(System.Text.Json.Utf8JsonWriter writer, string value, System.Text.Json.JsonSerializerOptions options)
    {
        writer.WriteStringValue(value);
    }
}
