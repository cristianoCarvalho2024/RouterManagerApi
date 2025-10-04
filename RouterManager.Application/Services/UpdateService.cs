using RouterManager.Application.Interfaces;
using RouterManager.Shared.Dtos.Responses;

namespace RouterManager.Application.Services;

public class UpdateService : IUpdateService
{
    private readonly IUpdateRepository _repo;
    public UpdateService(IUpdateRepository repo) => _repo = repo;

    public async Task<UpdatePackageResponse?> CheckAsync(Shared.Dtos.Requests.CheckForUpdateRequest request, CancellationToken ct = default)
    {
        var pkg = await _repo.GetApplicableAsync(request.ProviderId, request.ModelIdentifier, request.FirmwareVersion, ct);
        if (pkg == null) return null;
        return new UpdatePackageResponse
        {
            UpdateId = pkg.Id.ToString(),
            TargetVersion = pkg.TargetVersion,
            Description = pkg.Description,
            Actions = pkg.Actions
                .OrderBy(a => a.Order)
                .Select(a => new UpdateActionItem(a.Order, a.Service, a.Method, a.ParamsJson))
                .ToList()
        };
    }
}