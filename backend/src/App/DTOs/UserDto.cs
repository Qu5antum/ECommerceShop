using App.Enum;

namespace App.DTOs;


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


public class UserPreviewResponseDto
{
    public Guid Id { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}


public class UserUpdateDto
{
    public string? UserName { get; set; }
    public string? Email { get; set; }
}


public class UserPasswordUpdateDto
{
    public string Password { get; set; } = string.Empty;
    public string? ConfirmPassword { get; set; } = string.Empty;
}