using App.DTOs;
using App.Exceptions;
using App.Models;
using App.Repositories;
using App.Transactions;
using App.Enum;
using Microsoft.EntityFrameworkCore;

namespace App.Services;


public interface IOrderService
{
    Task<OrderReponseDto> CreateOrderAsync(Guid userId);
}


public class OrderService : IOrderService
{
    private readonly IOrderRepository _orderRepository;
    private readonly IOrderItemRepository _orderItemRepository;
    private readonly ICartRepository _cartRepository;
    private readonly IProductRepository _productRepository;
    private readonly ILogger<OrderService> _logger;
    private readonly IHelperService _helper;
    private readonly IUnitOfWork _unitOfWork;

    public OrderService(IOrderRepository orderRepository, IOrderItemRepository orderItemRepository, ICartRepository cartRepository, IProductRepository productRepository, ILogger<OrderService> logger, IHelperService helper, IUnitOfWork unitOfWork)
    {
        _orderRepository = orderRepository;
        _orderItemRepository = orderItemRepository;
        _cartRepository = cartRepository;
        _productRepository = productRepository;
        _logger = logger;
        _helper = helper;
        _unitOfWork = unitOfWork;
    }

    public async Task<OrderReponseDto> CreateOrderAsync(Guid userId)
    {
        await _unitOfWork.BeginTransactionAsync();

        await _helper.GetUserOr404(userId);

        var cartWithItems = await _cartRepository.GetCartWithItemsByUserIdAsync(userId);

        if (cartWithItems == null)
        {
            _logger.LogWarning("Cart not found by user: {userId}", userId);
            throw new NotFoundException("Cart not found");
        }

        if (cartWithItems.Items.Count == 0)
        {
            _logger.LogWarning("Cart can't be empty: {cartId}", cartWithItems.Id);
            throw new BadRequestException("Cart can't be empty");
        }

        List<Guid> productIds = new List<Guid>();

        foreach (var item in cartWithItems.Items)
        {
            productIds.Add(item.ProductId);
        }

        var products = await _productRepository.GetProductsByMultipleIds(productIds);

        if (products.Count() != productIds.Count())
        {
            _logger.LogWarning("Some products not found in list: {productsIds}", productIds);
            throw new NotFoundException("Some products not found in list");
        }

        var productDict = products.ToDictionary(p => p.Id);

        decimal totalAmount = 0;
        var orderItems = new List<OrderItem>();

        foreach (var item in cartWithItems.Items)
        {
            if (!productDict.TryGetValue(item.ProductId, out var product))
            {
                _logger.LogInformation("Product with ID not found: {productId}", item.ProductId);
                throw new NotFoundException("Product not found");
            }

            if (product.Stock < item.Quantity)
            {
                _logger.LogWarning($"Not enough stock for product {product.Name}. Available: {product.Stock}, requested: {item.Quantity}, Product id: {product.Id}");
                throw new BadRequestException($"Not enough stock for product {product.Name}. Available: {product.Stock}, requested: {item.Quantity}");
            }

            decimal itemTotal = product.Price * item.Quantity;
            totalAmount += itemTotal;

            orderItems.Add(new OrderItem
            {
                productId = product.Id,
                ProductName = product.Name,
                Price = product.Price,
                Quantity = item.Quantity
            });

            product.Stock -= item.Quantity;
        }

        try
        {
            var newOrder = new Order
            {
                userId = userId,
                TotalAmount = totalAmount,
                status = OrderStatus.Pending,
            };

            await _orderRepository.CreateAsync(newOrder);
            _logger.LogInformation("New order created: {orderId}", newOrder.Id);

            foreach (var item in orderItems)
            {
                item.orderId = newOrder.Id;
                await _orderItemRepository.CreateAsync(item);
                
                newOrder.orderItems.Add(item);
            }

            await _cartRepository.ClearCartAsync(cartWithItems.Id);
            
            await _unitOfWork.CommitAsync();

            return new OrderReponseDto
            {
                Id = newOrder.Id,
                userId = newOrder.userId,
                TotalAmount = newOrder.TotalAmount,
                status = newOrder.status,
                CreatedAt = newOrder.CreatedAt,
                UpdatedAt = newOrder.UpdatedAt,
                orderItems = newOrder.orderItems.Select(item => new OrderItemResponseDto
                {
                    Id = item.Id,               
                    orderId = item.orderId,     
                    productId = item.productId, 
                    ProductName = item.ProductName,
                    Price = item.Price,        
                    Quantity = item.Quantity   
                }).ToList()
            };
        }
        catch (DbUpdateException ex)
        {
            await _unitOfWork.RollbackAsync();
            _logger.LogError(ex, "A database error occurred while creating the order: {Message}.", ex.Message);
            throw new DatabaseException("Could not create the order to the database.");
        }
    }
}