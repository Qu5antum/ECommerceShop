using App.Enum;

namespace App.DTOs;


public class UserProfileResponseDto
{
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public bool isActive { get; set; } 
}

public class UserResponseDto
{
    public Guid Id { get; set; }

    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public UserRole Roles { get; set; }
    public bool isActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}