using System.IdentityModel.Tokens.Jwt;
using AutoMapper;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ProjectManagement.Application.DTOs;
using ProjectManagement.Application.Features.Auth.Commands;
using ProjectManagement.Domain.Entities;
using ProjectManagement.Domain.Events;
using ProjectManagement.Domain.Interfaces;

namespace ProjectManagement.Infrastructure.Identity;

public class RegisterCommandHandler : IRequestHandler<RegisterCommand, AuthResponseDto>
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ITokenService _tokenService;
    private readonly IMediator _mediator;

    public RegisterCommandHandler(UserManager<ApplicationUser> userManager,
        ITokenService tokenService, IMediator mediator)
    {
        _userManager = userManager;
        _tokenService = tokenService;
        _mediator = mediator;
    }

    public async Task<AuthResponseDto> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        var existingUser = await _userManager.FindByEmailAsync(request.Email);
        if (existingUser != null)
            throw new InvalidOperationException("A user with this email already exists");

        var user = new ApplicationUser
        {
            UserName = request.UserName,
            Email = request.Email,
            FullName = request.FullName
        };

        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
            throw new InvalidOperationException(string.Join(", ", result.Errors.Select(e => e.Description)));

        var requestedRole = request.Role ?? "Developer";
        Console.WriteLine($"[DEBUG] Registering user with Role: '{request.Role}'. Assigned Role: '{requestedRole}'");
        
        var validRoles = new[] { "Admin", "ProjectManager", "Developer" };

        if (!validRoles.Contains(requestedRole))
            throw new InvalidOperationException($"Invalid role requested: {requestedRole}. Valid roles are: Admin, ProjectManager, Developer");

        await _userManager.AddToRoleAsync(user, requestedRole);
        var (accessToken, refreshToken) = await _tokenService.GenerateTokensAsync(user);
        var roles = await _userManager.GetRolesAsync(user);

        await _mediator.Publish(new UserRegisteredEvent(user.Id, user.UserName!), cancellationToken);

        return new AuthResponseDto
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresAt = DateTime.UtcNow.AddHours(2),
            User = new UserDto
            {
                Id = user.Id,
                UserName = user.UserName!,
                FullName = user.FullName,
                Email = user.Email!,
                Roles = roles.ToList()
            }
        };
    }
}

public class LoginCommandHandler : IRequestHandler<LoginCommand, AuthResponseDto>
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ITokenService _tokenService;

    public LoginCommandHandler(UserManager<ApplicationUser> userManager, ITokenService tokenService)
    {
        _userManager = userManager;
        _tokenService = tokenService;
    }

    public async Task<AuthResponseDto> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByEmailAsync(request.Email)
            ?? throw new UnauthorizedAccessException("Invalid credentials");

        if (!await _userManager.CheckPasswordAsync(user, request.Password))
            throw new UnauthorizedAccessException("Invalid credentials");

        var (accessToken, refreshToken) = await _tokenService.GenerateTokensAsync(user);
        var roles = await _userManager.GetRolesAsync(user);

        return new AuthResponseDto
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresAt = DateTime.UtcNow.AddHours(2),
            User = new UserDto
            {
                Id = user.Id,
                UserName = user.UserName!,
                FullName = user.FullName,
                Email = user.Email!,
                Roles = roles.ToList()
            }
        };
    }
}

public class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, AuthResponseDto>
{
    private readonly ITokenService _tokenService;
    private readonly UserManager<ApplicationUser> _userManager;

    public RefreshTokenCommandHandler(ITokenService tokenService, UserManager<ApplicationUser> userManager)
    {
        _tokenService = tokenService;
        _userManager = userManager;
    }

    public async Task<AuthResponseDto> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        var (accessToken, refreshToken) = await _tokenService.RefreshTokenAsync(
            request.AccessToken, request.RefreshToken);

        // Get user from the new token for the response
        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(accessToken);
        var userId = jwt.Claims.First(c => c.Type == System.Security.Claims.ClaimTypes.NameIdentifier).Value;
        var user = await _userManager.FindByIdAsync(userId)
            ?? throw new UnauthorizedAccessException("User not found");
        var roles = await _userManager.GetRolesAsync(user);

        return new AuthResponseDto
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresAt = DateTime.UtcNow.AddHours(2),
            User = new UserDto
            {
                Id = user.Id,
                UserName = user.UserName!,
                FullName = user.FullName,
                Email = user.Email!,
                Roles = roles.ToList()
            }
        };
    }
}
