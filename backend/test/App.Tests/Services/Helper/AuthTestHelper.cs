using App.Models;
using App.Enum;

public static class AuthTestHelper
{
    public static User CreateTestAdminUser(
        string email = "test@example.com",
        string userName = "testuser",
        string password = "SecurePassword123",
        bool isActive = true)
    {
        return new User
        {
            Id = Guid.NewGuid(),
            UserName = userName,
            Email = email,
            Password = BCrypt.Net.BCrypt.HashPassword(password),
            isActive = isActive,
            Roles = UserRole.Admin
        };
    }
}