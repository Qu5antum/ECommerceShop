using System.Data.Common;
using App.DTOs;
using App.Exceptions;
using App.Models;
using App.Repositories;
using App.Transactions;
using Microsoft.EntityFrameworkCore;

namespace App.Services;


public interface ICartItemService
{
    Task<CartItemResponseDto> CreateCartItemAsync(Guid userId, CartItemCreateDto itemCreateDto);
    Task<bool> UpdateCartItemAsync(Guid userId, Guid itemId, CartItemUpdateDto itemUpdateDto);
    Task<bool> DeleteItemFromCartAsync(Guid userId, Guid itemId);
    Task<List<CartItemResponseDto>> GetAllItemsInCartAsync(Guid userId);
    Task<CartItemResponseDto> GetItemInCartByIdAsync(Guid userId, Guid itemId);
}

public class CartItemService : ICartItemService
{
    private readonly ICartItemRepository _itemRepository;
    private readonly ICartRepository _cartRepository;
    private readonly ILogger<CartItemService> _logger;
    private readonly IHelperService _helper;
    private readonly IUnitOfWork _unitOfWork;

    public CartItemService(ICartItemRepository itemRepository, ICartRepository cartRepository, ILogger<CartItemService> logger, IHelperService helper, IUnitOfWork unitOfWork)
    {
        _itemRepository = itemRepository;
        _cartRepository = cartRepository;
        _logger = logger;
        _helper = helper;
        _unitOfWork = unitOfWork;
    }

    public async Task<CartItemResponseDto> CreateCartItemAsync(Guid userId, CartItemCreateDto itemCreateDto)
    {
        await _unitOfWork.BeginTransactionAsync();
        await _helper.GetUserOr404(userId);

        var product = await _helper.GetProductOr404(itemCreateDto.ProductId);

        var cartId = await _cartRepository.GetCartIdByUserId(userId);

        if (cartId == null || cartId == Guid.Empty)
        {
            _logger.LogWarning("Cart not found by this id: {cartId}", cartId);
            throw new NotFoundException("Cart not found");
        }

        var isProductExistsInCart = await _itemRepository.GetCartItemByProductId(product.Id);

        if (isProductExistsInCart)
        {
            _logger.LogWarning("Product already exists in cart, Product ID: {productId}", product.Id);
            throw new BadRequestException("Product already exists in cart");
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
            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitAsync();

            _logger.LogInformation("Cart item successfully created, cartID: {cartId}", cartId);

            return new CartItemResponseDto
            {
                Id = newItem.Id,
                CartId = newItem.CartId,
                ProductId = newItem.ProductId,
                Quantity = newItem.Quantity,
                CreatedAt = newItem.CreatedAt,
                UpdatedAt = newItem.UpdatedAt
            };
        }
        catch (DbUpdateException ex)
        {
            await _unitOfWork.RollbackAsync();
            _logger.LogError(ex, "A database error occurred while creating the Cart item: {Message}.", ex.InnerException?.Message);
            throw new DatabaseException("Could not save the cart item to the database.");
        }
    }

    public async Task<bool> UpdateCartItemAsync(Guid userId, Guid itemId, CartItemUpdateDto itemUpdateDto)
    {
        await _unitOfWork.BeginTransactionAsync();
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

        try
        {
            cartItem.Quantity = itemUpdateDto.Quantity;
            cartItem.UpdatedAt = DateTime.UtcNow;

            await _itemRepository.UpdateAsync(cartItem);
            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitAsync();

            _logger.LogInformation("Cart item successfully updated");

            return true;
        }
        catch (DbUpdateException ex)
        {
            await _unitOfWork.RollbackAsync();
            _logger.LogError(
                ex,
                "Database error while updating item in cart: cart item ID: {itemId} Inner exception: {Message}",
                itemId,
                ex.InnerException?.Message
            );

            throw new DatabaseException("Could not update the product to the database.");
        }
    }

    public async Task<bool> DeleteItemFromCartAsync(Guid userId, Guid itemId)
    {
        await _unitOfWork.BeginTransactionAsync();
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
        try
        {
            await _itemRepository.DeleteAsync(cartItem);
            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitAsync();

            _logger.LogInformation("Item successfully delete from cart, cart ID: {cartId}, item ID: {itemId}", cartId, itemId);

            return true;
        }
        catch (DbUpdateException ex)
        {
            await _unitOfWork.RollbackAsync();
            _logger.LogError(ex, "Database error while deleting item from cart: {Message}", ex.Message);
            throw new DatabaseException("Database error while deleting item from cart");
        }

        
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
            Id = item.Id,
            CartId = item.CartId,
            ProductId = item.ProductId,
            Quantity = item.Quantity,
            CreatedAt = item.CreatedAt,
            UpdatedAt = item.UpdatedAt
        }).ToList();
    }

    public async Task<CartItemResponseDto> GetItemInCartByIdAsync(Guid userId, Guid itemId)
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

        return new CartItemResponseDto
        {
            Id = cartItem.Id,
            CartId = cartItem.CartId,
            ProductId = cartItem.ProductId,
            Quantity = cartItem.Quantity,
            CreatedAt = cartItem.CreatedAt,
            UpdatedAt = cartItem.UpdatedAt
        };
    }
}

