namespace RouterManager.Domain.Entities;

public class UpdatePackage
{
    public int Id { get; set; }
    // Crit�rios de aplica��o
    public int ProviderId { get; set; }
    public string ModelIdentifier { get; set; } = string.Empty;
    public string? FirmwareVersion { get; set; }
    public string? SerialNumber { get; set; }

    // Ordem de atualiza��o serializada em JSON para o app executar via HttpExecuter
    public string RequestPayload { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}