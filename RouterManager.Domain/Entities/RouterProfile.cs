namespace RouterManager.Domain.Entities;

public class RouterProfile
{
    public int Id { get; set; }
    public string Ip { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty; // armazenar hash posteriormente
    public string SerialNumber { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // vínculo com o dispositivo/usuário que criou
    public int UserId { get; set; }
    public User User { get; set; } = null!;
}
