using OpenChat.Domain.Dtos;

namespace OpenChat.Application.Interfaces.Services;

public interface IModelCatalogService
{
    Task<List<ModelDto>> GetModelsAsync(CancellationToken ct = default);
}
