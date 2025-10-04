namespace RouterManager.Shared.Dtos.Auth;

public class AuthResponse
{
    public string AccessToken { get; set; } = string.Empty;
    public int ExpiresIn { get; set; }
    public string Email { get; set; } = string.Empty;
}