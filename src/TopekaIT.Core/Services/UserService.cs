using System.Security.Cryptography;
using TopekaIT.Core.Domain.Entities;
using TopekaIT.Core.Domain.Enums;
using TopekaIT.Core.Ports;

namespace TopekaIT.Core.Services;

/// <summary>
/// Handles users, passwords, and station PINs. It keeps the login path and the kiosk path close enough that they follow the same security habits.
/// </summary>
public class UserService
{
    private readonly IUserRepository _repo;
    private readonly IDivisionRepository? _divisions;

    public UserService(IUserRepository repo)
    {
        _repo = repo;
    }

    public UserService(IUserRepository repo, IDivisionRepository divisions)
    {
        _repo = repo;
        _divisions = divisions;
    }

    public Task<IReadOnlyList<User>> GetAllAsync(CancellationToken ct = default) => _repo.GetAllAsync(ct);

    public Task<User?> GetByIdAsync(string id, CancellationToken ct = default) => _repo.GetByIdAsync(id, ct);

    public async Task<User?> ValidateCredentialsAsync(string username, string password, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrEmpty(password)) return null;
        var user = await _repo.GetByUsernameAsync(username.Trim(), ct);
        if (user == null) return null;
        var iterations = user.PasswordIterations > 0
            ? user.PasswordIterations
            : PasswordHasher.LegacyIterations;
        if (!PasswordHasher.Verify(password, user.PasswordHash, user.PasswordSalt, iterations))
        {
            return null;
        }

        if (iterations < PasswordHasher.CurrentIterations)
        {
            var upgraded = PasswordHasher.HashWithMetadata(password);
            user.PasswordHash = upgraded.hash;
            user.PasswordSalt = upgraded.salt;
            user.PasswordIterations = upgraded.iterations;
            await _repo.UpdateAsync(user, ct);
        }

