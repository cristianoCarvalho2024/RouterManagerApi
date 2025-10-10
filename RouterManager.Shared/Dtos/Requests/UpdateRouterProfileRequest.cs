namespace RouterManager.Shared.Dtos.Requests;

public record UpdateRouterProfileRequest(int Id, string Ip, string Username, string SerialNumber, string? Model, string? Password);
