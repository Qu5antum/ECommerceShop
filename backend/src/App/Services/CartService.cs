using App.DTOs;
using App.Models;
using App.Repositories;

namespace App.Services;


public interface ICartService
{
    Task<CartResponseDto> GetCartAsync(Guid userId);
}


public class CartService : ICartService
{
    private readonly ICartRepository _repository;
    private readonly ILogger<CartService> _logger;

    public CartService(ICartRepository repository, ILogger<CartService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

     public async Task<CartResponseDto> GetCartAsync(Guid userId)
    {
        var cart = await _repository.GetCartWithItemsByUserIdAsync(userId);

        if (cart == null)
        {
            cart = new Cart
            {
                UserId = userId
            };

            await _repository.CreateAsync(cart);

            _logger.LogInformation("Cart created for user {UserId}", userId);
        }

        return new CartResponseDto
        {
            UserId = cart.UserId,
            Items = cart.Items.Select(item => new CartItemResponseDto
            {
                CartId = item.Id,
                ProductId = item.ProductId,
                Quantity = item.Quantity
            }).ToList()
        };
    }
}