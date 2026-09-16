using App.DTOs;
using App.Exceptions;
using App.Models;
using App.Repositories;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.EntityFrameworkCore;

namespace App.Services;


public interface ICartItemService
{
    Task<CartItemResponseDto> CreateCartItemAsync(Guid userId, CartItemCreateDto itemCreateDto);
    Task<bool> DeleteItemFromCartAsync(Guid userId, Guid itemId);
    Task<List<CartItemResponseDto>> GetAllItemsInCartAsync(Guid userId);
}


public class CartItemService : ICartItemService
{
    private readonly ICartItemRepository _itemRepository;
    private readonly ICartRepository _cartRepository;
    private readonly ILogger<CartItemService> _logger;
    private readonly IHelperService _helper;

    public CartItemService(ICartItemRepository itemRepository, ICartRepository cartRepository, ILogger<CartItemService> logger, IHelperService helper)
    {
        _itemRepository = itemRepository;
        _cartRepository = cartRepository;
        _logger = logger;
        _helper = helper;
    }

    public async Task<CartItemResponseDto> CreateCartItemAsync(Guid userId, CartItemCreateDto itemCreateDto)
    {
        await _helper.GetUserOr404(userId);

        await _helper.GetProductOr404(itemCreateDto.ProductId);

        var cartId = await _cartRepository.GetCartIdByUserId(userId);

        if (cartId == null || cartId == Guid.Empty)
        {
            _logger.LogWarning("Cart not found by this id: {cartId}", cartId);
            throw new NotFoundException("Cart not found");
        }

        try
        {
            var newItem = new CartItem
            {
                CartId = cartId.Value,
                ProductId = itemCreateDto.ProductId,
                Quantity = itemCreateDto.Quantity,
            };

            await _itemRepository.CreateAsync(newItem);

            _logger.LogInformation("Cart item successfully created, cartID: {cartId}", cartId);

            return new CartItemResponseDto
            {
                CartId = newItem.CartId,
                ProductId = newItem.ProductId,
                Quantity = newItem.Quantity
            };
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "A database error occurred while creating the Cart item: {Message}.", ex.InnerException?.Message);
            throw new DatabaseException("Could not save the cart item to the database.");
        }
    }

    public async Task<bool> DeleteItemFromCartAsync(Guid userId, Guid itemId)
    {
        await _helper.GetUserOr404(userId);

        var cartId = await _cartRepository.GetCartIdByUserId(userId);

        if (cartId == null || cartId == Guid.Empty)
        {
            _logger.LogWarning("Cart not found by this id: {cartId}", cartId);
            throw new NotFoundException("Cart not found");
        }

        var cartItem = await _helper.GetCartItemOr404(itemId);
        
        if (cartItem.CartId != cartId)
        {
            _logger.LogWarning("Cart item does not belong to user, user ID: {userId}, Cart item ID: {itemId}", userId, itemId);
            throw new BadRequestException("Cart item does not belong to user");
        }

        await _itemRepository.DeleteAsync(cartItem);

        _logger.LogInformation("Item successfully delete from cart, cart ID: {cartId}, item ID: {itemId}", cartId, itemId);

        return true;
    }

    public async Task<List<CartItemResponseDto>> GetAllItemsInCartAsync(Guid userId)
    {
        await _helper.GetUserOr404(userId);

        var cartId = await _cartRepository.GetCartIdByUserId(userId);

        if (cartId == null || cartId == Guid.Empty)
        {
            _logger.LogWarning("Cart not found by this id: {cartId}", cartId);
            throw new NotFoundException("Cart not found");
        }

        var cartItems = await _itemRepository.GetAllItemsInCartAsyncByCartId(cartId.Value);

        return cartItems.Select(item => new CartItemResponseDto
        {
            CartId = item.CartId,
            ProductId = item.ProductId,
            Quantity = item.Quantity
        }).ToList();
    }
}

