using System.ComponentModel.DataAnnotations;
using App.Enum;

namespace App.DTOs;


public class OrderReponseDto
{
    public Guid Id { get; set; }
    public Guid userId { get; set; }
    public decimal TotalAmount { get; set; }
    public OrderStatus status { get; set; }
    public List<OrderItemResponseDto> orderItems { get; set; } = [];
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}


public class UpdateOrderStatusDto
{
    [Required]
    [EnumDataType(typeof(OrderStatus))]
    public OrderStatus Status { get; set; }
}