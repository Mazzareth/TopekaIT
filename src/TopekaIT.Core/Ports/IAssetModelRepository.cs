using TopekaIT.Core.Domain.Entities;

namespace TopekaIT.Core.Ports;

/// <summary>
/// Storage for the asset model pick-list.
/// </summary>
public interface IAssetModelRepository
{
    Task<IEnumerable<AssetModel>> GetAllAsync();
    Task AddAsync(AssetModel model);
}
