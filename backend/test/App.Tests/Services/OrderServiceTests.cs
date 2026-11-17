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
}