namespace RouterManager.Shared.Dtos.Auth;

public record RegisterRequest(string Email, string Password, string ConfirmPassword);