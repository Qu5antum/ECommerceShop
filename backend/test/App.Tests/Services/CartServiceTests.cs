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

    [Fact]
    public async Task GetAllItemsInCartAsync_WhenDataIsInCache_ReturnsCachedItemsWithoutHittingRepository()
    {
        var testUser = AuthTestHelper.CreateTestUser();

        var cachedItems = new List<CartItemResponseDto>
        {
            new CartItemResponseDto { Id = Guid.NewGuid(), ProductId = Guid.NewGuid(), Quantity = 2 }
        };

        _helper.Setup(h => h.GetUserOr404(testUser.Id))
            .ReturnsAsync(testUser);

        _cacheMock.Setup(c => c.GetDataAsync<List<CartItemResponseDto>>($"cartItems:{testUser.Id}"))
            .ReturnsAsync(cachedItems);

        var result = await _cartItemService.GetAllItemsInCartAsync(testUser.Id);

        result.Should().NotBeNull();
        result.Should().BeEquivalentTo(cachedItems);

        _cartRepositoryMock.Verify(r => r.GetCartIdByUserId(It.IsAny<Guid>()), Times.Never);
        _cartItemRepositoryMock.Verify(r => r.GetAllItemsInCartAsyncByCartId(It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task GetAllItemsInCartAsync_WhenCartNotFound_ThrowsNotFoundException()
    {
        var testUser = AuthTestHelper.CreateTestUser();

        _helper.Setup(h => h.GetUserOr404(testUser.Id))
            .ReturnsAsync(testUser);

        _cacheMock.Setup(c => c.GetDataAsync<List<CartItemResponseDto>>($"cartItems:{testUser.Id}"))
            .ReturnsAsync((List<CartItemResponseDto>?)null);

        _cartRepositoryMock.Setup(r => r.GetCartIdByUserId(testUser.Id))
            .ReturnsAsync(Guid.Empty);

        Func<Task> action = async () => await _cartItemService.GetAllItemsInCartAsync(testUser.Id);

        await action.Should().ThrowAsync<NotFoundException>()
            .WithMessage("Cart not found");

        _cartItemRepositoryMock.Verify(r => r.GetAllItemsInCartAsyncByCartId(It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task GetAllItemsInCartAsync_WhenDataIsNotInCache_FetchesFromRepositoryMapsAndCaches()
    {
        var testUser = AuthTestHelper.CreateTestUser();
        var testCart = CartTestHelper.CreateTestHelper(UserId: testUser.Id);
        var cacheKey = $"cart:items:{testUser.Id}";

        var testCartItems = new List<CartItem>
        {
            CartItemTestHelper.CreateTestCartItem(CartId: testCart.Id, Quantity: 1),
            CartItemTestHelper.CreateTestCartItem(CartId: testCart.Id, Quantity: 3)
        };

        _helper.Setup(h => h.GetUserOr404(testUser.Id))
            .ReturnsAsync(testUser);

        _cacheMock.Setup(c => c.GetDataAsync<List<CartItemResponseDto>>($"cartItems:{testUser.Id}"))
            .ReturnsAsync((List<CartItemResponseDto>?)null);

        _cartRepositoryMock.Setup(r => r.GetCartIdByUserId(testUser.Id))
            .ReturnsAsync(testCart.Id);

        _cartItemRepositoryMock.Setup(r => r.GetAllItemsInCartAsyncByCartId(testCart.Id))
            .ReturnsAsync(testCartItems);

        var result = await _cartItemService.GetAllItemsInCartAsync(testUser.Id);

        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result[0].Quantity.Should().Be(1);
        result[1].Quantity.Should().Be(3);

        _cartItemRepositoryMock.Verify(r => r.GetAllItemsInCartAsyncByCartId(testCart.Id), Times.Once);

        _cacheMock.Verify(c => c.SetDataAsync(
            $"cartItems:{testUser.Id}",
            It.Is<List<CartItemResponseDto>>(list => list.Count == 2),
            TimeSpan.FromMinutes(5)
        ), Times.Once);
    }

    [Fact]
    public async Task GetItemInCartByIdAsync_WhenDataIsInCache_ReturnsCachedItemWithoutHittingDatabase()
    {
        var userId = Guid.NewGuid();
        var itemId = Guid.NewGuid();

        var cachedItem = new CartItemResponseDto
        {
            Id = itemId,
            Quantity = 3,
            ProductId = Guid.NewGuid()
        };

        _cacheMock.Setup(c => c.GetDataAsync<CartItemResponseDto>($"cartItem:{itemId}"))
            .ReturnsAsync(cachedItem);

        var result = await _cartItemService.GetItemInCartByIdAsync(userId, itemId);

        result.Should().NotBeNull();
        result.Should().BeEquivalentTo(cachedItem);

        _helper.Verify(h => h.GetUserOr404(It.IsAny<Guid>()), Times.Never);
        _cartRepositoryMock.Verify(r => r.GetCartIdByUserId(It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task GetItemInCartByIdAsync_WhenCartNotFound_ThrowsNotFoundException()
    {
        var userId = Guid.NewGuid();
        var itemId = Guid.NewGuid();
        var testUser = AuthTestHelper.CreateTestUser(Id: userId);

        _cacheMock.Setup(c => c.GetDataAsync<CartItemResponseDto>($"cartItem:{itemId}"))
            .ReturnsAsync((CartItemResponseDto?)null);

        _helper.Setup(h => h.GetUserOr404(userId))
            .ReturnsAsync(testUser);

        _cartRepositoryMock.Setup(r => r.GetCartIdByUserId(userId))
            .ReturnsAsync(Guid.Empty);

        Func<Task> action = async () => await _cartItemService.GetItemInCartByIdAsync(userId, itemId);

        await action.Should().ThrowAsync<NotFoundException>()
            .WithMessage("Cart not found");

        _helper.Verify(h => h.GetCartItemOr404(It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task GetItemInCartByIdAsync_WhenItemDoesNotBelongToUser_ThrowsBadRequestException()
    {
        var userId = Guid.NewGuid();
        var itemId = Guid.NewGuid();
        
        var testUser = AuthTestHelper.CreateTestUser(Id: userId);
        var testCart = CartTestHelper.CreateTestHelper(UserId: userId);
        
        var foreignCartItem = CartItemTestHelper.CreateTestCartItem(Id: itemId, CartId: Guid.NewGuid());

        _cacheMock.Setup(c => c.GetDataAsync<CartItemResponseDto>($"cartItem:{itemId}"))
            .ReturnsAsync((CartItemResponseDto?)null);

        _helper.Setup(h => h.GetUserOr404(userId))
            .ReturnsAsync(testUser);

        _cartRepositoryMock.Setup(r => r.GetCartIdByUserId(userId))
            .ReturnsAsync(testCart.Id);

        _helper.Setup(h => h.GetCartItemOr404(itemId))
            .ReturnsAsync(foreignCartItem);

        Func<Task> action = async () => await _cartItemService.GetItemInCartByIdAsync(userId, itemId);

        await action.Should().ThrowAsync<BadRequestException>()
            .WithMessage("Cart item does not belong to user");

        _cacheMock.Verify(c => c.SetDataAsync(It.IsAny<string>(), It.IsAny<CartItemResponseDto>(), It.IsAny<TimeSpan>()), Times.Never);
    }

    [Fact]
    public async Task GetItemInCartByIdAsync_WhenDataIsNotInCache_FetchesFromDbMapsAndCaches()
    {
        var userId = Guid.NewGuid();
        var itemId = Guid.NewGuid();

        var testUser = AuthTestHelper.CreateTestUser(Id: userId);
        var testCart = CartTestHelper.CreateTestHelper(UserId: userId);
        var testCartItem = CartItemTestHelper.CreateTestCartItem(Id: itemId, CartId: testCart.Id, Quantity: 4);

        _cacheMock.Setup(c => c.GetDataAsync<CartItemResponseDto>($"cartItem:{itemId}"))
            .ReturnsAsync((CartItemResponseDto?)null);

        _helper.Setup(h => h.GetUserOr404(userId))
            .ReturnsAsync(testUser);

        _cartRepositoryMock.Setup(r => r.GetCartIdByUserId(userId))
            .ReturnsAsync(testCart.Id);

        _helper.Setup(h => h.GetCartItemOr404(itemId))
            .ReturnsAsync(testCartItem);

        var result = await _cartItemService.GetItemInCartByIdAsync(userId, itemId);

        result.Should().NotBeNull();
        result.Id.Should().Be(itemId);
        result.Quantity.Should().Be(4);
        result.CartId.Should().Be(testCart.Id);

        _cacheMock.Verify(c => c.SetDataAsync(
            $"cartItem:{itemId}",
            It.Is<CartItemResponseDto>(dto => dto.Id == itemId && dto.Quantity == 4),
            TimeSpan.FromMinutes(5)
        ), Times.Once);
    }
}