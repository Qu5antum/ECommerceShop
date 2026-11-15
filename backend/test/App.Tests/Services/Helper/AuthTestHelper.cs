using App.Models;
using App.Enum;

public static class AuthTestHelper
{
    public static User CreateTestUser(
        Guid? Id = null,
        string email = "test@example.com",
        string userName = "testuser",
        string password = "SecurePassword123",
        bool isActive = true,
        UserRole roles = UserRole.DefaultUser)
    {
        return new User
        {
            Id = Id ?? Guid.NewGuid(),
            UserName = userName,
            Email = email,
            Password = BCrypt.Net.BCrypt.HashPassword(password),
            isActive = isActive,
            Roles = roles
        };
    }
}