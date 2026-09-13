using App.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace App.Controllers;


[Authorize]
[ApiController]
[Route("api/[controller]")]
public class UserContoller : ControllerBase{
    private readonly IUserService _userService;

    public UserContoller(IUserService userService)
    {
        _userService = userService;
    }
    
    [Authorize(Roles = "Admin")]
    [HttpGet("users/all")]
    [ProducesResponseType(200)]
    public async Task<IActionResult> GetAllUsersNotAdmin()
    {
        var users = await _userService.GetAllUsersNotAdmin();

        return Ok(users);
    }

    [Authorize]
    [HttpGet("{id:guid}")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetUserProfile(Guid id)
    {
        var user = await _userService.GetUserProfile(id);

        return Ok(user);
    }
}
