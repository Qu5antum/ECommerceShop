namespace App.Enum;

[Flags]
public enum UserRole
{
    DefaultUser = 1,
    Admin = 2,
    Manager = 4,
    Seller = 8,
    Moderator = 16
}