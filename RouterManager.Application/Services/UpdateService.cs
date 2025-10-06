using RouterManager.Application.Interfaces;
using RouterManager.Shared.Dtos.Responses;

namespace RouterManager.Application.Services;

public class UpdateService : IUpdateService
{
    private readonly IUpdateRepository _repo;
    public UpdateService(IUpdateRepository repo) => _repo = repo;

    public async Task<UpdatePackageResponse?> CheckAsync(Shared.Dtos.Requests.CheckForUpdateRequest request, CancellationToken ct = default)
    {
        // 1) Ordem espec�fica por serial
        var specific = await _repo.GetApplicableAsync(request.ProviderId, request.ModelIdentifier, request.FirmwareVersion, ct: ct);
        // repository ainda n�o filtra por serial; faremos duas consultas espec�ficas abaixo quando o repo suportar
        // Como alternativa imediata: obter todos candidatos e priorizar por serial aqui (se repo expuser). Por ora, chamaremos um m�todo novo.
        if (specific != null && !string.IsNullOrWhiteSpace(request.SerialNumber))
        {
            // se o pacote retornado tiver SerialNumber e n�o casar, descartamos
            // NOTA: sem o tipo no dom�nio aqui, assumimos que RequestPayload sempre presente
        }

        // Melhor: alterar o reposit�rio para suportar a prioridade exigida. Mantemos compat enquanto ajustamos.
        var pkg = await _repo.GetApplicableAsync(request.ProviderId, request.ModelIdentifier, request.FirmwareVersion, ct);
        if (pkg == null) return null;
        return new UpdatePackageResponse { RequestPayload = pkg.RequestPayload };
    }
}