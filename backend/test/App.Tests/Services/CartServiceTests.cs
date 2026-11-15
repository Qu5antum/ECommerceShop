using Moq;
using Microsoft.Extensions.Logging;

using App.Repositories;
using App.Services;
using App.Transactions;
using App.Services.Caching;
using App.Models;
using App.Exceptions;
using App.DTOs;


public class CartServiceTests
{
    private readonly Mock<ICartItemRepository> _cartItemRepositoryMock;
    private readonly Mock<ICartRepository> _cartRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IRedisCacheService> _cacheMock; 
    private readonly Mock<ILogger<CartItemService>> _loggerMock;
    private readonly Mock<IHelperService> _helper;
    private readonly CartItemService _cartItemService;

    public CartServiceTests()
    {
        _cartItemRepositoryMock = new Mock<ICartItemRepository>();
        _cartRepositoryMock = new Mock<ICartRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _cacheMock = new Mock<IRedisCacheService>();
        _loggerMock = new Mock<ILogger<CartItemService>>();
        _helper = new Mock<IHelperService>();

        _cartItemService = new CartItemService(
            _cartItemRepositoryMock.Object,
            _cartRepositoryMock.Object,
            _loggerMock.Object,
            _helper.Object,
            _cacheMock.Object,
            _unitOfWorkMock.Object
        );
    }

