using App.Exceptions;
using App.Models;
using App.Repositories;

namespace App.Services;


public interface IHelper
{
    Task<User> GetUserOr404(Guid userId);
    Task<SellerProfile> GetSellerProfileOr404(Guid profileId);
    Task<Category> GetCategoryOr404(Guid categoryId);
    Task<Product> GetProductOr404(Guid productId);
}
public class Helper : IHelper
{
    private readonly IUserRepository _userRepository;
    private readonly ICategoryRepsitory _categoryRepository;
    private readonly ISellerProfileRepository _sellerProfileRepository;
    private readonly IProductRepository _productRepository;
    private readonly ILogger<Helper> _logger;

    public Helper
    (
        IUserRepository userRepository,
        ICategoryRepsitory categoryRepsitory,
        ISellerProfileRepository sellerProfileRepository,
        IProductRepository productRepository,
        ILogger<Helper> logger
    )
    {
        _userRepository = userRepository;
        _categoryRepository = categoryRepsitory;
        _sellerProfileRepository = sellerProfileRepository;
        _productRepository = productRepository;
        _logger = logger;
    }

    public async Task<User> GetUserOr404(Guid userId)
    {
        var user = await _userRepository.GetByIdAsync(userId);

        if (user == null)
        {
            _logger.LogWarning("User not found by this id: {userId}", userId);
            throw new NotFoundException("User not found");
        }

        return user;
    }

    public async Task<SellerProfile> GetSellerProfileOr404(Guid profileId)
    {
        var sellerProfile = await _sellerProfileRepository.GetByIdAsync(profileId);

        if (sellerProfile == null)
        {
            _logger.LogWarning("Seller profile not found by this id: {profileId}", profileId);
            throw new NotFoundException("Seller profile not found");
        }

        return sellerProfile;
    }

    public async Task<Category> GetCategoryOr404(Guid categoryId)
    {
        var category = await _categoryRepository.GetByIdAsync(categoryId);

        if (category == null)
        {
            _logger.LogWarning("Seller profile not found by this id: {categoryId}", categoryId);
            throw new NotFoundException("Category not found");
        }

        return category;
    }

    public async Task<Product> GetProductOr404(Guid productId)
    {
        var product = await _productRepository.GetByIdAsync(productId);

        if (product == null)
        {
            _logger.LogWarning("Seller profile not found by this id: {productId}", productId);
            throw new NotFoundException("Product not found");
        }

        return product;
    }
}