using App.Enum;

namespace App.Models;

public class User : BaseModel
{
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public UserRole Roles { get; set; } = UserRole.DefaultUser;
    public bool isActive { get; set; } = true;
    public void AddRole(UserRole role) => Roles |= role;
    
    public void RemoveRole(UserRole role) => Roles &= ~role;
    
    public bool HasRole(UserRole role) => Roles.HasFlag(role);
}