    [Fact]
    public async Task CreateCartItem_FindProductSaveItem_SaveinDb()
    {
        var testUser = AuthTestHelper.CreateTestUser();
        var testProduct = ProductTestHelper.CreateTestProduct(Stock: 10);

        var cartItemDto = new CartItemCreateDto
        {
            ProductId = testProduct.Id,
            Quantity = 5
        };

        _helper.Setup(h => h.GetUserOr404(testUser.Id))
            .ReturnsAsync(testUser);

        _helper.Setup(h => h.GetProductOr404(testProduct.Id))
            .ReturnsAsync(testProduct);

        _cartRepositoryMock.Setup(r => r.GetCartWithItemsByUserIdAsync(testUser.Id))
            .ReturnsAsync((Cart?)null);
        
        _cartItemRepositoryMock.Setup(r => r.GetCartItemByProductId(testProduct.Id))
            .ReturnsAsync(false);

        var result = await _cartItemService.CreateCartItemAsync(testUser.Id, cartItemDto);

        result.Should().NotBeNull();
        result.ProductId.Should().Be(testProduct.Id);
        result.Quantity.Should().Be(cartItemDto.Quantity);

        _cartItemRepositoryMock.Verify(r => r.CreateAsync(It.IsAny<CartItem>()), Times.Once);

        _unitOfWorkMock.Verify(u => u.BeginTransactionAsync(), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
        _unitOfWorkMock.Verify(u => u.CommitAsync(), Times.Once);

        _cacheMock.Verify(c => c.RemoveDataAsync(It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task CreateCartItem_IfProductAlreadyExistsInCart_ThrowsBadRequestException()
    {
        var testUser = AuthTestHelper.CreateTestUser();
        var testProduct = ProductTestHelper.CreateTestProduct(Stock: 10);

        var testCart = CartTestHelper.CreateTestHelper(UserId: testUser.Id);
        var testCartItem = CartItemTestHelper.CreateTestCartItem(CartId: testCart.Id, ProductId: testProduct.Id);

        var cartItemDto = new CartItemCreateDto
        {
            ProductId = testProduct.Id,
            Quantity = 5
        };

        _helper.Setup(h => h.GetUserOr404(testUser.Id))
            .ReturnsAsync(testUser);

        _helper.Setup(h => h.GetProductOr404(testProduct.Id))
            .ReturnsAsync(testProduct);

        _cartRepositoryMock.Setup(r => r.GetCartWithItemsByUserIdAsync(testUser.Id))
            .ReturnsAsync(testCart);

        _cartItemRepositoryMock.Setup(r => r.GetCartItemByProductId(testProduct.Id))
            .ReturnsAsync(true);

        Func<Task> action = async () => await _cartItemService.CreateCartItemAsync(testUser.Id, cartItemDto);

        await action.Should().ThrowAsync<BadRequestException>()
            .WithMessage("Product already exists in cart");

        _cartItemRepositoryMock.Verify(r => r.CreateAsync(It.IsAny<CartItem>()), Times.Never);

        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);

        _cacheMock.Verify(c => c.RemoveDataAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task CreateCartItem_WhenProductBelongsToDifferentSeller_ThrowsBadRequestException()
    {
        var testUser = AuthTestHelper.CreateTestUser();
        
        var sellerAId = Guid.NewGuid();
        var sellerBId = Guid.NewGuid();

        var existingProduct = ProductTestHelper.CreateTestProduct(SellerProfileId: sellerAId);
        
        var newProduct = ProductTestHelper.CreateTestProduct(SellerProfileId: sellerBId);

        var testCart = CartTestHelper.CreateTestHelper(UserId: testUser.Id);
        testCart.Items = new List<CartItem>
        {
            new CartItem { ProductId = existingProduct.Id, CartId = testCart.Id }
        };

        var cartItemDto = new CartItemCreateDto
        {
            ProductId = newProduct.Id,
            Quantity = 1
        };

        _helper.Setup(h => h.GetUserOr404(testUser.Id))
            .ReturnsAsync(testUser);

        _helper.Setup(h => h.GetProductOr404(newProduct.Id))
            .ReturnsAsync(newProduct);

        _helper.Setup(h => h.GetProductOr404(existingProduct.Id))
            .ReturnsAsync(existingProduct);

        _cartRepositoryMock.Setup(r => r.GetCartWithItemsByUserIdAsync(testUser.Id))
            .ReturnsAsync(testCart);

        _cartItemRepositoryMock.Setup(r => r.GetCartItemByProductId(newProduct.Id))
            .ReturnsAsync(false);

        Func<Task> action = async () => await _cartItemService.CreateCartItemAsync(testUser.Id, cartItemDto);

        await action.Should().ThrowAsync<BadRequestException>()
            .WithMessage("You can only add items from the same seller to the cart. Please clear your cart first.");

        _cartItemRepositoryMock.Verify(r => r.CreateAsync(It.IsAny<CartItem>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
        _cacheMock.Verify(c => c.RemoveDataAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task UpdateCartItem_SuccessfullUpdating_ReturnsOk()
    {
        var testUser = AuthTestHelper.CreateTestUser();

        var testCart = CartTestHelper.CreateTestHelper(UserId: testUser.Id);
        var testCartItem = CartItemTestHelper.CreateTestCartItem(CartId: testCart.Id);

        var cartItemUpdateDto = new CartItemUpdateDto
        {
            Quantity = 10
        };

        _helper.Setup(h => h.GetUserOr404(testUser.Id))
            .ReturnsAsync(testUser);

        _cartRepositoryMock.Setup(r => r.GetCartIdByUserId(testUser.Id))
            .ReturnsAsync(testCart.Id);

        _helper.Setup(h => h.GetCartItemOr404(testCartItem.Id))
            .ReturnsAsync(testCartItem);

        var result = await _cartItemService.UpdateCartItemAsync(
            testUser.Id, 
            testCartItem.Id, 
            cartItemUpdateDto
        );

        result.Should().BeTrue();
        
        _cartItemRepositoryMock.Verify(r => r.UpdateAsync(testCartItem), Times.Once);

        _unitOfWorkMock.Verify(u => u.BeginTransactionAsync(), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
        _unitOfWorkMock.Verify(u => u.CommitAsync(), Times.Once);

        _cacheMock.Verify(c => c.RemoveDataAsync($"cartItem:{testCartItem.Id}"), Times.Once);
    }

    [Fact]
    public async Task UpdateCartItem_CheckCartDoesNotBelongToUser_ThrowsBadRequestException()
    {
        var testUser = AuthTestHelper.CreateTestUser();

        var testCart = CartTestHelper.CreateTestHelper(UserId: testUser.Id);
        var testCartItem = CartItemTestHelper.CreateTestCartItem(CartId: Guid.NewGuid());

        var cartItemUpdateDto = new CartItemUpdateDto
        {
            Quantity = 10
        };

        _helper.Setup(h => h.GetUserOr404(testUser.Id))
            .ReturnsAsync(testUser);

        _cartRepositoryMock.Setup(r => r.GetCartIdByUserId(testUser.Id))
            .ReturnsAsync(testCart.Id);

        _helper.Setup(h => h.GetCartItemOr404(testCartItem.Id))
            .ReturnsAsync(testCartItem);

        Func<Task> action = async () => await _cartItemService.UpdateCartItemAsync(
            testUser.Id, 
            testCartItem.Id, 
            cartItemUpdateDto
        );

        await action.Should().ThrowAsync<BadRequestException>()
            .WithMessage("Cart item does not belong to user");

        _cartItemRepositoryMock.Verify(r => r.UpdateAsync(testCartItem), Times.Never);

        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);

        _cacheMock.Verify(c => c.RemoveDataAsync($"cartItem:{testCartItem.Id}"), Times.Never);
    }

    [Fact]
    public async Task DeleteItemFromCart_SuccessfullDeletingItem_ReturnsOk()
    {
        var testUser = AuthTestHelper.CreateTestUser();

        var testCart = CartTestHelper.CreateTestHelper(UserId: testUser.Id);
        var testCartItem = CartItemTestHelper.CreateTestCartItem(CartId: testCart.Id);

        _helper.Setup(h => h.GetUserOr404(testUser.Id))
            .ReturnsAsync(testUser);

        _cartRepositoryMock.Setup(r => r.GetCartIdByUserId(testUser.Id))
            .ReturnsAsync(testCart.Id);

        _helper.Setup(h => h.GetCartItemOr404(testCartItem.Id))
            .ReturnsAsync(testCartItem);

        var result = await _cartItemService.DeleteItemFromCartAsync(
            testUser.Id, 
            testCartItem.Id
        );

        result.Should().BeTrue();
        
        _cartItemRepositoryMock.Verify(r => r.DeleteAsync(testCartItem), Times.Once);

        _unitOfWorkMock.Verify(u => u.BeginTransactionAsync(), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
        _unitOfWorkMock.Verify(u => u.CommitAsync(), Times.Once);

        _cacheMock.Verify(c => c.RemoveDataAsync($"cartItem:{testCartItem.Id}"), Times.Once);
    }

    [Fact]
    public async Task DeleteItemFromCart_CheckCartDoesNotBelongToUser_ThrowsBadRequestException()
    {
        var testUser = AuthTestHelper.CreateTestUser();

        var testCart = CartTestHelper.CreateTestHelper(UserId: testUser.Id);
        var testCartItem = CartItemTestHelper.CreateTestCartItem(CartId: Guid.NewGuid());

        _helper.Setup(h => h.GetUserOr404(testUser.Id))
            .ReturnsAsync(testUser);

        _cartRepositoryMock.Setup(r => r.GetCartIdByUserId(testUser.Id))
            .ReturnsAsync(testCart.Id);

        _helper.Setup(h => h.GetCartItemOr404(testCartItem.Id))
            .ReturnsAsync(testCartItem);

        Func<Task> action = async () => await _cartItemService.DeleteItemFromCartAsync(
            testUser.Id, 
            testCartItem.Id
        );

        await action.Should().ThrowAsync<BadRequestException>()
            .WithMessage("Cart item does not belong to user");

        _cartItemRepositoryMock.Verify(r => r.DeleteAsync(testCartItem), Times.Never);

        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);

        _cacheMock.Verify(c => c.RemoveDataAsync($"cartItem:{testCartItem.Id}"), Times.Never);
    }
}