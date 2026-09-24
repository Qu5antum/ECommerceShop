using App.DTOs;
using App.Enum;
using App.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace App.Controllers;


[Authorize]
[ApiController]
[Route("api/[controller]")]
public class UserController : ControllerBase{
    private readonly IUserService _userService;
    private readonly Helper _helper;

    public UserController(IUserService userService, Helper helper)
    {
        _userService = userService;
        _helper = helper;
    }
    
    [Authorize(Roles = "Admin, Moderator")]
    [HttpGet("/Admin/Users")]
    [ProducesResponseType(200)]
    [ProducesResponseType(401)]
    public async Task<IActionResult> GetAllUsersNotAdmin()
    {
        var users = await _userService.GetAllUsersNotAdmin();

        return Ok(users);
    }

    [Authorize(Roles = "Admin, Moderator")]
    [HttpGet("Admin/{userId:guid}")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    [ProducesResponseType(401)]
    public async Task<IActionResult> GetUserById(Guid userId)
    {
        var user = await _userService.GetUserById(userId);

        return Ok(user);
    }

    [Authorize]
    [HttpPut]
    [ProducesResponseType(200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> UpdateUserProfile(UserUpdateDto userUpdateDto)
    {
        Guid userId = _helper.GetUserId();

        await _userService.UpdateUserProfile(userId, userUpdateDto);
        
        return Ok("User successfully updated");
    }

    [Authorize(Roles = "Admin, Moderator")]
    [HttpDelete("Admin/{userId:guid}")]
    [ProducesResponseType(200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> DeleteUser(Guid userId)
    {
        await _userService.DeleteUserAsync(userId);

        return Ok("User successfully deleted");
    }

    [Authorize(Roles = "Admin, Moderator")]
    [HttpPut("Admin/{userId:guid}/ActiveStatus")]
    [ProducesResponseType(200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> ActivateOrDiactivateUser(Guid userId, bool isActive)
    {
        await _userService.ActivateOrDiactivateUserAsync(userId, isActive);

        return Ok("User active status successfully updated");
    }

    [Authorize(Roles = "Admin, Moderator")]
    [HttpPut("Admin/{userId:guid}/RoleAdd")]
    [ProducesResponseType(200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> AddRoleToUser(Guid userId, UserRole role)
    {
        await _userService.AddRoleToUserAsync(userId, role);

        return Ok("Successfully added role of user");
    }

    [Authorize(Roles = "Admin, Moderator")]
    [HttpPut("Admin/{userId:guid}/RoleRemove")]
    [ProducesResponseType(200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> RemoveRoleToUser(Guid userId, UserRole role)
    {
        await _userService.RemoveRoleFromUserAsync(userId, role);

        return Ok("Successfully removed role of user");
    }
}