        return user;
    }

    public async Task MarkActiveAsync(string userId, DateTimeOffset timestamp, CancellationToken ct = default)
    {
        var user = await _repo.GetByIdAsync(userId, ct);
        if (user == null) return;

        user.LastActiveAt = timestamp;
        await _repo.UpdateAsync(user, ct);
    }

    public async Task<User> CreateAsync(string name, string username, string password, AccessTier role, string? divisionId = null, CancellationToken ct = default)
    {
        var all = await _repo.GetAllAsync(ct);
        var maxNum = all
            .Select(u => int.TryParse(u.Id.AsSpan(2), out var n) ? n : 0)
            .DefaultIfEmpty(0)
            .Max();
        var id = $"u-{(maxNum + 1):D3}";
        var avatar = BuildAvatar(name);
        var passwordMetadata = PasswordHasher.HashWithMetadata(string.IsNullOrEmpty(password) ? "changeme" : password);
        var user = new User
        {
            Id = id,
            Name = name,
            Username = username,
            Role = role,
            Avatar = avatar,
            PasswordHash = passwordMetadata.hash,
            PasswordSalt = passwordMetadata.salt,
            PasswordIterations = passwordMetadata.iterations,
            MustChangePassword = true,
            DivisionId = divisionId,
        };
        await _repo.AddAsync(user, ct);
        return user;
    }

    public async Task UpdateAsync(User user, string? newPassword, CancellationToken ct = default)
    {
        if (!string.IsNullOrEmpty(newPassword))
        {
            var passwordMetadata = PasswordHasher.HashWithMetadata(newPassword);
            user.PasswordHash = passwordMetadata.hash;
            user.PasswordSalt = passwordMetadata.salt;
            user.PasswordIterations = passwordMetadata.iterations;
            user.MustChangePassword = false;
        }
        user.Avatar = BuildAvatar(user.Name);
        await _repo.UpdateAsync(user, ct);
    }

    public async Task SetStationPinAsync(string userId, string? stationPin, CancellationToken ct = default)
    {
        var user = await _repo.GetByIdAsync(userId, ct)
            ?? throw new InvalidOperationException("User not found.");

        if (string.IsNullOrWhiteSpace(stationPin))
        {
            user.StationPinHash = null;
            user.StationPinSalt = null;
            user.StationPinIterations = 0;
            await _repo.UpdateAsync(user, ct);
            return;
        }

        var normalizedPin = NormalizeStationPin(stationPin);
        if (!IsValidStationPin(normalizedPin))
        {
            throw new InvalidOperationException("Station PIN must be exactly 6 numeric digits.");
        }

        if (string.IsNullOrWhiteSpace(user.DivisionId))
        {
            throw new InvalidOperationException("Station PINs require a division-scoped user.");
        }

        var all = await _repo.GetAllAsync(ct);
        var duplicate = all.Any(other =>
            !string.Equals(other.Id, user.Id, StringComparison.OrdinalIgnoreCase)
            && string.Equals(other.DivisionId, user.DivisionId, StringComparison.OrdinalIgnoreCase)
            && VerifyStationPin(other, normalizedPin));

        if (duplicate)
        {
            throw new InvalidOperationException("That station PIN is already used in this division.");
        }

        var pinMetadata = PasswordHasher.HashWithMetadata(normalizedPin);
        user.StationPinHash = pinMetadata.hash;
        user.StationPinSalt = pinMetadata.salt;
        user.StationPinIterations = pinMetadata.iterations;
        await _repo.UpdateAsync(user, ct);
    }

    public Task ClearStationPinAsync(string userId, CancellationToken ct = default) =>
        SetStationPinAsync(userId, null, ct);

    public async Task<StationPinValidationResult?> ValidateStationPinAsync(
        string pin,
        string? divisionId,
        bool allowCrossDivisionFallback = true,
        CancellationToken ct = default)
    {
        var normalizedPin = NormalizeStationPin(pin);
        if (!IsValidStationPin(normalizedPin))
        {
            return null;
        }

        var all = await _repo.GetAllAsync(ct);
        var selectedDivisionId = string.IsNullOrWhiteSpace(divisionId) ? null : divisionId.Trim();
        User? employee = null;

        if (!string.IsNullOrWhiteSpace(selectedDivisionId))
        {
            employee = all
                .Where(u => string.Equals(u.DivisionId, selectedDivisionId, StringComparison.OrdinalIgnoreCase))
                .FirstOrDefault(u => VerifyStationPin(u, normalizedPin));

            if (employee == null && allowCrossDivisionFallback)
            {
                var matchesOutsideSelectedDivision = all
                    .Where(u => !string.Equals(u.DivisionId, selectedDivisionId, StringComparison.OrdinalIgnoreCase))
                    .Where(u => VerifyStationPin(u, normalizedPin))
                    .Take(2)
                    .ToList();

                employee = matchesOutsideSelectedDivision.Count == 1 ? matchesOutsideSelectedDivision[0] : null;
            }
        }
        else
        {
            var matches = all
                .Where(u => VerifyStationPin(u, normalizedPin))
                .Take(2)
                .ToList();

            employee = matches.Count == 1 ? matches[0] : null;
        }

        if (employee == null || string.IsNullOrWhiteSpace(employee.DivisionId)) return null;

        var resolvedDivisionId = employee.DivisionId;
        var division = _divisions == null ? null : await _divisions.GetByIdAsync(resolvedDivisionId, ct);
        return new StationPinValidationResult(
            employee,
            division,
            employee.Role >= AccessTier.Supervisor,
            employee.Role >= AccessTier.Admin);
    }

    public async Task<string> ResetPasswordAsync(string userId, string? customPassword = null, CancellationToken ct = default)
    {
        var user = await _repo.GetByIdAsync(userId, ct);
        if (user == null) throw new InvalidOperationException("User not found.");

        var password = string.IsNullOrWhiteSpace(customPassword) ? GenerateTempPassword() : customPassword;
        var passwordMetadata = PasswordHasher.HashWithMetadata(password);
        user.PasswordHash = passwordMetadata.hash;
        user.PasswordSalt = passwordMetadata.salt;
        user.PasswordIterations = passwordMetadata.iterations;
        user.MustChangePassword = true;
        await _repo.UpdateAsync(user, ct);
        return password;
    }

    public Task DeleteAsync(string id, CancellationToken ct = default) => _repo.RemoveAsync(id, ct);

    public static string BuildAvatar(string name)
    {
        var parts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var letters = parts.Take(2).Select(p => p[0]).ToArray();
        return new string(letters).ToUpperInvariant();
    }

    public static string GenerateTempPassword()
    {
        // Exclude characters that are easy to misread when IT relays a temporary password.
        const string upper = "ABCDEFGHJKLMNPQRSTUVWXYZ";
        const string lower = "abcdefghjkmnpqrstuvwxyz";
        const string digits = "23456789";
        const string all = upper + lower + digits;

        var bytes = RandomNumberGenerator.GetBytes(16);
        var chars = new char[16];
        // Keep generated passwords compatible with the minimum composition expected by login policy.
        chars[0] = upper[bytes[0] % upper.Length];
        chars[1] = lower[bytes[1] % lower.Length];
        chars[2] = digits[bytes[2] % digits.Length];
        for (int i = 3; i < 16; i++)
            chars[i] = all[bytes[i] % all.Length];

        // Shuffle the required character classes away from predictable positions.
        var shuffleBytes = RandomNumberGenerator.GetBytes(16);
        for (int i = chars.Length - 1; i > 0; i--)
        {
            int j = shuffleBytes[i] % (i + 1);
            (chars[i], chars[j]) = (chars[j], chars[i]);
        }

        return new string(chars);
    }

    private static string NormalizeStationPin(string pin) => pin.Trim();

    private static bool IsValidStationPin(string pin)
        => pin.Length == 6 && pin.All(char.IsDigit);

    private static bool VerifyStationPin(User user, string pin)
    {
        if (string.IsNullOrWhiteSpace(user.StationPinHash) ||
            string.IsNullOrWhiteSpace(user.StationPinSalt) ||
            user.StationPinIterations <= 0)
        {
            return false;
        }

        try
        {
            return PasswordHasher.Verify(pin, user.StationPinHash, user.StationPinSalt, user.StationPinIterations);
        }
        catch (FormatException)
        {
            return false;
        }
    }
}

/// <summary>
/// What a good station PIN resolves to: the employee, their division, and whether they can do supervisor/admin station moves.
/// </summary>
public sealed record StationPinValidationResult(
    User Employee,
    Division? Division,
    bool HasSupervisorAuthority,
    bool HasAdminAuthority);
