using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectManagement.Application.DTOs;
using ProjectManagement.Domain.Entities;

namespace ProjectManagement.Api.Controllers;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/users")]
[ApiController]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;

    public UsersController(UserManager<ApplicationUser> userManager) => _userManager = userManager;

    /// <summary>Get list of all users</summary>
    [HttpGet]
    [Authorize(Roles = "Admin,ProjectManager")]
    [ProducesResponseType(typeof(List<UserDto>), 200)]
    public async Task<IActionResult> GetUsers()
    {
        var users = await _userManager.Users.ToListAsync();
        var result = new List<UserDto>();
        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            result.Add(new UserDto
            {
                Id = user.Id,
                UserName = user.UserName!,
                FullName = user.FullName,
                Email = user.Email!,
                Roles = roles.ToList()
            });
        }
        return Ok(result);
    }

    /// <summary>Get user by ID</summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(UserDto), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetUser(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null) return NotFound();

        var roles = await _userManager.GetRolesAsync(user);
        return Ok(new UserDto
        {
            Id = user.Id,
            UserName = user.UserName!,
            FullName = user.FullName,
            Email = user.Email!,
            Roles = roles.ToList()
        });
    }

    /// <summary>Update current user's profile</summary>
    [HttpPut("profile")]
    [ProducesResponseType(typeof(UserDto), 200)]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (userId == null) return Unauthorized();

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return NotFound();

        user.FullName = request.FullName;
        await _userManager.UpdateAsync(user);

        var roles = await _userManager.GetRolesAsync(user);
        return Ok(new UserDto
        {
            Id = user.Id,
            UserName = user.UserName!,
            FullName = user.FullName,
            Email = user.Email!,
            Roles = roles.ToList()
        });
    }
}

public class UpdateProfileRequest { public string FullName { get; set; } = string.Empty; }
