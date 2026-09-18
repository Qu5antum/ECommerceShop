using App.DTOs;
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
    [HttpGet("Users")]
    [ProducesResponseType(200)]
    [ProducesResponseType(401)]
    public async Task<IActionResult> GetAllUsersNotAdmin()
    {
        var users = await _userService.GetAllUsersNotAdmin();

        return Ok(users);
    }

    [Authorize]
    [HttpGet("{userId:guid}")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    [ProducesResponseType(401)]
    public async Task<IActionResult> GetUserProfile(Guid userId)
    {
        var user = await _userService.GetUserProfile(userId);

        return Ok(user);
    }

    [Authorize]
    [HttpPut("{userId:guid}")]
    [ProducesResponseType(200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> UpdateUserProfile(Guid userId, UserUpdateDto userUpdateDto)
    {
        await _userService.UpdateUserProfile(userId, userUpdateDto);
        return Ok("User successfully updated");
    }
}
