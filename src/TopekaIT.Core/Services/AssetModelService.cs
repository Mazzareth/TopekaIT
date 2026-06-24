using TopekaIT.Core.Domain.Entities;
using TopekaIT.Core.Ports;

namespace TopekaIT.Core.Services;

/// <summary>
/// Keeps the asset model pick-list behind the asset forms.
/// </summary>
public class AssetModelService
{
    private readonly IAssetModelRepository _repository;

    public AssetModelService(IAssetModelRepository repository)
    {
        _repository = repository;
    }

    public Task<IEnumerable<AssetModel>> GetAllAsync()
    {
        return _repository.GetAllAsync();
    }

    public Task AddAsync(string name)
    {
        var model = new AssetModel
        {
            Id = Guid.NewGuid().ToString("N").Substring(0, 16),
            Name = name
        };
        return _repository.AddAsync(model);
    }
}
