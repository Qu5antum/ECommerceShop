using App.DTOs;
using App.Enum;
using App.Services;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace App.Controllers;


[Authorize]
[ApiController]
[Route("api/[controller]")]
public class UserController : ControllerBase{
    private readonly IUserService _userService;
    private readonly Helper _helper;
    private readonly IValidator<UserPasswordUpdateDto> _validator;

    public UserController(IUserService userService, Helper helper, IValidator<UserPasswordUpdateDto> validator)
    {
        _userService = userService;
        _helper = helper;
        _validator = validator;
    }
    
    [Authorize(Roles = "Admin, Moderator, Manager")]
    [HttpGet("Admin/Users")]
    public async Task<IActionResult> GetAllUsersNotAdmin()
    {
        var users = await _userService.GetAllUsersNotAdmin();

        return Ok(users);
    }

    [Authorize(Roles = "Admin, Moderator, Manager")]
    [HttpGet("Admin/{userId:guid}")]
    public async Task<IActionResult> GetUserById(Guid userId)
    {
        var user = await _userService.GetUserById(userId);

        return Ok(user);
    }

    [HttpPut]
    public async Task<IActionResult> UpdateUserProfile(UserUpdateDto userUpdateDto)
    {
        Guid userId = _helper.GetUserId();

        await _userService.UpdateUserProfile(userId, userUpdateDto);
        
        return Ok("User successfully updated");
    }

    [HttpGet("Profile")]
    public async Task<IActionResult> GetCurrentUserProfile()
    {
        Guid userId = _helper.GetUserId();

        var user = await _userService.GetUserById(userId);

        return Ok(user);
    }

    [HttpPut("ChangePassword")]
    public async Task<IActionResult> UpdateUserPassword(UserPasswordUpdateDto passwordUpdateDto)
    {
        Guid userId = _helper.GetUserId();

        var validationResult = await _validator.ValidateAsync(passwordUpdateDto);
        
        if (!validationResult.IsValid)
        {
            return BadRequest(validationResult.ToDictionary());
        }

        await _userService.UpdateUserPasswordAsync(userId, passwordUpdateDto);

        return Ok("Password successfully updated");
    }


    [Authorize(Roles = "Admin, Moderator, Manager")]
    [HttpDelete("Admin/{userId:guid}")]
    public async Task<IActionResult> DeleteUser(Guid userId)
    {
        Guid currentUserId = _helper.GetUserId();

        await _userService.DeleteUserAsync(currentUserId, userId);

        return Ok("User successfully deleted");
    }

    [Authorize(Roles = "Admin, Moderator, Manager")]
    [HttpPut("Admin/{userId:guid}/ActiveStatus")]
    public async Task<IActionResult> ActivateOrDiactivateUser(Guid userId, bool isActive)
    {
        Guid currentUserId = _helper.GetUserId();

        await _userService.ActivateOrDiactivateUserAsync(currentUserId, userId, isActive);

        return Ok("User active status successfully updated");
    }

    [Authorize(Roles = "Admin, Moderator, Manager")]
    [HttpPut("Admin/{userId:guid}/RoleAdd")]
    public async Task<IActionResult> AddRoleToUser(Guid userId, UserRole role)
    {
        Guid currentUserId = _helper.GetUserId();

        await _userService.AddRoleToUserAsync(currentUserId, userId, role);

        return Ok("Successfully added role of user");
    }

    [Authorize(Roles = "Admin, Moderator, Manager")]
    [HttpPut("Admin/{userId:guid}/RoleRemove")]
    public async Task<IActionResult> RemoveRoleToUser(Guid userId, UserRole role)
    {
        Guid currentUserId = _helper.GetUserId();

        await _userService.RemoveRoleFromUserAsync(currentUserId, userId, role);

        return Ok("Successfully removed role of user");
    }
}
