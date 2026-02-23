using MediatR;
using ProjectManagement.Application.DTOs;
using ProjectManagement.Domain.Interfaces;

namespace ProjectManagement.Application.Features.Auth.Commands;

// -- Register --
public record RegisterCommand(
    string UserName, string Email, string Password,
    string FullName, string? Role = null) : IRequest<AuthResponseDto>;

// -- Login --
public record LoginCommand(string Email, string Password) : IRequest<AuthResponseDto>;

// -- Refresh Token --
public record RefreshTokenCommand(string AccessToken, string RefreshToken) : IRequest<AuthResponseDto>;
