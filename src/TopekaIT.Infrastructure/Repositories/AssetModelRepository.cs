using Microsoft.EntityFrameworkCore;
using TopekaIT.Core.Domain.Entities;
using TopekaIT.Core.Ports;
using TopekaIT.Infrastructure.Data;

namespace TopekaIT.Infrastructure.Repositories;

/// <summary>
/// EF storage for asset model names used by the asset forms.
/// </summary>
public class AssetModelRepository : IAssetModelRepository
{
    private readonly IDivisionDbContextFactory _factory;

    public AssetModelRepository(IDivisionDbContextFactory factory)
    {
        _factory = factory;
    }

    public async Task<IEnumerable<AssetModel>> GetAllAsync()
    {
        await using var db = await _factory.CreateDbContextAsync();
        return await db.AssetModels
            .AsNoTracking()
            .OrderBy(m => m.Name)
            .ToListAsync();
    }

    public async Task AddAsync(AssetModel model)
    {
        await using var db = await _factory.CreateDbContextAsync();
        db.AssetModels.Add(model);
        await db.SaveChangesAsync();
    }
}
