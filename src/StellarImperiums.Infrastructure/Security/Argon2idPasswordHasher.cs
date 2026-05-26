using System.Security.Cryptography;
using System.Text;
using Konscious.Security.Cryptography;
using StellarImperiums.Application.Abstractions;
using StellarImperiums.Domain.Users;

namespace StellarImperiums.Infrastructure.Security;

/// <summary>
/// Argon2id implementation of <see cref="IPasswordHasher"/> following OWASP 2024 recommendations
/// (memory = 64 MiB, iterations = 3, parallelism = 4, 16-byte salt, 32-byte tag).
/// </summary>
/// <remarks>
/// Hashes are serialized to the PHC string format <c>$argon2id$v=19$m=65536,t=3,p=4$&lt;salt&gt;$&lt;hash&gt;</c>
/// where both <c>salt</c> and <c>hash</c> are unpadded base64 (RFC 4648 §3.2 — PHC convention).
/// </remarks>
public sealed class Argon2idPasswordHasher : IPasswordHasher
{
    private const int Version = 19;
    private const int MemorySizeKiB = 65536;
    private const int Iterations = 3;
    private const int DegreeOfParallelism = 4;
    private const int SaltLength = 16;
    private const int HashLength = 32;

    /// <inheritdoc />
    public PasswordHash Hash(string plaintextPassword)
    {
        ArgumentException.ThrowIfNullOrEmpty(plaintextPassword);

        var salt = RandomNumberGenerator.GetBytes(SaltLength);
        var hash = ComputeHash(plaintextPassword, salt);

        var phc =
            $"$argon2id$v={Version}$m={MemorySizeKiB},t={Iterations},p={DegreeOfParallelism}" +
            $"${EncodeUnpaddedBase64(salt)}${EncodeUnpaddedBase64(hash)}";

        return PasswordHash.Create(phc);
    }

    /// <inheritdoc />
    public bool Verify(string plaintextPassword, PasswordHash hash)
    {
        ArgumentException.ThrowIfNullOrEmpty(plaintextPassword);
        ArgumentNullException.ThrowIfNull(hash);

        if (!TryParsePhc(hash.Value, out var parameters, out var salt, out var expectedHash))
        {
            return false;
        }

        var computedHash = ComputeHash(plaintextPassword, salt, parameters);
        return CryptographicOperations.FixedTimeEquals(computedHash, expectedHash);
    }

    private static byte[] ComputeHash(string password, byte[] salt, Argon2Parameters? parameters = null)
    {
        var p = parameters ?? new Argon2Parameters(MemorySizeKiB, Iterations, DegreeOfParallelism);
        using var argon2 = new Argon2id(Encoding.UTF8.GetBytes(password))
        {
            Salt = salt,
            DegreeOfParallelism = p.Parallelism,
            Iterations = p.Iterations,
            MemorySize = p.MemorySizeKiB
        };
        return argon2.GetBytes(HashLength);
    }

    private static string EncodeUnpaddedBase64(byte[] data) =>
        Convert.ToBase64String(data).TrimEnd('=');

    private static byte[] DecodeUnpaddedBase64(string data)
    {
        var padding = (4 - (data.Length % 4)) % 4;
        return Convert.FromBase64String(data + new string('=', padding));
    }

    private static bool TryParsePhc(
        string phc,
        out Argon2Parameters parameters,
        out byte[] salt,
        out byte[] hash)
    {
        parameters = default;
        salt = [];
        hash = [];

        var segments = phc.Split('$');
        if (segments.Length != 6 || segments[1] != "argon2id")
        {
            return false;
        }

        if (segments[2] != $"v={Version}")
        {
            return false;
        }

        var paramsPart = segments[3].Split(',');
        if (paramsPart.Length != 3)
        {
            return false;
        }

        if (!TryParseInt(paramsPart[0], "m=", out var memorySize)
            || !TryParseInt(paramsPart[1], "t=", out var iterations)
            || !TryParseInt(paramsPart[2], "p=", out var parallelism))
        {
            return false;
        }

        try
        {
            salt = DecodeUnpaddedBase64(segments[4]);
            hash = DecodeUnpaddedBase64(segments[5]);
        }
        catch (FormatException)
        {
            return false;
        }

        parameters = new Argon2Parameters(memorySize, iterations, parallelism);
        return true;
    }

    private static bool TryParseInt(string segment, string prefix, out int value)
    {
        value = 0;
        return segment.StartsWith(prefix, StringComparison.Ordinal)
            && int.TryParse(segment.AsSpan(prefix.Length), out value);
    }

    private readonly record struct Argon2Parameters(int MemorySizeKiB, int Iterations, int Parallelism);
}
