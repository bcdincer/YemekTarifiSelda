using BackendApi.Application.DTOs;
using BackendApi.Domain.Entities;

namespace BackendApi.Application.Mappings;

public static class ShoppingListMapper
{
    public static ShoppingListResponseDto ToDto(this ShoppingList shoppingList)
    {
        return new ShoppingListResponseDto
        {
            Id = shoppingList.Id,
            UserId = shoppingList.UserId,
            Name = shoppingList.Name,
            MealPlanId = shoppingList.MealPlanId,
            IsCompleted = shoppingList.IsCompleted,
            CreatedAt = shoppingList.CreatedAt,
            UpdatedAt = shoppingList.UpdatedAt,
            CompletedAt = shoppingList.CompletedAt,
            Items = shoppingList.Items.Select(i => i.ToDto()).ToList()
        };
    }

    public static ShoppingListItemResponseDto ToDto(this ShoppingListItem item)
    {
        return new ShoppingListItemResponseDto
        {
            Id = item.Id,
            Ingredient = item.Ingredient,
            Quantity = item.Quantity,
            Unit = item.Unit,
            IsChecked = item.IsChecked,
            DisplayOrder = item.DisplayOrder
        };
    }
}

