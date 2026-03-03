namespace RequestHub.Application.DTOs;

public record LoginRequest(string Username, string Password);
public record AuthResponse(string Token, DateTime ExpiresAtUtc, string Username, string Role);
