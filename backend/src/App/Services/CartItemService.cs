using App.DTOs;
using App.Exceptions;
using App.Models;
using App.Repositories;
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
    private readonly IProductRepository _productRepository;
    private readonly ILogger<CartItemService> _logger;
    private readonly IHelperService _helper;

    public CartItemService(ICartItemRepository itemRepository, ICartRepository cartRepository, IProductRepository productRepository, ILogger<CartItemService> logger, IHelperService helper)
    {
        _itemRepository = itemRepository;
        _cartRepository = cartRepository;
        _productRepository = productRepository;
        _logger = logger;
        _helper = helper;
    }

    public async Task<CartItemResponseDto> CreateCartItemAsync(Guid userId, CartItemCreateDto itemCreateDto)
    {
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

        if (itemCreateDto.Quantity <= 0)
        {
            _logger.LogWarning("Quantity can't be negative or zero");
            throw new BadRequestException("Quantity can't be negative or zero");
        }

        if (product.Stock < itemCreateDto.Quantity)
        {
            _logger.LogWarning("Product stock is less than quantity, product ID: {productId}", itemCreateDto.ProductId);
            throw new BadRequestException("Product stock is less than quantity");
        }

        try
        {
            var newItem = new CartItem
            {
                CartId = cartId.Value,
                ProductId = itemCreateDto.ProductId,
                Quantity = itemCreateDto.Quantity,
            };

            product.Stock -= itemCreateDto.Quantity;

            await _productRepository.UpdateAsync(product);

            _logger.LogInformation("Product stock updated, product stock: {productId}", itemCreateDto.ProductId);

            await _itemRepository.CreateAsync(newItem);

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
            _logger.LogError(ex, "A database error occurred while creating the Cart item: {Message}.", ex.InnerException?.Message);
            throw new DatabaseException("Could not save the cart item to the database.");
        }
    }

    public async Task<bool> UpdateCartItemAsync(Guid userId, Guid itemId, CartItemUpdateDto itemUpdateDto)
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

        var product = await _helper.GetProductOr404(cartItem.ProductId);

        int totalAvaliableStock = product.Stock + cartItem.Quantity;

        if (totalAvaliableStock < itemUpdateDto.Quantity)
        {
            _logger.LogWarning("Product stock is less than requested quantity, product ID: {productId}", product.Id);
            throw new BadRequestException("Product stock is less than quantity");
        } 

        try{
            product.Stock = totalAvaliableStock - itemUpdateDto.Quantity;

            await _productRepository.UpdateAsync(product);

            _logger.LogInformation("Product stock is updated");

            cartItem.Quantity = itemUpdateDto.Quantity;

            await _itemRepository.UpdateAsync(cartItem);

            _logger.LogInformation("Cart item successfully updated");

            return true;
        }
        catch (DbUpdateException ex)
        {
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

        var product = await _helper.GetProductOr404(cartItem.ProductId);

        product.Stock += cartItem.Quantity;

        await _productRepository.UpdateAsync(product);

        _logger.LogInformation("Product stock updated, product stock: {productId}", cartItem.ProductId);

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

