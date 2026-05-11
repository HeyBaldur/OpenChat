using OpenChat.API.Models;

namespace OpenChat.API.Services;

public interface IModelCatalogService
{
    Task<List<ModelDto>> GetModelsAsync(CancellationToken ct = default);
}
