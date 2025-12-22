namespace BackendApi.Application.Services;

public interface IAiIngredientService
{
    Task<List<string>> AdjustIngredientsAsync(List<string> ingredients, int originalServings, int newServings);
}

