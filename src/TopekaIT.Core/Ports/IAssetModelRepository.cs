using TopekaIT.Core.Domain.Entities;

namespace TopekaIT.Core.Ports;

public interface IAssetModelRepository
{
    Task<IEnumerable<AssetModel>> GetAllAsync();
    Task AddAsync(AssetModel model);
}
