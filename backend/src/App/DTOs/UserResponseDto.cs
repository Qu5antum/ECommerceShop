using App.Enum;

namespace App.DTOs;


public class UserResponseDto
{
    public Guid Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public List<UserRole> Roles { get; set; } = [];
}