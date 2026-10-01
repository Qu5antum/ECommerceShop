using App.DTOs;
using App.Services;
using Microsoft.AspNetCore.Mvc;

namespace App.Controllers;


[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("Register")]
    public async Task<IActionResult> Register(RegisterRequest registerRequest)
    {
        await _authService.RegisterUser(registerRequest);

        return Ok(new
        {
            message = "User successfully created"
        });
    }

    [HttpPost("AdminRegister")]
    public async Task<IActionResult> CreateUserAdmin(RegisterRequest registerRequest)
    {
        await _authService.CreateUserAdmin(registerRequest);

        return Ok(new
        {
            message = "Admin successfully created"
        });
    }

    [HttpPost("Login")]
    public async Task<IActionResult> Login(LoginRequest loginRequest)
    {
        var response = await _authService.LoginUser(loginRequest);

        return Ok(response);
    }
}