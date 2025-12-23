using System.Text.Json.Serialization;

namespace FrontendMvc.Models.Recipes;

public class RecipeViewModel
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    
    // Backend'den liste olarak geliyor, string'e çeviriyoruz
    [JsonPropertyName("ingredients")]
    [JsonConverter(typeof(IngredientsListConverter))]
    public string Ingredients { get; set; } = string.Empty;
    
    [JsonPropertyName("steps")]
    [JsonConverter(typeof(StepsListConverter))]
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
    
    public string? ImageUrl { get; set; } // Backward compatibility için (ana fotoğraf)
    public List<RecipeImageViewModel> Images { get; set; } = new(); // Çoklu fotoğraflar
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
    public int? AuthorId { get; set; }
    public AuthorViewModel? Author { get; set; }
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

// Ingredients listesini string'e çeviren converter
public class IngredientsListConverter : JsonConverter<string>
{
    public override string Read(ref System.Text.Json.Utf8JsonReader reader, Type typeToConvert, System.Text.Json.JsonSerializerOptions options)
    {
        if (reader.TokenType == System.Text.Json.JsonTokenType.StartArray)
        {
            var ingredients = new List<string>();
            while (reader.Read() && reader.TokenType != System.Text.Json.JsonTokenType.EndArray)
            {
                if (reader.TokenType == System.Text.Json.JsonTokenType.StartObject)
                {
                    string? name = null;
                    while (reader.Read() && reader.TokenType != System.Text.Json.JsonTokenType.EndObject)
                    {
                        if (reader.TokenType == System.Text.Json.JsonTokenType.PropertyName)
                        {
                            var propName = reader.GetString();
                            reader.Read();
                            if (propName == "name" && reader.TokenType == System.Text.Json.JsonTokenType.String)
                            {
                                name = reader.GetString();
                            }
                        }
                    }
                    if (!string.IsNullOrWhiteSpace(name))
                    {
                        ingredients.Add(name);
                    }
                }
                else if (reader.TokenType == System.Text.Json.JsonTokenType.String)
                {
                    var name = reader.GetString();
                    if (!string.IsNullOrWhiteSpace(name))
                    {
                        ingredients.Add(name);
                    }
                }
            }
            return string.Join("\n", ingredients);
        }
        else if (reader.TokenType == System.Text.Json.JsonTokenType.String)
        {
            // Backward compatibility: Eğer string olarak geliyorsa direkt döndür
            return reader.GetString() ?? string.Empty;
        }
        return string.Empty;
    }

    public override void Write(System.Text.Json.Utf8JsonWriter writer, string value, System.Text.Json.JsonSerializerOptions options)
    {
        writer.WriteStringValue(value);
    }
}

// Steps listesini string'e çeviren converter
public class StepsListConverter : JsonConverter<string>
{
    public override string Read(ref System.Text.Json.Utf8JsonReader reader, Type typeToConvert, System.Text.Json.JsonSerializerOptions options)
    {
        if (reader.TokenType == System.Text.Json.JsonTokenType.StartArray)
        {
            var steps = new List<string>();
            while (reader.Read() && reader.TokenType != System.Text.Json.JsonTokenType.EndArray)
            {
                if (reader.TokenType == System.Text.Json.JsonTokenType.StartObject)
                {
                    string? description = null;
                    while (reader.Read() && reader.TokenType != System.Text.Json.JsonTokenType.EndObject)
                    {
                        if (reader.TokenType == System.Text.Json.JsonTokenType.PropertyName)
                        {
                            var propName = reader.GetString();
                            reader.Read();
                            if (propName == "description" && reader.TokenType == System.Text.Json.JsonTokenType.String)
                            {
                                description = reader.GetString();
                            }
                        }
                    }
                    if (!string.IsNullOrWhiteSpace(description))
                    {
                        steps.Add(description);
                    }
                }
                else if (reader.TokenType == System.Text.Json.JsonTokenType.String)
                {
                    var description = reader.GetString();
                    if (!string.IsNullOrWhiteSpace(description))
                    {
                        steps.Add(description);
                    }
                }
            }
            return string.Join("\n", steps);
        }
        else if (reader.TokenType == System.Text.Json.JsonTokenType.String)
        {
            // Backward compatibility: Eğer string olarak geliyorsa direkt döndür
            return reader.GetString() ?? string.Empty;
        }
        return string.Empty;
    }

    public override void Write(System.Text.Json.Utf8JsonWriter writer, string value, System.Text.Json.JsonSerializerOptions options)
    {
        writer.WriteStringValue(value);
    }
}
