using Moq;
using Microsoft.Extensions.Logging;

using App.Repositories;
using App.Services;
using App.Transactions;
using App.Services.Caching;
using App.Models;
using App.Exceptions;
using App.DTOs;
using App.Enum;
using System.Data.Common;
using FluentAssertions.Specialized;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;


public class OrderServiceTests
{
    private readonly Mock<IOrderRepository> _orderRepositoryMock;
    private readonly Mock<IOrderItemRepository> _orderItemRepositoryMock;
    private readonly Mock<ICartRepository> _cartRepositoryMock;
    private readonly Mock<IProductRepository> _productRepositoryMock;
    private readonly Mock<ISellerProfileRepository> _sellerProfileRepositoryMock;
    private readonly Mock<INotificationRepository> _notificationRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IRedisCacheService> _cacheMock; 
    private readonly Mock<ILogger<OrderService>> _loggerMock;
    private readonly Mock<IHelperService> _helper;
    private readonly OrderService _orderService;

    public OrderServiceTests()
    {
        _orderRepositoryMock = new Mock<IOrderRepository>();
        _orderItemRepositoryMock = new Mock<IOrderItemRepository>();
        _cartRepositoryMock = new Mock<ICartRepository>();
        _productRepositoryMock = new Mock<IProductRepository>();
        _sellerProfileRepositoryMock = new Mock<ISellerProfileRepository>();
        _notificationRepositoryMock = new Mock<INotificationRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _cacheMock = new Mock<IRedisCacheService>();
        _loggerMock = new Mock<ILogger<OrderService>>();
        _helper = new Mock<IHelperService>();

        _orderService = new OrderService(
            _orderRepositoryMock.Object,
            _orderItemRepositoryMock.Object,
            _cartRepositoryMock.Object,
            _productRepositoryMock.Object,
            _sellerProfileRepositoryMock.Object,
            _notificationRepositoryMock.Object,
            _loggerMock.Object,
            _helper.Object,
            _unitOfWorkMock.Object,
            _cacheMock.Object
        );
    }

