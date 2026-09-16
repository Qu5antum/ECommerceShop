namespace App.DTOs;


public class CartResponseDto
{
    public Guid UserId { get; set; }
    public List<CartItemResponseDto> Items { get; set; } = [];
}