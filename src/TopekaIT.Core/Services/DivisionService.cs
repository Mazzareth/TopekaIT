using TopekaIT.Core.Domain.Entities;
using TopekaIT.Core.Ports;

namespace TopekaIT.Core.Services;

public class DivisionService
{
    private readonly IDivisionRepository _repo;

    public DivisionService(IDivisionRepository repo)
    {
        _repo = repo;
    }

    public Task<IReadOnlyList<Division>> GetAllAsync(CancellationToken ct = default)
        => _repo.GetAllAsync(ct);

    public Task<Division?> GetByIdAsync(string id, CancellationToken ct = default)
        => _repo.GetByIdAsync(id, ct);

    public async Task<Division> CreateAsync(string code, string name, string connectionString, CancellationToken ct = default)
    {
        var id = NormalizeCode(code);
        if (await _repo.GetByIdAsync(id, ct) != null)
        {
            throw new InvalidOperationException($"Division '{id}' already exists.");
        }

        var division = new Division
        {
            Id = id,
            Name = name.Trim(),
            ConnectionString = connectionString.Trim(),
            CreatedAt = DateTimeOffset.UtcNow,
        };

        await _repo.AddAsync(division, ct);
        return division;
    }

    public Task<Division> CreateAsync(string name, string connectionString, CancellationToken ct = default)
        => CreateAsync(name, name, connectionString, ct);

    public Task UpdateAsync(Division division, CancellationToken ct = default)
        => _repo.UpdateAsync(division, ct);

    public Task RemoveAsync(string id, CancellationToken ct = default)
        => _repo.RemoveAsync(id, ct);

    public static string BuildLocalDbConnectionString(string code)
    {
        var databaseName = BuildDatabaseName(code);
        return $"Server=(localdb)\\MSSQLLocalDB;Database={databaseName};Trusted_Connection=true;";
    }

    public static string BuildDatabaseName(string code)
        => $"IT_{NormalizeCode(code).Replace("-", "_", StringComparison.Ordinal)}";

    private static string NormalizeCode(string code)
    {
        var cleaned = new string(code.Trim().ToUpperInvariant()
            .Select(ch => char.IsLetterOrDigit(ch) || ch == '-' ? ch : '-')
            .ToArray());

        while (cleaned.Contains("--", StringComparison.Ordinal))
        {
            cleaned = cleaned.Replace("--", "-", StringComparison.Ordinal);
        }

        return cleaned.Trim('-');
    }
}
