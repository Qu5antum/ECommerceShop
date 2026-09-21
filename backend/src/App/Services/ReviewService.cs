using App.Repositories;
using App.Transactions;

namespace App.Services;


public interface IReviewService
{
    
}


public class ReviewService : IReviewService
{
    private readonly IReviewRepository _reviewRepository;
    private readonly ILogger<ReviewService> _logger;
    private readonly IHelperService _helper;
    private readonly IUnitOfWork _unitOfWork;

    public ReviewService(IReviewRepository reviewRepository, ILogger<ReviewService> logger, IHelperService helper, IUnitOfWork unitOfWork)
    {
        _reviewRepository = reviewRepository;
        _logger = logger;
        _helper = helper;
        _unitOfWork = unitOfWork;
    }
}