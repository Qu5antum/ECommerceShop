namespace App.DTOs;


public class CreateReviewDto
{
    public int Rating { get; set; }
    public string Comment { get; set; } = string.Empty;
}


public class UpdateReviewDto
{
    public int? Rating { get; set; }
    public string? Comment { get; set; }
}


public class ReviewResponseDto
{
    public Guid UserId { get; set; }
    public Guid ProductId { get; set; }
    public int Rating { get; set; }
    public string Comment { get; set; } = string.Empty;
}