using App.DTOs;
using App.Exceptions;
using App.Models;
using App.Repositories;
using Microsoft.EntityFrameworkCore;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using App.Enum;

namespace App.Services;


public interface IAuthService
{
    Task<bool> RegisterUser(RegisterRequest registerRequest);
    Task<LoginResponse> LoginUser(LoginRequest loginRequest);
}


public class AuthService : IAuthService
{
    private readonly IUserRepository _repository;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthService> _logger;

    public AuthService(IUserRepository repository, IConfiguration configuration, ILogger<AuthService> logger)
    {
        _configuration = configuration;
        _repository = repository;
        _logger = logger;
    }

    public async Task<bool> RegisterUser(RegisterRequest registerRequest)
    {
        var userWithEmail = await _repository.GetUserByEmail(registerRequest.Email);

        if (userWithEmail is not null)
        {
            _logger.LogWarning("User with this email already exists: {Email}", registerRequest.Email);
            throw new AlreadyExistsException("User with this username already exists");
        }

        var userWithUsername = await _repository.GetUserByUserName(registerRequest.UserName);

        if (userWithUsername is not null)
        {
            _logger.LogWarning("User with this username already exists: {UserName}", registerRequest.UserName);
            throw new AlreadyExistsException("User with this username already exists");
        }

        try
        {
            string HashPassword = BCrypt.Net.BCrypt.HashPassword(registerRequest.Password);

            var newUser = new User
            {
                UserName = registerRequest.UserName,
                Email = registerRequest.Email,
                Password = HashPassword,
                isActive = true
            };

            _logger.LogInformation(
                "Creating user: {Email}, IsActive: {IsActive}",
                newUser.Email,
                newUser.isActive
            );

            await _repository.CreateAsync(newUser);

            return true;
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "A database error occurred while creating the user.");
            throw new DatabaseException("Could not save the user to the database.");
        }
    }

    public async Task<LoginResponse> LoginUser(LoginRequest loginRequest)
    {
        var user = await _repository.GetUserByEmail(loginRequest.Email);

        if (user is null)
        {
            _logger.LogWarning("User not found by this Email: {Email}", loginRequest.Email);
            throw new UnauthorizedException("Invalid email or password");
        }

        if (!user.isActive)
        {
            _logger.LogWarning("Inactivate user attemted to login: {userId}", user.Id);
            throw new UnauthorizedException("User account is inactive");
        }

        var passwordValid = BCrypt.Net.BCrypt.Verify(
            loginRequest.Password,
            user.Password
        );

        if (!passwordValid)
        {
            _logger.LogWarning("User password is invalid: {userId}", user.Id);
            throw new UnauthorizedException("Invalid email or password");
        }

        var jwtKey = _configuration["Jwt:Key"];
        if (string.IsNullOrWhiteSpace(jwtKey))
        {
            throw new InvalidOperationException("JWT key is not configured.");
        }

        var issuer = _configuration["Jwt:Issuer"];
        var audience = _configuration["Jwt:Audience"];

        var expiresAt = DateTime.UtcNow.AddMinutes(30);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(ClaimTypes.Name, user.UserName)
        };

        foreach (var role in GetRoles(user.Roles))
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(jwtKey)
        );

        var credentials = new SigningCredentials(
            key,
            SecurityAlgorithms.HmacSha256
        );

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: expiresAt,
            signingCredentials: credentials
        );

        var accessToken = new JwtSecurityTokenHandler().WriteToken(token);

        _logger.LogInformation("User {userId} successfully logged in", user.Id);

        return new LoginResponse
        {
            AccessToken = accessToken,
            ExpiresAt = expiresAt,
            userId = user.Id,
            UserName = user.UserName,
            Email = user.Email
        };
    }

    public static IEnumerable<string> GetRoles(UserRole roles)
    {
        foreach (var role in System.Enum.GetValues<UserRole>())
        {
            if (roles.HasFlag(role))
            {
                yield return role.ToString();
            }
        }
    }
}