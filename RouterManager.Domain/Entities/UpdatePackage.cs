namespace RouterManager.Domain.Entities;

public class UpdatePackage
{
    public int Id { get; set; }

    // Nome amig�vel da ordem
    public string Name { get; set; } = string.Empty;

    // Crit�rios de aplica��o (mantidos para compatibilidade)
    public int ProviderId { get; set; }
    public string ModelIdentifier { get; set; } = string.Empty;
    public string? FirmwareVersion { get; set; }
    public string? SerialNumber { get; set; }

    // Ordem de atualiza��o legada (mantida para compatibilidade)
    public string RequestPayload { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // A��es desta ordem
    public ICollection<UpdateAction> Actions { get; set; } = new List<UpdateAction>();
}