    [Fact]
    public async Task CreateOrder_SuccessfullResponse_CreatingOrder()
    {
        var Id = Guid.NewGuid();
        var sellerId = Guid.NewGuid();
        var testUser = AuthTestHelper.CreateTestUser();
        var testProducts = new List<Product>
        {
            ProductTestHelper.CreateTestProduct(SellerProfileId: sellerId, Name: "TestProductName1", Price: 20),
            ProductTestHelper.CreateTestProduct(SellerProfileId: sellerId, Name: "TestProductName2", Price: 30)
        };

        var testCartWithItems = new Cart
        {
            Id = Id,
            UserId = testUser.Id,

            Items = new List<CartItem>
            {
                new CartItem
                {
                    CartId = Id,
                    ProductId = testProducts[0].Id,
                    Quantity = 5
                },
                new CartItem
                {
                    CartId = Id,
                    ProductId = testProducts[1].Id,
                    Quantity = 3
                }
            }
        };

        _helper.Setup(h => h.GetUserOr404(testUser.Id))
            .ReturnsAsync(testUser);

        _cartRepositoryMock.Setup(r => r.GetCartWithItemsByUserIdAsync(testUser.Id))
            .ReturnsAsync(testCartWithItems);

        _productRepositoryMock.Setup(r => r.GetObjectsByMultipleIdsAsync(It.IsAny<List<Guid>>()))
            .ReturnsAsync(testProducts);

        _sellerProfileRepositoryMock.Setup(r => r.GetUserIdBySellerProfileId(sellerId))
            .ReturnsAsync(sellerId);

        var result = await _orderService.CreateOrderAsync(testUser.Id);

        result.Should().NotBeNull();
        result.userId.Should().Be(testUser.Id);
        result.TotalAmount.Should().Be(190);
        result.orderItems.Should().HaveCount(2);

        _orderRepositoryMock.Verify(r => r.CreateAsync(It.IsAny<Order>()), Times.Once);

        _cartRepositoryMock.Verify(r => r.ClearCartAsync(It.IsAny<Guid>()), Times.Once);

        _notificationRepositoryMock.Verify(r => r.CreateAsync(It.Is<Notification>(n => n.UserId == testUser.Id)), Times.Once);
        _notificationRepositoryMock.Verify(r => r.CreateAsync(It.Is<Notification>(n => n.UserId == sellerId)), Times.Once);

        _unitOfWorkMock.Verify(u => u.BeginTransactionAsync(), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
        _unitOfWorkMock.Verify(u => u.CommitAsync(), Times.Once);

        _cacheMock.Verify(c => c.RemoveDataAsync(It.IsAny<string>()), Times.Exactly(16));
    }

    [Fact]
    public async Task CreateOrder_IfCartNotFound_ThrowsNotFoundException()
    {
        var testUser = AuthTestHelper.CreateTestUser();

        _helper.Setup(h => h.GetUserOr404(testUser.Id))
            .ReturnsAsync(testUser);

        _cartRepositoryMock.Setup(r => r.GetCartWithItemsByUserIdAsync(testUser.Id))
            .ReturnsAsync((Cart?)null);

        Func<Task> action = async () => await _orderService.CreateOrderAsync(testUser.Id);

        await action.Should().ThrowAsync<NotFoundException>()
            .WithMessage("Cart not found");

        _orderRepositoryMock.Verify(r => r.CreateAsync(It.IsAny<Order>()), Times.Never);

        _cartRepositoryMock.Verify(r => r.ClearCartAsync(It.IsAny<Guid>()), Times.Never);

        _notificationRepositoryMock.Verify(r => r.CreateAsync(It.Is<Notification>(n => n.UserId == testUser.Id)), Times.Never);

        _unitOfWorkMock.Verify(u => u.BeginTransactionAsync(), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
        _unitOfWorkMock.Verify(u => u.CommitAsync(), Times.Never);

        _cacheMock.Verify(c => c.RemoveDataAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task CreateOrder_IfCartIsEmpty_ThrowsBadRequestException()
    {
        var testUser = AuthTestHelper.CreateTestUser();

        var testCart = CartTestHelper.CreateTestHelper(UserId: testUser.Id);

        _helper.Setup(h => h.GetUserOr404(testUser.Id))
            .ReturnsAsync(testUser);

        _cartRepositoryMock.Setup(r => r.GetCartWithItemsByUserIdAsync(testUser.Id))
            .ReturnsAsync(testCart);

        Func<Task> action = async () => await _orderService.CreateOrderAsync(testUser.Id);

        await action.Should().ThrowAsync<BadRequestException>()
            .WithMessage("Cart can't be empty");

        _orderRepositoryMock.Verify(r => r.CreateAsync(It.IsAny<Order>()), Times.Never);

        _cartRepositoryMock.Verify(r => r.ClearCartAsync(It.IsAny<Guid>()), Times.Never);

        _notificationRepositoryMock.Verify(r => r.CreateAsync(It.Is<Notification>(n => n.UserId == testUser.Id)), Times.Never);

        _unitOfWorkMock.Verify(u => u.BeginTransactionAsync(), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
        _unitOfWorkMock.Verify(u => u.CommitAsync(), Times.Never);

        _cacheMock.Verify(c => c.RemoveDataAsync(It.IsAny<string>()), Times.Never);    
    }

    [Fact]
    public async Task CreateOrder_WhenStockNotEnough_ThrowsBadRequestException()
    {
        var Id = Guid.NewGuid();
        var sellerId = Guid.NewGuid();
        var testUser = AuthTestHelper.CreateTestUser();
        var testProducts = new List<Product>
        {
            ProductTestHelper.CreateTestProduct(SellerProfileId: sellerId, Name: "TestProductName1", Price: 20, Stock: 4),
            ProductTestHelper.CreateTestProduct(SellerProfileId: sellerId, Name: "TestProductName2", Price: 30, Stock: 10)
        };

        var testCartWithItems = new Cart
        {
            Id = Id,
            UserId = testUser.Id,

            Items = new List<CartItem>
            {
                new CartItem
                {
                    CartId = Id,
                    ProductId = testProducts[0].Id,
                    Quantity = 5
                },
                new CartItem
                {
                    CartId = Id,
                    ProductId = testProducts[1].Id,
                    Quantity = 3
                }
            }
        };

        _helper.Setup(h => h.GetUserOr404(testUser.Id))
            .ReturnsAsync(testUser);

        _cartRepositoryMock.Setup(r => r.GetCartWithItemsByUserIdAsync(testUser.Id))
            .ReturnsAsync(testCartWithItems);

        _productRepositoryMock.Setup(r => r.GetObjectsByMultipleIdsAsync(It.IsAny<List<Guid>>()))
            .ReturnsAsync(testProducts);

        _sellerProfileRepositoryMock.Setup(r => r.GetUserIdBySellerProfileId(sellerId))
            .ReturnsAsync(sellerId);
        
        Func<Task> action = async () => await _orderService.CreateOrderAsync(testUser.Id);

        await action.Should().ThrowAsync<BadRequestException>()
            .WithMessage($"Not enough stock for product {testProducts[0].Name}. Available: {testProducts[0].Stock}, requested: {testCartWithItems.Items[0].Quantity}");

        _orderRepositoryMock.Verify(r => r.CreateAsync(It.IsAny<Order>()), Times.Never);

        _cartRepositoryMock.Verify(r => r.ClearCartAsync(It.IsAny<Guid>()), Times.Never);

        _notificationRepositoryMock.Verify(r => r.CreateAsync(It.Is<Notification>(n => n.UserId == testUser.Id)), Times.Never);

        _unitOfWorkMock.Verify(u => u.BeginTransactionAsync(), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
        _unitOfWorkMock.Verify(u => u.CommitAsync(), Times.Never);

        _cacheMock.Verify(c => c.RemoveDataAsync(It.IsAny<string>()), Times.Never); 
    }

    [Fact]
    public async Task GetOrdersByUserIdAsync_WhenDataIsInCache_ReturnsCachedOrdersWithoutHittingRepository()
    {
        var userId = Guid.NewGuid();
        var testUser = AuthTestHelper.CreateTestUser(Id: userId);

        var cachedOrders = new List<OrderResponseDto>
        {
            new OrderResponseDto
            {
                Id = Guid.NewGuid(),
                userId = userId,
                TotalAmount = 500,
                status = OrderStatus.Pending,
                CreatedAt = DateTime.UtcNow,
                orderItems = new List<OrderItemResponseDto>
                {
                    new OrderItemResponseDto
                    {
                        Id = Guid.NewGuid(),
                        productId = Guid.NewGuid(),
                        ProductName = "TestProductName",
                        Price = 500,
                        Quantity = 5
                    }
                }
            }
        };

        _helper.Setup(h => h.GetUserOr404(userId))
            .ReturnsAsync(testUser);

        _cacheMock.Setup(c => c.GetDataAsync<List<OrderResponseDto>>($"orders:{userId}"))
            .ReturnsAsync(cachedOrders);

        var result = await _orderService.GetOrdersByUserIdAsync(userId);

        result.Should().NotBeNull();
        result.Should().BeEquivalentTo(cachedOrders);

        _helper.Verify(h => h.GetUserOr404(userId), Times.Once);
        _orderRepositoryMock.Verify(r => r.GetOrdersByUserIdAsync(It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<int>()), Times.Never);
        _cacheMock.Verify(c => c.SetDataAsync(It.IsAny<string>(), It.IsAny<List<OrderResponseDto>>(), It.IsAny<TimeSpan>()), Times.Never);
    }

    [Fact]
    public async Task GetOrdersByUserIdAsync_WhenCacheMiss_FetchesFromRepositoryAndCaches()
    {
        var userId = Guid.NewGuid();
        var testUser = AuthTestHelper.CreateTestUser(Id: userId);

        var expectedOrders = new List<OrderResponseDto>
        {
            new OrderResponseDto
            {
                Id = Guid.NewGuid(),
                userId = userId,
                TotalAmount = 2500,
                status = OrderStatus.Paid,
                CreatedAt = DateTime.UtcNow,
                orderItems = new List<OrderItemResponseDto>
                {
                    new OrderItemResponseDto
                    {
                        Id = Guid.NewGuid(),
                        productId = Guid.NewGuid(),
                        ProductName = "Laptop",
                        Price = 500,
                        Quantity = 5
                    }
                }
            }
        };

        _helper.Setup(h => h.GetUserOr404(userId))
            .ReturnsAsync(testUser);

        _cacheMock.Setup(c => c.GetDataAsync<List<OrderResponseDto>>($"orders:{userId}"))
            .ReturnsAsync((List<OrderResponseDto>?)null);

        _orderRepositoryMock.Setup(r => r.GetOrdersByUserIdAsync(userId))
            .ReturnsAsync(expectedOrders);

        var result = await _orderService.GetOrdersByUserIdAsync(userId);

        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        result.Should().BeEquivalentTo(expectedOrders);

        _orderRepositoryMock.Verify(r => r.GetOrdersByUserIdAsync(userId), Times.Once);

        _cacheMock.Verify(c => c.SetDataAsync(
            $"orders:{userId}",
            It.Is<List<OrderResponseDto>>(list => list.Count == 1),
            TimeSpan.FromMinutes(5)
        ), Times.Once);
    }

    [Fact]
    public async Task GetOrderByUserIdAsync_WhenDataIsInCache_ReturnsCachedOrderWithoutHittingRepository()
    {
        var userId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var testUser = AuthTestHelper.CreateTestUser(Id: userId);

        var cachedOrder = new OrderResponseDto
        {
            Id = orderId,
            userId = userId,
            TotalAmount = 500,
            status = OrderStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            orderItems = new List<OrderItemResponseDto>()
        };

        _helper.Setup(h => h.GetUserOr404(userId))
            .ReturnsAsync(testUser);

        _cacheMock.Setup(c => c.GetDataAsync<OrderResponseDto>($"order:{userId}:{orderId}"))
            .ReturnsAsync(cachedOrder);

        var result = await _orderService.GetOrderByUserIdAsync(userId, orderId);

        result.Should().NotBeNull();
        result.Should().BeEquivalentTo(cachedOrder);

        _helper.Verify(h => h.GetUserOr404(userId), Times.Once);
        _orderRepositoryMock.Verify(r => r.GetOrderWithItemsById(It.IsAny<Guid>()), Times.Never);
        _cacheMock.Verify(c => c.SetDataAsync(It.IsAny<string>(), It.IsAny<OrderResponseDto>(), It.IsAny<TimeSpan>()), Times.Never);
    }

    [Fact]
    public async Task GetOrderByUserIdAsync_WhenCacheMissAndValidUser_FetchesFromRepositoryMapsAndCaches()
    {
        var userId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var testUser = AuthTestHelper.CreateTestUser(Id: userId);

        var testOrder = OrderTestHelper.CreateTestOrder(Id: orderId, userId: userId, TotalAmount: 1000, status: OrderStatus.Paid);
        var testOrderItem = OrderItemTestHelper.CreateTestOrderItem(orderId: orderId, ProductName: "Phone", Price: 500, Quantity: 2);
        testOrder.orderItems = new List<OrderItem> { testOrderItem };

        _helper.Setup(h => h.GetUserOr404(userId))
            .ReturnsAsync(testUser);

        _cacheMock.Setup(c => c.GetDataAsync<OrderResponseDto>($"order:{userId}:{orderId}"))
            .ReturnsAsync((OrderResponseDto?)null);

        _orderRepositoryMock.Setup(r => r.GetOrderWithItemsById(orderId))
            .ReturnsAsync(testOrder);

        var result = await _orderService.GetOrderByUserIdAsync(userId, orderId);

        result.Should().NotBeNull();
        result.Id.Should().Be(orderId);
        result.userId.Should().Be(userId);
        result.TotalAmount.Should().Be(1000);
        result.orderItems.Should().HaveCount(1);
        result.orderItems.First().ProductName.Should().Be("Phone");

        _orderRepositoryMock.Verify(r => r.GetOrderWithItemsById(orderId), Times.Once);
        _cacheMock.Verify(c => c.SetDataAsync(
            $"order:{userId}:{orderId}",
            It.Is<OrderResponseDto>(dto => dto.Id == orderId),
            TimeSpan.FromMinutes(5)
        ), Times.Once);
    }

    [Fact]
    public async Task GetOrderByUserIdAsync_WhenOrderNotFound_ThrowsNotFoundException()
    {
        var userId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var testUser = AuthTestHelper.CreateTestUser(Id: userId);

        _helper.Setup(h => h.GetUserOr404(userId))
            .ReturnsAsync(testUser);

        _cacheMock.Setup(c => c.GetDataAsync<OrderResponseDto>($"order:{userId}:{orderId}"))
            .ReturnsAsync((OrderResponseDto?)null);

        _orderRepositoryMock.Setup(r => r.GetOrderWithItemsById(orderId))
            .ReturnsAsync((Order?)null);

        Func<Task> action = async () => await _orderService.GetOrderByUserIdAsync(userId, orderId);

        await action.Should().ThrowAsync<NotFoundException>()
            .WithMessage("Order not found");

        _cacheMock.Verify(c => c.SetDataAsync(It.IsAny<string>(), It.IsAny<OrderResponseDto>(), It.IsAny<TimeSpan>()), Times.Never);
    }

    [Fact]
    public async Task GetOrderByUserIdAsync_WhenOrderDoesNotBelongToUser_ThrowsBadRequestException()
    {
        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var testUser = AuthTestHelper.CreateTestUser(Id: userId);

        var foreignOrder = OrderTestHelper.CreateTestOrder(Id: orderId, userId: otherUserId);

        _helper.Setup(h => h.GetUserOr404(userId))
            .ReturnsAsync(testUser);

        _cacheMock.Setup(c => c.GetDataAsync<OrderResponseDto>($"order:{userId}:{orderId}"))
            .ReturnsAsync((OrderResponseDto?)null);

        _orderRepositoryMock.Setup(r => r.GetOrderWithItemsById(orderId))
            .ReturnsAsync(foreignOrder);

        Func<Task> action = async () => await _orderService.GetOrderByUserIdAsync(userId, orderId);

        await action.Should().ThrowAsync<BadRequestException>()
            .WithMessage("Order does not belong to user");

        _cacheMock.Verify(c => c.SetDataAsync(It.IsAny<string>(), It.IsAny<OrderResponseDto>(), It.IsAny<TimeSpan>()), Times.Never);
    }

    [Fact]
    public async Task CancelOrder_SuccessfullResponse_ReturnsTrue()
    {
        var sellerProfileId = Guid.NewGuid();
        var sellerUserId = Guid.NewGuid();
        var testUser = AuthTestHelper.CreateTestUser();

        var testProduct = ProductTestHelper.CreateTestProduct(
            SellerProfileId: sellerProfileId,
            Stock: 10
        );
        var testProducts = new List<Product> { testProduct };

        var testOrder = OrderTestHelper.CreateTestOrder(userId: testUser.Id);

        var testOrderItem = OrderItemTestHelper.CreateTestOrderItem(
            orderId: testOrder.Id,
            productId: testProduct.Id,
            ProductName: testProduct.Name,
            Price: testProduct.Price,
            Quantity: 2
        );

        testOrder.orderItems = new List<OrderItem> { testOrderItem };

        _helper.Setup(h => h.GetUserOr404(testUser.Id))
            .ReturnsAsync(testUser);

        _orderRepositoryMock.Setup(r => r.GetOrderWithItemsById(testOrder.Id))
            .ReturnsAsync(testOrder);

        _productRepositoryMock.Setup(r => r.GetObjectsByMultipleIdsAsync(It.IsAny<List<Guid>>()))
            .ReturnsAsync(testProducts);

        _sellerProfileRepositoryMock.Setup(r => r.GetUserIdBySellerProfileId(sellerProfileId))
            .ReturnsAsync(sellerUserId);

        var result = await _orderService.CancelOrderAsync(testUser.Id, testOrder.Id);

        result.Should().BeTrue();
        testOrder.status.Should().Be(OrderStatus.Cancelled);
        testProduct.Stock.Should().Be(12); 

        _notificationRepositoryMock.Verify(r => r.CreateAsync(It.IsAny<Notification>()), Times.Exactly(2));

        _unitOfWorkMock.Verify(u => u.BeginTransactionAsync(), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
        _unitOfWorkMock.Verify(u => u.CommitAsync(), Times.Once);

        _cacheMock.Verify(c => c.RemoveDataAsync(It.IsAny<string>()), Times.Exactly(5));
    }

    [Fact]
    public async Task CancelOrder_WhenOrderNotFound_ThrowsNotFoundException()
    {
        var userId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var testUser = AuthTestHelper.CreateTestUser(Id: userId);

        _helper.Setup(h => h.GetUserOr404(userId))
            .ReturnsAsync(testUser);

        _orderRepositoryMock.Setup(r => r.GetOrderWithItemsById(orderId))
            .ReturnsAsync((Order?)null);

        Func<Task> action = async () => await _orderService.CancelOrderAsync(userId, orderId);

        await action.Should().ThrowAsync<NotFoundException>()
            .WithMessage("Order not found");

        _unitOfWorkMock.Verify(u => u.BeginTransactionAsync(), Times.Never);
        _cacheMock.Verify(c => c.RemoveDataAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task CancelOrder_WhenOrderDoesNotBelongToUser_ThrowsBadRequestException()
    {
        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var testUser = AuthTestHelper.CreateTestUser(Id: userId);
        var foreignOrder = OrderTestHelper.CreateTestOrder(Id: orderId, userId: otherUserId);

        _helper.Setup(h => h.GetUserOr404(userId))
            .ReturnsAsync(testUser);

        _orderRepositoryMock.Setup(r => r.GetOrderWithItemsById(orderId))
            .ReturnsAsync(foreignOrder);

        Func<Task> action = async () => await _orderService.CancelOrderAsync(userId, orderId);

        await action.Should().ThrowAsync<BadRequestException>()
            .WithMessage("Order does not belong to user");

        _unitOfWorkMock.Verify(u => u.BeginTransactionAsync(), Times.Never);
    }

    [Fact]
    public async Task CancelOrder_WhenStatusIsGreaterThanPaid_ThrowsBadRequestException()
    {
        var userId = Guid.NewGuid();
        var testUser = AuthTestHelper.CreateTestUser(Id: userId);
        
        var testOrder = OrderTestHelper.CreateTestOrder(userId: userId, status: OrderStatus.Shipped);

        _helper.Setup(h => h.GetUserOr404(userId))
            .ReturnsAsync(testUser);

        _orderRepositoryMock.Setup(r => r.GetOrderWithItemsById(testOrder.Id))
            .ReturnsAsync(testOrder);

        Func<Task> action = async () => await _orderService.CancelOrderAsync(userId, testOrder.Id);

        await action.Should().ThrowAsync<BadRequestException>()
            .WithMessage("Orders cannot be cancelled before the Paid stage.");

        _unitOfWorkMock.Verify(u => u.BeginTransactionAsync(), Times.Never);
    }

    [Fact]
    public async Task CancelOrder_WhenSomeProductsAreMissing_ThrowsNotFoundException()
    {
        var userId = Guid.NewGuid();
        var testUser = AuthTestHelper.CreateTestUser(Id: userId);
        var testOrder = OrderTestHelper.CreateTestOrder(userId: userId, status: OrderStatus.Paid);

        var item1 = OrderItemTestHelper.CreateTestOrderItem(orderId: testOrder.Id);
        var item2 = OrderItemTestHelper.CreateTestOrderItem(orderId: testOrder.Id);
        testOrder.orderItems = new List<OrderItem> { item1, item2 };

        var dbProducts = new List<Product> { ProductTestHelper.CreateTestProduct(Id: item1.productId) };

        _helper.Setup(h => h.GetUserOr404(userId))
            .ReturnsAsync(testUser);

        _orderRepositoryMock.Setup(r => r.GetOrderWithItemsById(testOrder.Id))
            .ReturnsAsync(testOrder);

        _productRepositoryMock.Setup(r => r.GetObjectsByMultipleIdsAsync(It.IsAny<List<Guid>>()))
            .ReturnsAsync(dbProducts);

        Func<Task> action = async () => await _orderService.CancelOrderAsync(userId, testOrder.Id);

        await action.Should().ThrowAsync<NotFoundException>()
            .WithMessage("Some products are missing");

        _unitOfWorkMock.Verify(u => u.BeginTransactionAsync(), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task CancelOrder_WhenSellerUserIdNotFound_ThrowsNotFoundException()
    {
        var userId = Guid.NewGuid();
        var sellerProfileId = Guid.NewGuid();
        var testUser = AuthTestHelper.CreateTestUser(Id: userId);
        var testProduct = ProductTestHelper.CreateTestProduct(SellerProfileId: sellerProfileId);
        var testOrder = OrderTestHelper.CreateTestOrder(userId: userId, status: OrderStatus.Pending);
        var item = OrderItemTestHelper.CreateTestOrderItem(orderId: testOrder.Id, productId: testProduct.Id);
        
        testOrder.orderItems = new List<OrderItem> { item };

        _helper.Setup(h => h.GetUserOr404(userId))
            .ReturnsAsync(testUser);

        _orderRepositoryMock.Setup(r => r.GetOrderWithItemsById(testOrder.Id))
            .ReturnsAsync(testOrder);

        _productRepositoryMock.Setup(r => r.GetObjectsByMultipleIdsAsync(It.IsAny<List<Guid>>()))
            .ReturnsAsync(new List<Product> { testProduct });

        _sellerProfileRepositoryMock.Setup(r => r.GetUserIdBySellerProfileId(sellerProfileId))
            .ReturnsAsync(Guid.Empty);

        Func<Task> action = async () => await _orderService.CancelOrderAsync(userId, testOrder.Id);

        await action.Should().ThrowAsync<NotFoundException>()
            .WithMessage("User not found by seller");

        _unitOfWorkMock.Verify(u => u.BeginTransactionAsync(), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task CancelOrder_WhenDbUpdateExceptionOccurs_RollsBackAndThrowsDatabaseException()
    {
        var userId = Guid.NewGuid();
        var sellerProfileId = Guid.NewGuid();
        var sellerUserId = Guid.NewGuid();
        var testUser = AuthTestHelper.CreateTestUser(Id: userId);
        var testProduct = ProductTestHelper.CreateTestProduct(SellerProfileId: sellerProfileId);
        var testOrder = OrderTestHelper.CreateTestOrder(userId: userId, status: OrderStatus.Paid);
        var item = OrderItemTestHelper.CreateTestOrderItem(orderId: testOrder.Id, productId: testProduct.Id);

        testOrder.orderItems = new List<OrderItem> { item };

        _helper.Setup(h => h.GetUserOr404(userId))
            .ReturnsAsync(testUser);

        _orderRepositoryMock.Setup(r => r.GetOrderWithItemsById(testOrder.Id))
            .ReturnsAsync(testOrder);

        _productRepositoryMock.Setup(r => r.GetObjectsByMultipleIdsAsync(It.IsAny<List<Guid>>()))
            .ReturnsAsync(new List<Product> { testProduct });

        _sellerProfileRepositoryMock.Setup(r => r.GetUserIdBySellerProfileId(sellerProfileId))
            .ReturnsAsync(sellerUserId);

        _unitOfWorkMock.Setup(u => u.SaveChangesAsync())
            .ThrowsAsync(new DbUpdateException("Database error", new Exception()));

        Func<Task> action = async () => await _orderService.CancelOrderAsync(userId, testOrder.Id);

        await action.Should().ThrowAsync<DatabaseException>()
            .WithMessage("Could not cancel the order to the database.");

        _unitOfWorkMock.Verify(u => u.RollbackAsync(), Times.Once);
        _unitOfWorkMock.Verify(u => u.CommitAsync(), Times.Never);
    }
}