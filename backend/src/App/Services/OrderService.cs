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
    Task<List<OrderReponseDto>> GetOrdersByUserIdAsync(Guid userId);
    Task<OrderReponseDto> GetOrderByUserIdAsync(Guid userId, Guid orderId);
    Task<bool> CancelOrderAsync(Guid userId, Guid orderId);
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
            await _unitOfWork.SaveChangesAsync();
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

    public async Task<List<OrderReponseDto>> GetOrdersByUserIdAsync(Guid userId)
    {
        await _helper.GetUserOr404(userId);

        var orders = await _orderRepository.GetOrdersByUserIdAsync(userId);

        _logger.LogInformation("Successfull response of orders: {userId}", userId);

        return orders.Select(order => new OrderReponseDto 
        {
            Id = order.Id,
            userId = order.userId,
            TotalAmount = order.TotalAmount,
            status = order.status,
            CreatedAt = order.CreatedAt,
            UpdatedAt = order.UpdatedAt,
            orderItems = order.orderItems.Select(item => new OrderItemResponseDto
            {
                Id = item.Id,              
                orderId = item.orderId,    
                productId = item.productId, 
                ProductName = item.ProductName,
                Price = item.Price,       
                Quantity = item.Quantity   
            }).ToList()
        }).ToList();
    }

    public async Task<OrderReponseDto> GetOrderByUserIdAsync(Guid userId, Guid orderId)
    {
        await _helper.GetUserOr404(userId);

        var order = await _orderRepository.GetOrderWithItemsById(orderId);

        if (order == null)
        {
            _logger.LogWarning("Order not found: {orderId}", orderId);
            throw new NotFoundException("Order not found");
        }

        if (order.userId != userId)
        {
            _logger.LogWarning("Order does not belong to user, order ID: {orderId}, user ID: {userId}", orderId, userId);
            throw new BadRequestException("Order does not belong to user");
        }

        _logger.LogInformation("Successfully retrieved order: {orderId}", orderId);

        return new OrderReponseDto
        {
            Id = order.Id,
            userId = order.userId,
            TotalAmount = order.TotalAmount,
            status = order.status,
            CreatedAt = order.CreatedAt,
            UpdatedAt = order.UpdatedAt,
            orderItems = order.orderItems.Select(item => new OrderItemResponseDto
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

    public async Task<bool> CancelOrderAsync(Guid userId, Guid orderId)
    {
        await _unitOfWork.BeginTransactionAsync();

        await _helper.GetUserOr404(userId);
        
        var order = await _orderRepository.GetOrderWithItemsById(orderId);

        if (order == null)
        {
            _logger.LogWarning("Order not found by this id: {orderId}", orderId);
            throw new NotFoundException("Order not found");
        }

        if (order.userId != userId)
        {
            _logger.LogWarning("Order does not belong to user, user ID: {userId}, order ID: {orderId}", userId, orderId);
            throw new BadRequestException("Order does not belong to user");
        }

        if (order.status < OrderStatus.Paid)
        {
            _logger.LogWarning("Cannot cancel order before Paid stage, order ID: {orderId}, status: {status}", orderId, order.status);
            throw new BadRequestException("Orders cannot be cancelled before the Paid stage.");
        }

        try
        {
            foreach (var item in order.orderItems)
            {
                var product = await _helper.GetProductOr404(item.productId);
                
                product.Stock += item.Quantity;

            }

            order.status = OrderStatus.Cancelled;
            order.UpdatedAt = DateTime.UtcNow;

            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitAsync();

            _logger.LogWarning("Order was cancelled, user ID: {userId}, order ID: {orderId}", userId, orderId);

            return true;
        }
        catch (DbUpdateException ex)
        {
            await _unitOfWork.RollbackAsync();
            _logger.LogError(ex, "A database error occurred while cancelling the order: {Message}.", ex.Message);
            throw new DatabaseException("Could not cancel the order to the database.");
        }
    }
}