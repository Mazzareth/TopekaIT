using System.Security.Cryptography;
using TopekaIT.Core.Domain.Entities;
using TopekaIT.Core.Domain.Enums;
using TopekaIT.Core.Ports;

namespace TopekaIT.Core.Services;

public class UserService
{
    private readonly IUserRepository _repo;

    public UserService(IUserRepository repo)
    {
        _repo = repo;
    }

    public Task<IReadOnlyList<User>> GetAllAsync(CancellationToken ct = default) => _repo.GetAllAsync(ct);

    public Task<User?> GetByIdAsync(string id, CancellationToken ct = default) => _repo.GetByIdAsync(id, ct);

    public async Task<User?> ValidateCredentialsAsync(string username, string password, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrEmpty(password)) return null;
        var user = await _repo.GetByUsernameAsync(username.Trim(), ct);
        if (user == null) return null;
        return PasswordHasher.Verify(password, user.PasswordHash, user.PasswordSalt) ? user : null;
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
        var (hash, salt) = PasswordHasher.Hash(string.IsNullOrEmpty(password) ? "changeme" : password);
        var user = new User
        {
            Id = id,
            Name = name,
            Username = username,
            Role = role,
            Avatar = avatar,
            PasswordHash = hash,
            PasswordSalt = salt,
            DivisionId = divisionId,
        };
        await _repo.AddAsync(user, ct);
        return user;
    }

    public async Task UpdateAsync(User user, string? newPassword, CancellationToken ct = default)
    {
        if (!string.IsNullOrEmpty(newPassword))
        {
            var (hash, salt) = PasswordHasher.Hash(newPassword);
            user.PasswordHash = hash;
            user.PasswordSalt = salt;
        }
        user.Avatar = BuildAvatar(user.Name);
        await _repo.UpdateAsync(user, ct);
    }

    public async Task SetAuditAsync(string userId, bool value, CancellationToken ct = default)
    {
        var user = await _repo.GetByIdAsync(userId, ct);
        if (user == null) return;
        user.Audit = value;
        await _repo.UpdateAsync(user, ct);
    }

    /// <summary>
    /// Resets a user's password to a randomly generated temporary password.
    /// Returns the plaintext temp password so IT can share it with the user.
    /// </summary>
    public async Task<string> ResetPasswordAsync(string userId, string? customPassword = null, CancellationToken ct = default)
    {
        var user = await _repo.GetByIdAsync(userId, ct);
        if (user == null) throw new InvalidOperationException("User not found.");

        var password = string.IsNullOrWhiteSpace(customPassword) ? GenerateTempPassword() : customPassword;
        var (hash, salt) = PasswordHasher.Hash(password);
        user.PasswordHash = hash;
        user.PasswordSalt = salt;
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
        const string upper = "ABCDEFGHJKLMNPQRSTUVWXYZ"; // no I/O ambiguity
        const string lower = "abcdefghjkmnpqrstuvwxyz";  // no i/l/o ambiguity
        const string digits = "23456789";                 // no 0/1 ambiguity
        const string all = upper + lower + digits;

        var bytes = RandomNumberGenerator.GetBytes(16);
        var chars = new char[16];
        // Guarantee at least one upper, one lower, one digit
        chars[0] = upper[bytes[0] % upper.Length];
        chars[1] = lower[bytes[1] % lower.Length];
        chars[2] = digits[bytes[2] % digits.Length];
        for (int i = 3; i < 16; i++)
            chars[i] = all[bytes[i] % all.Length];

        // Shuffle using Fisher-Yates with the remaining random bytes
        var shuffleBytes = RandomNumberGenerator.GetBytes(16);
        for (int i = chars.Length - 1; i > 0; i--)
        {
            int j = shuffleBytes[i] % (i + 1);
            (chars[i], chars[j]) = (chars[j], chars[i]);
        }

        return new string(chars);
    }
}
