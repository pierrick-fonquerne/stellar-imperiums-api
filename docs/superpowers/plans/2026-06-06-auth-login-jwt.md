# JWT Authentication (login, refresh, logout, me) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Compléter le cycle d'authentification de l'API Stellar Imperiums : `POST /auth/login` (email + mot de passe → access JWT 15 min + refresh cookie 30 j), `POST /auth/refresh` (rotation avec détection de réutilisation par famille), `POST /auth/logout` (révocation), `GET /users/me` protégé, rate limiting sur le login, tests d'intégration Testcontainers.

**Architecture:** Clean Architecture existante. `RefreshToken` est une entité Rich Domain (invariants de rotation/révocation). Le JWT est généré par `JwtTokenService` (Infrastructure) derrière `ITokenService` (Application). Les handlers Wolverine suivent le pattern `RegisterUserHandler` existant. Le refresh token voyage en cookie `HttpOnly` ; seul son hash SHA-256 est persisté (table `jeton_rafraichissement`, convention française du schéma partagé).

**Tech Stack:** .NET 10, EF Core + Npgsql, Wolverine + FluentValidation, `Microsoft.IdentityModel.JsonWebTokens` (HS256), `Microsoft.AspNetCore.Authentication.JwtBearer`, `Microsoft.AspNetCore.RateLimiting`, xUnit + NSubstitute + Shouldly, Testcontainers.PostgreSql.

**Spec:** `docs/superpowers/specs/2026-06-06-auth-login-jwt-design.md`
**Branche:** `feature/auth-login-jwt` (depuis `develop`)
**Répertoire de travail:** racine du repo `api/`

**Conventions impératives (CLAUDE.md):**
- Commentaires XML en anglais sur tout membre public des projets `src/` ; AUCUN commentaire inline.
- Aucune mention d'IA dans les commits.
- Avant chaque commit : `dotnet build` + tests du projet touché verts.

---

## File Structure

```
src/StellarImperiums.Domain/
  Tokens/RefreshToken.cs                            (créer — entité)
  Tokens/Exceptions/InvalidRefreshTokenException.cs (créer)
  Tokens/Exceptions/RefreshTokenExpiredException.cs (créer)
  Tokens/Exceptions/RefreshTokenRevokedException.cs (créer)
  Tokens/Exceptions/RefreshTokenReuseException.cs   (créer)
src/StellarImperiums.Application/
  Abstractions/ITokenService.cs                     (créer — + records résultat)
  Abstractions/IRefreshTokenRepository.cs           (créer)
  Abstractions/IUserRepository.cs                   (modifier — +2 méthodes)
  Users/Commands/LoginUserCommand.cs                (créer)
  Users/Commands/LoginUserResult.cs                 (créer)
  Users/Commands/LoginUserValidator.cs              (créer)
  Users/Commands/LoginUserHandler.cs                (créer)
  Users/Queries/GetCurrentUserQuery.cs              (créer)
  Users/Queries/CurrentUserResult.cs                (créer)
  Users/Queries/GetCurrentUserHandler.cs            (créer)
  Users/Exceptions/InvalidCredentialsException.cs   (créer)
  Users/Exceptions/UserSuspendedException.cs        (créer)
  Users/Exceptions/UserNotFoundException.cs         (créer)
  Tokens/Commands/RefreshTokenCommand.cs            (créer)
  Tokens/Commands/RefreshTokenResult.cs             (créer)
  Tokens/Commands/RefreshTokenHandler.cs            (créer)
  Tokens/Commands/LogoutCommand.cs                  (créer)
  Tokens/Commands/LogoutHandler.cs                  (créer)
  Tokens/Exceptions/RefreshTokenRejectedException.cs (créer)
src/StellarImperiums.Infrastructure/
  Security/JwtOptions.cs                            (créer)
  Security/JwtTokenService.cs                       (créer)
  Persistence/StellarDbContext.cs                   (modifier — DbSet)
  Persistence/Configurations/RefreshTokenConfiguration.cs (créer)
  Persistence/Repositories/EfUserRepository.cs      (modifier — +2 méthodes)
  Persistence/Repositories/EfRefreshTokenRepository.cs (créer)
  Persistence/Migrations/<timestamp>_AddRefreshTokens.cs (générer)
  DependencyInjection.cs                            (modifier)
src/StellarImperiums.Api/
  Contracts/Auth/LoginRequest.cs                    (créer)
  Contracts/Auth/LoginResponse.cs                   (créer — + UserSummary)
  Contracts/Auth/RefreshResponse.cs                 (créer)
  Contracts/Users/MeResponse.cs                     (créer)
  Controllers/V1/AuthController.cs                  (modifier — 3 endpoints + cookies)
  Controllers/V1/UsersController.cs                 (créer)
  Middleware/DomainExceptionMiddleware.cs           (modifier — 4 catches)
  Program.cs                                        (modifier — JwtBearer, rate limiter)
  appsettings.json                                  (modifier — section Jwt)
tests/StellarImperiums.Domain.Tests/Tokens/RefreshTokenTests.cs        (créer)
tests/StellarImperiums.Application.Tests/Users/Commands/LoginUserHandlerTests.cs   (créer)
tests/StellarImperiums.Application.Tests/Users/Commands/LoginUserValidatorTests.cs (créer)
tests/StellarImperiums.Application.Tests/Users/Queries/GetCurrentUserHandlerTests.cs (créer)
tests/StellarImperiums.Application.Tests/Tokens/Commands/RefreshTokenHandlerTests.cs (créer)
tests/StellarImperiums.Application.Tests/Tokens/Commands/LogoutHandlerTests.cs     (créer)
tests/StellarImperiums.Infrastructure.Tests/Security/JwtTokenServiceTests.cs       (créer)
tests/StellarImperiums.Api.IntegrationTests/StellarApiFactory.cs       (créer)
tests/StellarImperiums.Api.IntegrationTests/AuthFlowTests.cs           (créer)
tests/StellarImperiums.Api.IntegrationTests/RateLimitingTests.cs       (créer)
```

---

### Task 1: Domain — entité `RefreshToken` et ses exceptions

**Files:**
- Create: `src/StellarImperiums.Domain/Tokens/Exceptions/InvalidRefreshTokenException.cs`
- Create: `src/StellarImperiums.Domain/Tokens/Exceptions/RefreshTokenExpiredException.cs`
- Create: `src/StellarImperiums.Domain/Tokens/Exceptions/RefreshTokenRevokedException.cs`
- Create: `src/StellarImperiums.Domain/Tokens/Exceptions/RefreshTokenReuseException.cs`
- Create: `src/StellarImperiums.Domain/Tokens/RefreshToken.cs`
- Test: `tests/StellarImperiums.Domain.Tests/Tokens/RefreshTokenTests.cs`

- [ ] **Step 1: Écrire les tests qui échouent**

Créer `tests/StellarImperiums.Domain.Tests/Tokens/RefreshTokenTests.cs` :

```csharp
using Shouldly;
using StellarImperiums.Domain.Tokens;
using StellarImperiums.Domain.Tokens.Exceptions;

namespace StellarImperiums.Domain.Tests.Tokens;

public class RefreshTokenTests
{
    private const string Hash = "A1B2C3D4E5F60718293A4B5C6D7E8F90A1B2C3D4E5F60718293A4B5C6D7E8F90";
    private const string NewHash = "00112233445566778899AABBCCDDEEFF00112233445566778899AABBCCDDEEFF";

    private static readonly DateTimeOffset Now = new(2026, 6, 6, 12, 0, 0, TimeSpan.Zero);
    private static readonly Guid Family = Guid.Parse("0193b6a0-0000-7000-8000-000000000001");

    private static RefreshToken CreateToken(
        DateTimeOffset? expiresAt = null,
        DateTimeOffset? createdAt = null) =>
        RefreshToken.Create(42, Hash, Family, expiresAt ?? Now.AddDays(30), createdAt ?? Now);

    [Fact]
    public void Create_WithValidArguments_SetsProperties()
    {
        var token = CreateToken();

        token.UserId.ShouldBe(42);
        token.TokenHash.ShouldBe(Hash);
        token.FamilyId.ShouldBe(Family);
        token.CreatedAt.ShouldBe(Now);
        token.ExpiresAt.ShouldBe(Now.AddDays(30));
        token.RevokedAt.ShouldBeNull();
        token.ReplacedByTokenHash.ShouldBeNull();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithEmptyHash_Throws(string emptyHash)
    {
        Should.Throw<InvalidRefreshTokenException>(
            () => RefreshToken.Create(42, emptyHash, Family, Now.AddDays(30), Now));
    }

    [Fact]
    public void Create_WithEmptyFamilyId_Throws()
    {
        Should.Throw<InvalidRefreshTokenException>(
            () => RefreshToken.Create(42, Hash, Guid.Empty, Now.AddDays(30), Now));
    }

    [Fact]
    public void Create_WithExpiryNotAfterCreation_Throws()
    {
        Should.Throw<InvalidRefreshTokenException>(
            () => RefreshToken.Create(42, Hash, Family, Now, Now));
    }

    [Fact]
    public void IsActive_WhenFreshlyCreated_IsTrue()
    {
        CreateToken().IsActive(Now.AddDays(1)).ShouldBeTrue();
    }

    [Fact]
    public void IsActive_WhenExpired_IsFalse()
    {
        var token = CreateToken(expiresAt: Now.AddDays(1));

        token.IsExpired(Now.AddDays(2)).ShouldBeTrue();
        token.IsActive(Now.AddDays(2)).ShouldBeFalse();
    }

    [Fact]
    public void IsActive_WhenRevoked_IsFalse()
    {
        var token = CreateToken();
        token.Revoke(Now.AddHours(1));

        token.IsRevoked.ShouldBeTrue();
        token.IsActive(Now.AddHours(2)).ShouldBeFalse();
    }

    [Fact]
    public void IsActive_WhenRotated_IsFalse()
    {
        var token = CreateToken();
        token.Rotate(NewHash, Now.AddDays(31), Now.AddDays(1));

        token.IsRotated.ShouldBeTrue();
        token.IsActive(Now.AddDays(1)).ShouldBeFalse();
    }

    [Fact]
    public void Rotate_ReturnsSuccessorInSameFamily_AndMarksCurrentAsRotated()
    {
        var token = CreateToken();

        var successor = token.Rotate(NewHash, Now.AddDays(31), Now.AddDays(1));

        successor.TokenHash.ShouldBe(NewHash);
        successor.FamilyId.ShouldBe(Family);
        successor.UserId.ShouldBe(42);
        successor.CreatedAt.ShouldBe(Now.AddDays(1));
        successor.ExpiresAt.ShouldBe(Now.AddDays(31));
        token.ReplacedByTokenHash.ShouldBe(NewHash);
    }

    [Fact]
    public void Rotate_WhenRevoked_Throws()
    {
        var token = CreateToken();
        token.Revoke(Now.AddHours(1));

        Should.Throw<RefreshTokenRevokedException>(
            () => token.Rotate(NewHash, Now.AddDays(31), Now.AddDays(1)));
    }

    [Fact]
    public void Rotate_WhenAlreadyRotated_Throws()
    {
        var token = CreateToken();
        token.Rotate(NewHash, Now.AddDays(31), Now.AddDays(1));

        Should.Throw<RefreshTokenReuseException>(
            () => token.Rotate(NewHash, Now.AddDays(31), Now.AddDays(2)));
    }

    [Fact]
    public void Rotate_WhenExpired_Throws()
    {
        var token = CreateToken(expiresAt: Now.AddDays(1));

        Should.Throw<RefreshTokenExpiredException>(
            () => token.Rotate(NewHash, Now.AddDays(31), Now.AddDays(2)));
    }

    [Fact]
    public void Revoke_SetsRevokedAt()
    {
        var token = CreateToken();

        token.Revoke(Now.AddHours(1));

        token.RevokedAt.ShouldBe(Now.AddHours(1));
    }

    [Fact]
    public void Revoke_CalledTwice_KeepsFirstTimestamp()
    {
        var token = CreateToken();
        token.Revoke(Now.AddHours(1));

        token.Revoke(Now.AddHours(5));

        token.RevokedAt.ShouldBe(Now.AddHours(1));
    }
}
```

Vérifier que `tests/StellarImperiums.Domain.Tests/StellarImperiums.Domain.Tests.csproj` référence `Shouldly` ; sinon : `dotnet add tests/StellarImperiums.Domain.Tests package Shouldly`.

- [ ] **Step 2: Vérifier que la compilation échoue**

Run: `dotnet build tests/StellarImperiums.Domain.Tests`
Expected: FAIL — `RefreshToken` et le namespace `StellarImperiums.Domain.Tokens` n'existent pas.

- [ ] **Step 3: Implémenter les exceptions**

Créer `src/StellarImperiums.Domain/Tokens/Exceptions/InvalidRefreshTokenException.cs` :

```csharp
using StellarImperiums.Domain.Common;

namespace StellarImperiums.Domain.Tokens.Exceptions;

/// <summary>
/// Thrown when a refresh token cannot be created because an argument violates a domain invariant.
/// </summary>
public sealed class InvalidRefreshTokenException : DomainException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidRefreshTokenException"/> class.
    /// </summary>
    /// <param name="message">The reason the refresh token was rejected.</param>
    public InvalidRefreshTokenException(string message)
        : base(message)
    {
    }
}
```

Créer `src/StellarImperiums.Domain/Tokens/Exceptions/RefreshTokenExpiredException.cs` :

```csharp
using StellarImperiums.Domain.Common;

namespace StellarImperiums.Domain.Tokens.Exceptions;

/// <summary>
/// Thrown when an operation is attempted on a refresh token past its expiry.
/// </summary>
public sealed class RefreshTokenExpiredException : DomainException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RefreshTokenExpiredException"/> class.
    /// </summary>
    public RefreshTokenExpiredException()
        : base("The refresh token has expired.")
    {
    }
}
```

Créer `src/StellarImperiums.Domain/Tokens/Exceptions/RefreshTokenRevokedException.cs` :

```csharp
using StellarImperiums.Domain.Common;

namespace StellarImperiums.Domain.Tokens.Exceptions;

/// <summary>
/// Thrown when an operation is attempted on a revoked refresh token.
/// </summary>
public sealed class RefreshTokenRevokedException : DomainException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RefreshTokenRevokedException"/> class.
    /// </summary>
    public RefreshTokenRevokedException()
        : base("The refresh token has been revoked.")
    {
    }
}
```

Créer `src/StellarImperiums.Domain/Tokens/Exceptions/RefreshTokenReuseException.cs` :

```csharp
using StellarImperiums.Domain.Common;

namespace StellarImperiums.Domain.Tokens.Exceptions;

/// <summary>
/// Thrown when a refresh token that was already rotated is presented again, which signals a possible theft.
/// </summary>
public sealed class RefreshTokenReuseException : DomainException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RefreshTokenReuseException"/> class.
    /// </summary>
    public RefreshTokenReuseException()
        : base("The refresh token has already been rotated.")
    {
    }
}
```

- [ ] **Step 4: Implémenter l'entité**

Créer `src/StellarImperiums.Domain/Tokens/RefreshToken.cs` :

```csharp
using StellarImperiums.Domain.Common;
using StellarImperiums.Domain.Tokens.Exceptions;

namespace StellarImperiums.Domain.Tokens;

/// <summary>
/// Entity representing one link of a refresh token rotation chain for a user session.
/// </summary>
/// <remarks>
/// Only the SHA-256 hash of the opaque token value is stored. Tokens issued by successive
/// rotations share the same <see cref="FamilyId"/>, which allows revoking the whole chain
/// when a rotated token is replayed. The parameterless constructor exists solely for
/// Entity Framework Core materialization.
/// </remarks>
public sealed class RefreshToken : Entity
{
    /// <summary>
    /// Gets the identifier of the user owning this token.
    /// </summary>
    public int UserId { get; private set; }

    /// <summary>
    /// Gets the SHA-256 hash (uppercase hexadecimal) of the opaque token value.
    /// </summary>
    public string TokenHash { get; private set; }

    /// <summary>
    /// Gets the identifier shared by every token of the same rotation chain.
    /// </summary>
    public Guid FamilyId { get; private set; }

    /// <summary>
    /// Gets the creation timestamp (UTC).
    /// </summary>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>
    /// Gets the expiry timestamp (UTC).
    /// </summary>
    public DateTimeOffset ExpiresAt { get; private set; }

    /// <summary>
    /// Gets the revocation timestamp (UTC), or <c>null</c> if the token was never revoked.
    /// </summary>
    public DateTimeOffset? RevokedAt { get; private set; }

    /// <summary>
    /// Gets the hash of the successor token, or <c>null</c> if this token was never rotated.
    /// </summary>
    public string? ReplacedByTokenHash { get; private set; }

    /// <summary>
    /// Gets a value indicating whether the token has been revoked.
    /// </summary>
    public bool IsRevoked => RevokedAt is not null;

    /// <summary>
    /// Gets a value indicating whether the token has been rotated to a successor.
    /// </summary>
    public bool IsRotated => ReplacedByTokenHash is not null;

    private RefreshToken()
    {
        TokenHash = null!;
    }

    /// <summary>
    /// Creates a new <see cref="RefreshToken"/> in the active state.
    /// </summary>
    /// <param name="userId">The identifier of the owning user.</param>
    /// <param name="tokenHash">The SHA-256 hash of the opaque token value.</param>
    /// <param name="familyId">The rotation chain identifier.</param>
    /// <param name="expiresAt">The expiry timestamp (must be after <paramref name="createdAt"/>).</param>
    /// <param name="createdAt">The creation timestamp (defaults to the current UTC time).</param>
    /// <returns>The newly created token.</returns>
    /// <exception cref="InvalidRefreshTokenException">
    /// Thrown when the hash is empty, the family identifier is empty, or the expiry is not after the creation time.
    /// </exception>
    public static RefreshToken Create(
        int userId,
        string tokenHash,
        Guid familyId,
        DateTimeOffset expiresAt,
        DateTimeOffset? createdAt = null)
    {
        if (string.IsNullOrWhiteSpace(tokenHash))
        {
            throw new InvalidRefreshTokenException("Token hash cannot be empty.");
        }

        if (familyId == Guid.Empty)
        {
            throw new InvalidRefreshTokenException("Family id cannot be empty.");
        }

        var creation = createdAt ?? DateTimeOffset.UtcNow;
        if (expiresAt <= creation)
        {
            throw new InvalidRefreshTokenException("Expiry must be after the creation time.");
        }

        return new RefreshToken
        {
            UserId = userId,
            TokenHash = tokenHash,
            FamilyId = familyId,
            CreatedAt = creation,
            ExpiresAt = expiresAt
        };
    }

    /// <summary>
    /// Indicates whether the token is expired at the given instant.
    /// </summary>
    /// <param name="now">The instant to evaluate against (UTC).</param>
    /// <returns><c>true</c> when the token is expired.</returns>
    public bool IsExpired(DateTimeOffset now) => now >= ExpiresAt;

    /// <summary>
    /// Indicates whether the token can still be used at the given instant.
    /// </summary>
    /// <param name="now">The instant to evaluate against (UTC).</param>
    /// <returns><c>true</c> when the token is neither revoked, rotated, nor expired.</returns>
    public bool IsActive(DateTimeOffset now) => !IsRevoked && !IsRotated && !IsExpired(now);

    /// <summary>
    /// Rotates the token: marks it as replaced and returns its successor in the same family.
    /// </summary>
    /// <param name="newTokenHash">The hash of the successor token.</param>
    /// <param name="newExpiresAt">The expiry of the successor token.</param>
    /// <param name="now">The rotation instant (UTC).</param>
    /// <returns>The successor token.</returns>
    /// <exception cref="RefreshTokenRevokedException">Thrown when the token is revoked.</exception>
    /// <exception cref="RefreshTokenReuseException">Thrown when the token was already rotated.</exception>
    /// <exception cref="RefreshTokenExpiredException">Thrown when the token is expired.</exception>
    public RefreshToken Rotate(string newTokenHash, DateTimeOffset newExpiresAt, DateTimeOffset now)
    {
        if (IsRevoked)
        {
            throw new RefreshTokenRevokedException();
        }

        if (IsRotated)
        {
            throw new RefreshTokenReuseException();
        }

        if (IsExpired(now))
        {
            throw new RefreshTokenExpiredException();
        }

        var successor = Create(UserId, newTokenHash, FamilyId, newExpiresAt, now);
        ReplacedByTokenHash = newTokenHash;
        return successor;
    }

    /// <summary>
    /// Revokes the token. Calling this method on an already revoked token keeps the original timestamp.
    /// </summary>
    /// <param name="when">The revocation instant (UTC).</param>
    public void Revoke(DateTimeOffset when)
    {
        if (RevokedAt is null)
        {
            RevokedAt = when;
        }
    }
}
```

- [ ] **Step 5: Vérifier que les tests passent**

Run: `dotnet test tests/StellarImperiums.Domain.Tests --filter "FullyQualifiedName~RefreshTokenTests"`
Expected: PASS — 15 tests verts.

- [ ] **Step 6: Commit**

```bash
git add src/StellarImperiums.Domain/Tokens tests/StellarImperiums.Domain.Tests/Tokens
git commit -m "feat(domain): refresh token entity with rotation and revocation invariants"
```

---

### Task 2: Application — abstractions (`ITokenService`, `IRefreshTokenRepository`, extension `IUserRepository`)

**Files:**
- Create: `src/StellarImperiums.Application/Abstractions/ITokenService.cs`
- Create: `src/StellarImperiums.Application/Abstractions/IRefreshTokenRepository.cs`
- Modify: `src/StellarImperiums.Application/Abstractions/IUserRepository.cs`
- Modify: `src/StellarImperiums.Infrastructure/Persistence/Repositories/EfUserRepository.cs`

Pas de test dédié ici : les interfaces sont exercées par les tests des handlers (Tasks 4-7) et les méthodes EF par les tests d'intégration (Task 12).

- [ ] **Step 1: Créer `ITokenService` et ses records de résultat**

Créer `src/StellarImperiums.Application/Abstractions/ITokenService.cs` :

```csharp
using StellarImperiums.Domain.Users;

namespace StellarImperiums.Application.Abstractions;

/// <summary>
/// Issues access tokens and opaque refresh token material for authenticated users.
/// </summary>
public interface ITokenService
{
    /// <summary>
    /// Creates a signed access token carrying the user's identity claims.
    /// </summary>
    /// <param name="user">The authenticated user.</param>
    /// <returns>The serialized token and its lifetime in seconds.</returns>
    AccessTokenResult CreateAccessToken(User user);

    /// <summary>
    /// Generates a new cryptographically random refresh token.
    /// </summary>
    /// <returns>The opaque value to hand to the client, its hash to persist, and its expiry.</returns>
    RefreshTokenMaterial GenerateRefreshToken();

    /// <summary>
    /// Computes the persistence hash of an opaque refresh token value received from a client.
    /// </summary>
    /// <param name="refreshTokenValue">The opaque refresh token value.</param>
    /// <returns>The SHA-256 hash in uppercase hexadecimal.</returns>
    string HashRefreshToken(string refreshTokenValue);
}

/// <summary>
/// Result of an access token creation.
/// </summary>
/// <param name="Token">The serialized signed token.</param>
/// <param name="ExpiresInSeconds">The token lifetime in seconds.</param>
public sealed record AccessTokenResult(string Token, int ExpiresInSeconds);

/// <summary>
/// Material produced when generating a refresh token.
/// </summary>
/// <param name="Value">The opaque value handed to the client (never persisted).</param>
/// <param name="Hash">The SHA-256 hash persisted in place of the value.</param>
/// <param name="ExpiresAt">The expiry timestamp (UTC).</param>
public sealed record RefreshTokenMaterial(string Value, string Hash, DateTimeOffset ExpiresAt);
```

- [ ] **Step 2: Créer `IRefreshTokenRepository`**

Créer `src/StellarImperiums.Application/Abstractions/IRefreshTokenRepository.cs` :

```csharp
using StellarImperiums.Domain.Tokens;

namespace StellarImperiums.Application.Abstractions;

/// <summary>
/// Persistence abstraction for the <see cref="RefreshToken"/> entity.
/// </summary>
/// <remarks>
/// Lookups track the returned entity so that domain state transitions (rotation, revocation)
/// are persisted by <see cref="SaveChangesAsync"/>.
/// </remarks>
public interface IRefreshTokenRepository
{
    /// <summary>
    /// Adds a new <see cref="RefreshToken"/> to the persistence context (no commit).
    /// </summary>
    /// <param name="token">The token to add.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    Task AddAsync(RefreshToken token, CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds a refresh token by the hash of its opaque value.
    /// </summary>
    /// <param name="tokenHash">The SHA-256 hash to look up.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The tracked token, or <c>null</c> when no token matches.</returns>
    Task<RefreshToken?> GetByHashAsync(string tokenHash, CancellationToken cancellationToken = default);

    /// <summary>
    /// Revokes every non-revoked token of the given rotation family (no commit).
    /// </summary>
    /// <param name="familyId">The rotation chain identifier.</param>
    /// <param name="when">The revocation instant (UTC).</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    Task RevokeFamilyAsync(Guid familyId, DateTimeOffset when, CancellationToken cancellationToken = default);

    /// <summary>
    /// Persists pending changes to the underlying store.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token.</param>
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
```

- [ ] **Step 3: Étendre `IUserRepository`**

Dans `src/StellarImperiums.Application/Abstractions/IUserRepository.cs`, ajouter ces deux méthodes avant `SaveChangesAsync` :

```csharp
    /// <summary>
    /// Finds a user by email address.
    /// </summary>
    /// <param name="email">The email address to look up.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The tracked user, or <c>null</c> when no user matches.</returns>
    Task<User?> GetByEmailAsync(Email email, CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds a user by identifier.
    /// </summary>
    /// <param name="id">The user identifier.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The tracked user, or <c>null</c> when no user matches.</returns>
    Task<User?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
```

- [ ] **Step 4: Implémenter dans `EfUserRepository`**

Dans `src/StellarImperiums.Infrastructure/Persistence/Repositories/EfUserRepository.cs`, ajouter avant `SaveChangesAsync` :

```csharp
    /// <inheritdoc />
    public Task<User?> GetByEmailAsync(Email email, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(email);
        return dbContext.Users
            .FirstOrDefaultAsync(u => u.Email == email, cancellationToken);
    }

    /// <inheritdoc />
    public Task<User?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        dbContext.Users.FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
```

Note : pas de `AsNoTracking` — le login mute l'utilisateur (`RecordLogin`) puis sauvegarde.

- [ ] **Step 5: Vérifier la compilation de la solution**

Run: `dotnet build`
Expected: PASS — 0 erreur (les nouveaux membres d'interface sont implémentés).

- [ ] **Step 6: Commit**

```bash
git add src/StellarImperiums.Application/Abstractions src/StellarImperiums.Infrastructure/Persistence/Repositories/EfUserRepository.cs
git commit -m "feat(app): token service and refresh token repository abstractions"
```

---

### Task 3: Application — exceptions et `LoginUserValidator`

**Files:**
- Create: `src/StellarImperiums.Application/Users/Exceptions/InvalidCredentialsException.cs`
- Create: `src/StellarImperiums.Application/Users/Exceptions/UserSuspendedException.cs`
- Create: `src/StellarImperiums.Application/Users/Exceptions/UserNotFoundException.cs`
- Create: `src/StellarImperiums.Application/Tokens/Exceptions/RefreshTokenRejectedException.cs`
- Create: `src/StellarImperiums.Application/Users/Commands/LoginUserCommand.cs`
- Create: `src/StellarImperiums.Application/Users/Commands/LoginUserValidator.cs`
- Test: `tests/StellarImperiums.Application.Tests/Users/Commands/LoginUserValidatorTests.cs`

- [ ] **Step 1: Écrire les tests du validator qui échouent**

Créer `tests/StellarImperiums.Application.Tests/Users/Commands/LoginUserValidatorTests.cs` :

```csharp
using Shouldly;
using StellarImperiums.Application.Users.Commands;

namespace StellarImperiums.Application.Tests.Users.Commands;

public class LoginUserValidatorTests
{
    private readonly LoginUserValidator _validator = new();

    [Fact]
    public void Validate_WithValidCommand_Passes()
    {
        var result = _validator.Validate(new LoginUserCommand("vega@stellar.io", "P@ssword12345"));

        result.IsValid.ShouldBeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-an-email")]
    public void Validate_WithInvalidEmail_Fails(string email)
    {
        var result = _validator.Validate(new LoginUserCommand(email, "P@ssword12345"));

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(LoginUserCommand.Email));
    }

    [Fact]
    public void Validate_WithEmptyPassword_Fails()
    {
        var result = _validator.Validate(new LoginUserCommand("vega@stellar.io", ""));

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(LoginUserCommand.Password));
    }

    [Fact]
    public void Validate_WithOverlongPassword_Fails()
    {
        var result = _validator.Validate(
            new LoginUserCommand("vega@stellar.io", new string('a', 129)));

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(LoginUserCommand.Password));
    }

    [Fact]
    public void Validate_DoesNotEnforceMinimumPasswordLength()
    {
        var result = _validator.Validate(new LoginUserCommand("vega@stellar.io", "abc"));

        result.IsValid.ShouldBeTrue();
    }
}
```

- [ ] **Step 2: Vérifier que la compilation échoue**

Run: `dotnet build tests/StellarImperiums.Application.Tests`
Expected: FAIL — `LoginUserCommand` et `LoginUserValidator` n'existent pas.

- [ ] **Step 3: Implémenter exceptions, command et validator**

Créer `src/StellarImperiums.Application/Users/Exceptions/InvalidCredentialsException.cs` :

```csharp
namespace StellarImperiums.Application.Users.Exceptions;

/// <summary>
/// Thrown when a login attempt fails, without revealing whether the email or the password was wrong.
/// </summary>
public sealed class InvalidCredentialsException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidCredentialsException"/> class.
    /// </summary>
    public InvalidCredentialsException()
        : base("Invalid email or password.")
    {
    }
}
```

Créer `src/StellarImperiums.Application/Users/Exceptions/UserSuspendedException.cs` :

```csharp
namespace StellarImperiums.Application.Users.Exceptions;

/// <summary>
/// Thrown when a suspended user attempts to authenticate or refresh a session.
/// </summary>
public sealed class UserSuspendedException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UserSuspendedException"/> class.
    /// </summary>
    public UserSuspendedException()
        : base("This account is suspended.")
    {
    }
}
```

Créer `src/StellarImperiums.Application/Users/Exceptions/UserNotFoundException.cs` :

```csharp
namespace StellarImperiums.Application.Users.Exceptions;

/// <summary>
/// Thrown when the user referenced by a valid credential no longer exists.
/// </summary>
public sealed class UserNotFoundException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UserNotFoundException"/> class.
    /// </summary>
    public UserNotFoundException()
        : base("User not found.")
    {
    }
}
```

Créer `src/StellarImperiums.Application/Tokens/Exceptions/RefreshTokenRejectedException.cs` :

```csharp
namespace StellarImperiums.Application.Tokens.Exceptions;

/// <summary>
/// Thrown when a presented refresh token is unknown, expired, revoked, or replayed.
/// The message is intentionally generic so the API never reveals which check failed.
/// </summary>
public sealed class RefreshTokenRejectedException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RefreshTokenRejectedException"/> class.
    /// </summary>
    public RefreshTokenRejectedException()
        : base("Invalid or expired refresh token.")
    {
    }
}
```

Créer `src/StellarImperiums.Application/Users/Commands/LoginUserCommand.cs` :

```csharp
namespace StellarImperiums.Application.Users.Commands;

/// <summary>
/// Command issued to authenticate a player with email and password.
/// </summary>
/// <param name="Email">The account email address.</param>
/// <param name="Password">The plaintext password (never persisted).</param>
public sealed record LoginUserCommand(string Email, string Password);
```

Créer `src/StellarImperiums.Application/Users/Commands/LoginUserValidator.cs` :

```csharp
using FluentValidation;
using StellarImperiums.Domain.Users;

namespace StellarImperiums.Application.Users.Commands;

/// <summary>
/// FluentValidation rules applied to <see cref="LoginUserCommand"/> before the handler runs.
/// </summary>
/// <remarks>
/// No minimum password length is enforced here: revealing the password policy on the login
/// endpoint would leak information. The maximum guards against denial-of-service through
/// oversized argon2 inputs.
/// </remarks>
public sealed class LoginUserValidator : AbstractValidator<LoginUserCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LoginUserValidator"/> class.
    /// </summary>
    public LoginUserValidator()
    {
        RuleFor(c => c.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Email is not a valid email address.")
            .MaximumLength(Email.MaxLength).WithMessage($"Email must be at most {Email.MaxLength} characters.");

        RuleFor(c => c.Password)
            .NotEmpty().WithMessage("Password is required.")
            .MaximumLength(RegisterUserValidator.MaxPasswordLength)
            .WithMessage($"Password must be at most {RegisterUserValidator.MaxPasswordLength} characters.");
    }
}
```

- [ ] **Step 4: Vérifier que les tests passent**

Run: `dotnet test tests/StellarImperiums.Application.Tests --filter "FullyQualifiedName~LoginUserValidatorTests"`
Expected: PASS — 6 tests verts.

- [ ] **Step 5: Commit**

```bash
git add src/StellarImperiums.Application tests/StellarImperiums.Application.Tests/Users/Commands/LoginUserValidatorTests.cs
git commit -m "feat(app): login command, validator and authentication exceptions"
```

---

### Task 4: Application — `LoginUserHandler`

**Files:**
- Create: `src/StellarImperiums.Application/Users/Commands/LoginUserResult.cs`
- Create: `src/StellarImperiums.Application/Users/Commands/LoginUserHandler.cs`
- Test: `tests/StellarImperiums.Application.Tests/Users/Commands/LoginUserHandlerTests.cs`

- [ ] **Step 1: Écrire les tests qui échouent**

Créer `tests/StellarImperiums.Application.Tests/Users/Commands/LoginUserHandlerTests.cs` :

```csharp
using NSubstitute;
using Shouldly;
using StellarImperiums.Application.Abstractions;
using StellarImperiums.Application.Users.Commands;
using StellarImperiums.Application.Users.Exceptions;
using StellarImperiums.Domain.Tokens;
using StellarImperiums.Domain.Users;

namespace StellarImperiums.Application.Tests.Users.Commands;

public class LoginUserHandlerTests
{
    private const string ValidHash =
        "$argon2id$v=19$m=65536,t=3,p=4$0FCks4yDhpw2PqOmKg7FAw$EjBGRNLgHES+HUezNXUaQgZmM+Q/fFo2Uhnanx4DFEY";

    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly IRefreshTokenRepository _refreshTokens = Substitute.For<IRefreshTokenRepository>();
    private readonly IPasswordHasher _passwordHasher = Substitute.For<IPasswordHasher>();
    private readonly ITokenService _tokenService = Substitute.For<ITokenService>();
    private readonly LoginUserHandler _handler;

    public LoginUserHandlerTests()
    {
        _handler = new LoginUserHandler(_users, _refreshTokens, _passwordHasher, _tokenService);
        _tokenService.CreateAccessToken(Arg.Any<User>())
            .Returns(new AccessTokenResult("access-token", 900));
        _tokenService.GenerateRefreshToken()
            .Returns(new RefreshTokenMaterial(
                "refresh-value",
                "0011223344556677889900112233445566778899001122334455667788990011",
                DateTimeOffset.UtcNow.AddDays(30)));
    }

    private static User CreateUser(bool mustChangePassword = false)
    {
        return User.Create(
            Username.Create("Cmdr_Vega"),
            Email.Create("vega@stellar.io"),
            PasswordHash.Create(ValidHash),
            mustChangePassword: mustChangePassword);
    }

    [Fact]
    public async Task Handle_WithValidCredentials_ReturnsTokensAndRecordsLogin()
    {
        var user = CreateUser();
        _users.GetByEmailAsync(Arg.Any<Email>(), Arg.Any<CancellationToken>()).Returns(user);
        _passwordHasher.Verify("P@ssword12345", user.PasswordHash).Returns(true);

        var result = await _handler.Handle(
            new LoginUserCommand("vega@stellar.io", "P@ssword12345"), CancellationToken.None);

        result.AccessToken.ShouldBe("access-token");
        result.ExpiresInSeconds.ShouldBe(900);
        result.Username.ShouldBe("Cmdr_Vega");
        result.Email.ShouldBe("vega@stellar.io");
        result.Role.ShouldBe("Player");
        result.RefreshTokenValue.ShouldBe("refresh-value");
        result.MustChangePassword.ShouldBeFalse();
        user.LastLoginAt.ShouldNotBeNull();
        await _refreshTokens.Received(1).AddAsync(
            Arg.Is<RefreshToken>(t =>
                t.TokenHash == "0011223344556677889900112233445566778899001122334455667788990011"
                && t.FamilyId != Guid.Empty),
            Arg.Any<CancellationToken>());
        await _refreshTokens.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithUnknownEmail_ThrowsGenericAndStillVerifiesPassword()
    {
        _users.GetByEmailAsync(Arg.Any<Email>(), Arg.Any<CancellationToken>())
            .Returns((User?)null);

        await Should.ThrowAsync<InvalidCredentialsException>(
            () => _handler.Handle(
                new LoginUserCommand("ghost@stellar.io", "P@ssword12345"), CancellationToken.None));

        _passwordHasher.Received(1).Verify("P@ssword12345", Arg.Any<PasswordHash>());
        await _refreshTokens.DidNotReceive().AddAsync(Arg.Any<RefreshToken>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithWrongPassword_ThrowsGenericAndCreatesNoToken()
    {
        var user = CreateUser();
        _users.GetByEmailAsync(Arg.Any<Email>(), Arg.Any<CancellationToken>()).Returns(user);
        _passwordHasher.Verify("wrong-password", user.PasswordHash).Returns(false);

        await Should.ThrowAsync<InvalidCredentialsException>(
            () => _handler.Handle(
                new LoginUserCommand("vega@stellar.io", "wrong-password"), CancellationToken.None));

        await _refreshTokens.DidNotReceive().AddAsync(Arg.Any<RefreshToken>(), Arg.Any<CancellationToken>());
        await _refreshTokens.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithSuspendedUser_ThrowsAndCreatesNoToken()
    {
        var user = CreateUser();
        user.Suspend();
        _users.GetByEmailAsync(Arg.Any<Email>(), Arg.Any<CancellationToken>()).Returns(user);
        _passwordHasher.Verify("P@ssword12345", user.PasswordHash).Returns(true);

        await Should.ThrowAsync<UserSuspendedException>(
            () => _handler.Handle(
                new LoginUserCommand("vega@stellar.io", "P@ssword12345"), CancellationToken.None));

        await _refreshTokens.DidNotReceive().AddAsync(Arg.Any<RefreshToken>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_PropagatesMustChangePasswordFlag()
    {
        var user = CreateUser(mustChangePassword: true);
        _users.GetByEmailAsync(Arg.Any<Email>(), Arg.Any<CancellationToken>()).Returns(user);
        _passwordHasher.Verify("P@ssword12345", user.PasswordHash).Returns(true);

        var result = await _handler.Handle(
            new LoginUserCommand("vega@stellar.io", "P@ssword12345"), CancellationToken.None);

        result.MustChangePassword.ShouldBeTrue();
    }
}
```

- [ ] **Step 2: Vérifier que la compilation échoue**

Run: `dotnet build tests/StellarImperiums.Application.Tests`
Expected: FAIL — `LoginUserHandler` et `LoginUserResult` n'existent pas.

- [ ] **Step 3: Implémenter result et handler**

Créer `src/StellarImperiums.Application/Users/Commands/LoginUserResult.cs` :

```csharp
namespace StellarImperiums.Application.Users.Commands;

/// <summary>
/// Result returned after a successful login.
/// </summary>
/// <param name="AccessToken">The serialized signed access token.</param>
/// <param name="ExpiresInSeconds">The access token lifetime in seconds.</param>
/// <param name="UserId">The authenticated user identifier.</param>
/// <param name="Username">The authenticated user's username.</param>
/// <param name="Email">The authenticated user's email address.</param>
/// <param name="Role">The authenticated user's role name.</param>
/// <param name="MustChangePassword">Whether the user must change their password before playing.</param>
/// <param name="RefreshTokenValue">The opaque refresh token value to set as a cookie.</param>
/// <param name="RefreshTokenExpiresAt">The refresh token expiry used as the cookie lifetime.</param>
public sealed record LoginUserResult(
    string AccessToken,
    int ExpiresInSeconds,
    int UserId,
    string Username,
    string Email,
    string Role,
    bool MustChangePassword,
    string RefreshTokenValue,
    DateTimeOffset RefreshTokenExpiresAt);
```

Créer `src/StellarImperiums.Application/Users/Commands/LoginUserHandler.cs` :

```csharp
using StellarImperiums.Application.Abstractions;
using StellarImperiums.Application.Users.Exceptions;
using StellarImperiums.Domain.Tokens;
using StellarImperiums.Domain.Users;
using DomainEmail = StellarImperiums.Domain.Users.Email;

namespace StellarImperiums.Application.Users.Commands;

/// <summary>
/// Wolverine handler that authenticates a user and issues an access token plus a refresh token.
/// </summary>
/// <remarks>
/// When the email is unknown, the password is still verified against a constant dummy hash so the
/// response time does not reveal whether the account exists (timing attack mitigation).
/// </remarks>
public sealed class LoginUserHandler(
    IUserRepository users,
    IRefreshTokenRepository refreshTokens,
    IPasswordHasher passwordHasher,
    ITokenService tokenService)
{
    private static readonly PasswordHash DummyHash = PasswordHash.Create(
        "$argon2id$v=19$m=65536,t=3,p=4$0FCks4yDhpw2PqOmKg7FAw$EjBGRNLgHES+HUezNXUaQgZmM+Q/fFo2Uhnanx4DFEY");

    /// <summary>
    /// Handles the login command.
    /// </summary>
    /// <param name="command">The login command.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The issued tokens and the authenticated user's profile summary.</returns>
    /// <exception cref="InvalidCredentialsException">Thrown when the email or password is wrong.</exception>
    /// <exception cref="UserSuspendedException">Thrown when the account is suspended.</exception>
    public async Task<LoginUserResult> Handle(LoginUserCommand command, CancellationToken cancellationToken)
    {
        var email = DomainEmail.Create(command.Email);
        var user = await users.GetByEmailAsync(email, cancellationToken).ConfigureAwait(false);

        if (user is null)
        {
            passwordHasher.Verify(command.Password, DummyHash);
            throw new InvalidCredentialsException();
        }

        if (!passwordHasher.Verify(command.Password, user.PasswordHash))
        {
            throw new InvalidCredentialsException();
        }

        if (user.IsSuspended)
        {
            throw new UserSuspendedException();
        }

        var now = DateTimeOffset.UtcNow;
        user.RecordLogin(now);

        var access = tokenService.CreateAccessToken(user);
        var material = tokenService.GenerateRefreshToken();
        var refreshToken = RefreshToken.Create(user.Id, material.Hash, Guid.NewGuid(), material.ExpiresAt, now);

        await refreshTokens.AddAsync(refreshToken, cancellationToken).ConfigureAwait(false);
        await refreshTokens.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new LoginUserResult(
            access.Token,
            access.ExpiresInSeconds,
            user.Id,
            user.Username.Value,
            user.Email.Value,
            user.Role.ToString(),
            user.MustChangePassword,
            material.Value,
            material.ExpiresAt);
    }
}
```

- [ ] **Step 4: Vérifier que les tests passent**

Run: `dotnet test tests/StellarImperiums.Application.Tests --filter "FullyQualifiedName~LoginUserHandlerTests"`
Expected: PASS — 5 tests verts.

- [ ] **Step 5: Commit**

```bash
git add src/StellarImperiums.Application/Users/Commands tests/StellarImperiums.Application.Tests/Users/Commands/LoginUserHandlerTests.cs
git commit -m "feat(app): login handler with timing attack mitigation"
```

---

### Task 5: Application — `RefreshTokenHandler`

**Files:**
- Create: `src/StellarImperiums.Application/Tokens/Commands/RefreshTokenCommand.cs`
- Create: `src/StellarImperiums.Application/Tokens/Commands/RefreshTokenResult.cs`
- Create: `src/StellarImperiums.Application/Tokens/Commands/RefreshTokenHandler.cs`
- Test: `tests/StellarImperiums.Application.Tests/Tokens/Commands/RefreshTokenHandlerTests.cs`

- [ ] **Step 1: Écrire les tests qui échouent**

Créer `tests/StellarImperiums.Application.Tests/Tokens/Commands/RefreshTokenHandlerTests.cs` :

```csharp
using NSubstitute;
using Shouldly;
using StellarImperiums.Application.Abstractions;
using StellarImperiums.Application.Tokens.Commands;
using StellarImperiums.Application.Tokens.Exceptions;
using StellarImperiums.Application.Users.Exceptions;
using StellarImperiums.Domain.Tokens;
using StellarImperiums.Domain.Users;

namespace StellarImperiums.Application.Tests.Tokens.Commands;

public class RefreshTokenHandlerTests
{
    private const string ValidHash =
        "$argon2id$v=19$m=65536,t=3,p=4$0FCks4yDhpw2PqOmKg7FAw$EjBGRNLgHES+HUezNXUaQgZmM+Q/fFo2Uhnanx4DFEY";
    private const string CurrentHash =
        "A1B2C3D4E5F60718293A4B5C6D7E8F90A1B2C3D4E5F60718293A4B5C6D7E8F90";
    private const string SuccessorHash =
        "00112233445566778899AABBCCDDEEFF00112233445566778899AABBCCDDEEFF";

    private static readonly Guid Family = Guid.Parse("0193b6a0-0000-7000-8000-000000000001");

    private readonly IRefreshTokenRepository _refreshTokens = Substitute.For<IRefreshTokenRepository>();
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly ITokenService _tokenService = Substitute.For<ITokenService>();
    private readonly RefreshTokenHandler _handler;

    public RefreshTokenHandlerTests()
    {
        _handler = new RefreshTokenHandler(_refreshTokens, _users, _tokenService);
        _tokenService.HashRefreshToken("opaque-value").Returns(CurrentHash);
        _tokenService.CreateAccessToken(Arg.Any<User>())
            .Returns(new AccessTokenResult("new-access-token", 900));
        _tokenService.GenerateRefreshToken()
            .Returns(new RefreshTokenMaterial(
                "new-opaque-value", SuccessorHash, DateTimeOffset.UtcNow.AddDays(30)));
    }

    private static RefreshToken CreateStoredToken(DateTimeOffset? expiresAt = null) =>
        RefreshToken.Create(
            42,
            CurrentHash,
            Family,
            expiresAt ?? DateTimeOffset.UtcNow.AddDays(30),
            DateTimeOffset.UtcNow.AddDays(-1));

    private static User CreateUser() =>
        User.Create(
            Username.Create("Cmdr_Vega"),
            Email.Create("vega@stellar.io"),
            PasswordHash.Create(ValidHash));

    [Fact]
    public async Task Handle_WithActiveToken_RotatesAndReturnsNewTokens()
    {
        var stored = CreateStoredToken();
        _refreshTokens.GetByHashAsync(CurrentHash, Arg.Any<CancellationToken>()).Returns(stored);
        _users.GetByIdAsync(42, Arg.Any<CancellationToken>()).Returns(CreateUser());

        var result = await _handler.Handle(
            new RefreshTokenCommand("opaque-value"), CancellationToken.None);

        result.AccessToken.ShouldBe("new-access-token");
        result.RefreshTokenValue.ShouldBe("new-opaque-value");
        stored.ReplacedByTokenHash.ShouldBe(SuccessorHash);
        await _refreshTokens.Received(1).AddAsync(
            Arg.Is<RefreshToken>(t => t.TokenHash == SuccessorHash && t.FamilyId == Family),
            Arg.Any<CancellationToken>());
        await _refreshTokens.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithUnknownToken_ThrowsRejected()
    {
        _refreshTokens.GetByHashAsync(CurrentHash, Arg.Any<CancellationToken>())
            .Returns((RefreshToken?)null);

        await Should.ThrowAsync<RefreshTokenRejectedException>(
            () => _handler.Handle(new RefreshTokenCommand("opaque-value"), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_WithReplayedRotatedToken_RevokesWholeFamily()
    {
        var stored = CreateStoredToken();
        stored.Rotate(SuccessorHash, DateTimeOffset.UtcNow.AddDays(30), DateTimeOffset.UtcNow);
        _refreshTokens.GetByHashAsync(CurrentHash, Arg.Any<CancellationToken>()).Returns(stored);

        await Should.ThrowAsync<RefreshTokenRejectedException>(
            () => _handler.Handle(new RefreshTokenCommand("opaque-value"), CancellationToken.None));

        await _refreshTokens.Received(1).RevokeFamilyAsync(
            Family, Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>());
        await _refreshTokens.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithRevokedToken_ThrowsRejectedWithoutFamilyRevocation()
    {
        var stored = CreateStoredToken();
        stored.Revoke(DateTimeOffset.UtcNow);
        _refreshTokens.GetByHashAsync(CurrentHash, Arg.Any<CancellationToken>()).Returns(stored);

        await Should.ThrowAsync<RefreshTokenRejectedException>(
            () => _handler.Handle(new RefreshTokenCommand("opaque-value"), CancellationToken.None));

        await _refreshTokens.DidNotReceive().RevokeFamilyAsync(
            Arg.Any<Guid>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithExpiredToken_ThrowsRejected()
    {
        var stored = RefreshToken.Create(
            42, CurrentHash, Family,
            DateTimeOffset.UtcNow.AddMinutes(-5),
            DateTimeOffset.UtcNow.AddDays(-31));
        _refreshTokens.GetByHashAsync(CurrentHash, Arg.Any<CancellationToken>()).Returns(stored);

        await Should.ThrowAsync<RefreshTokenRejectedException>(
            () => _handler.Handle(new RefreshTokenCommand("opaque-value"), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_WithSuspendedUser_RevokesFamilyAndThrows()
    {
        var stored = CreateStoredToken();
        var user = CreateUser();
        user.Suspend();
        _refreshTokens.GetByHashAsync(CurrentHash, Arg.Any<CancellationToken>()).Returns(stored);
        _users.GetByIdAsync(42, Arg.Any<CancellationToken>()).Returns(user);

        await Should.ThrowAsync<UserSuspendedException>(
            () => _handler.Handle(new RefreshTokenCommand("opaque-value"), CancellationToken.None));

        await _refreshTokens.Received(1).RevokeFamilyAsync(
            Family, Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithMissingUser_ThrowsRejected()
    {
        var stored = CreateStoredToken();
        _refreshTokens.GetByHashAsync(CurrentHash, Arg.Any<CancellationToken>()).Returns(stored);
        _users.GetByIdAsync(42, Arg.Any<CancellationToken>()).Returns((User?)null);

        await Should.ThrowAsync<RefreshTokenRejectedException>(
            () => _handler.Handle(new RefreshTokenCommand("opaque-value"), CancellationToken.None));
    }
}
```

- [ ] **Step 2: Vérifier que la compilation échoue**

Run: `dotnet build tests/StellarImperiums.Application.Tests`
Expected: FAIL — `RefreshTokenCommand`, `RefreshTokenResult`, `RefreshTokenHandler` n'existent pas.

- [ ] **Step 3: Implémenter command, result et handler**

Créer `src/StellarImperiums.Application/Tokens/Commands/RefreshTokenCommand.cs` :

```csharp
namespace StellarImperiums.Application.Tokens.Commands;

/// <summary>
/// Command issued to exchange a refresh token for a new access token and a rotated refresh token.
/// </summary>
/// <param name="RefreshTokenValue">The opaque refresh token value read from the client cookie.</param>
public sealed record RefreshTokenCommand(string RefreshTokenValue);
```

Créer `src/StellarImperiums.Application/Tokens/Commands/RefreshTokenResult.cs` :

```csharp
namespace StellarImperiums.Application.Tokens.Commands;

/// <summary>
/// Result returned after a successful refresh token rotation.
/// </summary>
/// <param name="AccessToken">The new serialized signed access token.</param>
/// <param name="ExpiresInSeconds">The access token lifetime in seconds.</param>
/// <param name="RefreshTokenValue">The new opaque refresh token value to set as a cookie.</param>
/// <param name="RefreshTokenExpiresAt">The new refresh token expiry used as the cookie lifetime.</param>
public sealed record RefreshTokenResult(
    string AccessToken,
    int ExpiresInSeconds,
    string RefreshTokenValue,
    DateTimeOffset RefreshTokenExpiresAt);
```

Créer `src/StellarImperiums.Application/Tokens/Commands/RefreshTokenHandler.cs` :

```csharp
using StellarImperiums.Application.Abstractions;
using StellarImperiums.Application.Tokens.Exceptions;
using StellarImperiums.Application.Users.Exceptions;

namespace StellarImperiums.Application.Tokens.Commands;

/// <summary>
/// Wolverine handler that rotates a refresh token and issues a new access token.
/// </summary>
/// <remarks>
/// Presenting a token that was already rotated is treated as a theft signal: the whole
/// rotation family is revoked so both the attacker and the legitimate session are cut off.
/// </remarks>
public sealed class RefreshTokenHandler(
    IRefreshTokenRepository refreshTokens,
    IUserRepository users,
    ITokenService tokenService)
{
    /// <summary>
    /// Handles the refresh command.
    /// </summary>
    /// <param name="command">The refresh command.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The new access token and the rotated refresh token material.</returns>
    /// <exception cref="RefreshTokenRejectedException">
    /// Thrown when the token is unknown, expired, revoked, replayed, or its owner no longer exists.
    /// </exception>
    /// <exception cref="UserSuspendedException">Thrown when the owner account is suspended.</exception>
    public async Task<RefreshTokenResult> Handle(RefreshTokenCommand command, CancellationToken cancellationToken)
    {
        var hash = tokenService.HashRefreshToken(command.RefreshTokenValue);
        var token = await refreshTokens.GetByHashAsync(hash, cancellationToken).ConfigureAwait(false);

        if (token is null)
        {
            throw new RefreshTokenRejectedException();
        }

        var now = DateTimeOffset.UtcNow;

        if (token.IsRotated)
        {
            if (!token.IsRevoked)
            {
                await refreshTokens.RevokeFamilyAsync(token.FamilyId, now, cancellationToken).ConfigureAwait(false);
                await refreshTokens.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            }

            throw new RefreshTokenRejectedException();
        }

        if (token.IsRevoked || token.IsExpired(now))
        {
            throw new RefreshTokenRejectedException();
        }

        var user = await users.GetByIdAsync(token.UserId, cancellationToken).ConfigureAwait(false)
            ?? throw new RefreshTokenRejectedException();

        if (user.IsSuspended)
        {
            await refreshTokens.RevokeFamilyAsync(token.FamilyId, now, cancellationToken).ConfigureAwait(false);
            await refreshTokens.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            throw new UserSuspendedException();
        }

        var material = tokenService.GenerateRefreshToken();
        var successor = token.Rotate(material.Hash, material.ExpiresAt, now);

        await refreshTokens.AddAsync(successor, cancellationToken).ConfigureAwait(false);
        await refreshTokens.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var access = tokenService.CreateAccessToken(user);
        return new RefreshTokenResult(access.Token, access.ExpiresInSeconds, material.Value, material.ExpiresAt);
    }
}
```

- [ ] **Step 4: Vérifier que les tests passent**

Run: `dotnet test tests/StellarImperiums.Application.Tests --filter "FullyQualifiedName~RefreshTokenHandlerTests"`
Expected: PASS — 7 tests verts.

- [ ] **Step 5: Commit**

```bash
git add src/StellarImperiums.Application/Tokens tests/StellarImperiums.Application.Tests/Tokens
git commit -m "feat(app): refresh token rotation handler with family revocation on replay"
```

---

### Task 6: Application — `LogoutHandler`

**Files:**
- Create: `src/StellarImperiums.Application/Tokens/Commands/LogoutCommand.cs`
- Create: `src/StellarImperiums.Application/Tokens/Commands/LogoutHandler.cs`
- Test: `tests/StellarImperiums.Application.Tests/Tokens/Commands/LogoutHandlerTests.cs`

- [ ] **Step 1: Écrire les tests qui échouent**

Créer `tests/StellarImperiums.Application.Tests/Tokens/Commands/LogoutHandlerTests.cs` :

```csharp
using NSubstitute;
using StellarImperiums.Application.Abstractions;
using StellarImperiums.Application.Tokens.Commands;
using StellarImperiums.Domain.Tokens;

namespace StellarImperiums.Application.Tests.Tokens.Commands;

public class LogoutHandlerTests
{
    private const string CurrentHash =
        "A1B2C3D4E5F60718293A4B5C6D7E8F90A1B2C3D4E5F60718293A4B5C6D7E8F90";

    private static readonly Guid Family = Guid.Parse("0193b6a0-0000-7000-8000-000000000001");

    private readonly IRefreshTokenRepository _refreshTokens = Substitute.For<IRefreshTokenRepository>();
    private readonly ITokenService _tokenService = Substitute.For<ITokenService>();
    private readonly LogoutHandler _handler;

    public LogoutHandlerTests()
    {
        _handler = new LogoutHandler(_refreshTokens, _tokenService);
        _tokenService.HashRefreshToken("opaque-value").Returns(CurrentHash);
    }

    [Fact]
    public async Task Handle_WithKnownToken_RevokesFamily()
    {
        var stored = RefreshToken.Create(
            42, CurrentHash, Family, DateTimeOffset.UtcNow.AddDays(30), DateTimeOffset.UtcNow.AddDays(-1));
        _refreshTokens.GetByHashAsync(CurrentHash, Arg.Any<CancellationToken>()).Returns(stored);

        await _handler.Handle(new LogoutCommand("opaque-value"), CancellationToken.None);

        await _refreshTokens.Received(1).RevokeFamilyAsync(
            Family, Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>());
        await _refreshTokens.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithUnknownToken_DoesNothing()
    {
        _refreshTokens.GetByHashAsync(CurrentHash, Arg.Any<CancellationToken>())
            .Returns((RefreshToken?)null);

        await _handler.Handle(new LogoutCommand("opaque-value"), CancellationToken.None);

        await _refreshTokens.DidNotReceive().RevokeFamilyAsync(
            Arg.Any<Guid>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>());
        await _refreshTokens.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Handle_WithMissingTokenValue_DoesNothing(string? value)
    {
        await _handler.Handle(new LogoutCommand(value), CancellationToken.None);

        _tokenService.DidNotReceive().HashRefreshToken(Arg.Any<string>());
        await _refreshTokens.DidNotReceive().GetByHashAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }
}
```

- [ ] **Step 2: Vérifier que la compilation échoue**

Run: `dotnet build tests/StellarImperiums.Application.Tests`
Expected: FAIL — `LogoutCommand` et `LogoutHandler` n'existent pas.

- [ ] **Step 3: Implémenter command et handler**

Créer `src/StellarImperiums.Application/Tokens/Commands/LogoutCommand.cs` :

```csharp
namespace StellarImperiums.Application.Tokens.Commands;

/// <summary>
/// Command issued to terminate a session by revoking its refresh token family.
/// </summary>
/// <param name="RefreshTokenValue">
/// The opaque refresh token value read from the client cookie, or <c>null</c> when no cookie was sent.
/// </param>
public sealed record LogoutCommand(string? RefreshTokenValue);
```

Créer `src/StellarImperiums.Application/Tokens/Commands/LogoutHandler.cs` :

```csharp
using StellarImperiums.Application.Abstractions;

namespace StellarImperiums.Application.Tokens.Commands;

/// <summary>
/// Wolverine handler that revokes the refresh token family of a session.
/// </summary>
/// <remarks>
/// Logout is idempotent: a missing or unknown token is silently ignored so the endpoint
/// always succeeds from the client's point of view.
/// </remarks>
public sealed class LogoutHandler(IRefreshTokenRepository refreshTokens, ITokenService tokenService)
{
    /// <summary>
    /// Handles the logout command.
    /// </summary>
    /// <param name="command">The logout command.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    public async Task Handle(LogoutCommand command, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(command.RefreshTokenValue))
        {
            return;
        }

        var hash = tokenService.HashRefreshToken(command.RefreshTokenValue);
        var token = await refreshTokens.GetByHashAsync(hash, cancellationToken).ConfigureAwait(false);

        if (token is null)
        {
            return;
        }

        await refreshTokens.RevokeFamilyAsync(token.FamilyId, DateTimeOffset.UtcNow, cancellationToken).ConfigureAwait(false);
        await refreshTokens.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
```

- [ ] **Step 4: Vérifier que les tests passent**

Run: `dotnet test tests/StellarImperiums.Application.Tests --filter "FullyQualifiedName~LogoutHandlerTests"`
Expected: PASS — 5 tests verts (3 cas du Theory inclus).

- [ ] **Step 5: Commit**

```bash
git add src/StellarImperiums.Application/Tokens/Commands tests/StellarImperiums.Application.Tests/Tokens/Commands/LogoutHandlerTests.cs
git commit -m "feat(app): idempotent logout handler revoking the token family"
```

---

### Task 7: Application — `GetCurrentUserQuery`

**Files:**
- Create: `src/StellarImperiums.Application/Users/Queries/GetCurrentUserQuery.cs`
- Create: `src/StellarImperiums.Application/Users/Queries/CurrentUserResult.cs`
- Create: `src/StellarImperiums.Application/Users/Queries/GetCurrentUserHandler.cs`
- Test: `tests/StellarImperiums.Application.Tests/Users/Queries/GetCurrentUserHandlerTests.cs`

- [ ] **Step 1: Écrire les tests qui échouent**

Créer `tests/StellarImperiums.Application.Tests/Users/Queries/GetCurrentUserHandlerTests.cs` :

```csharp
using NSubstitute;
using Shouldly;
using StellarImperiums.Application.Abstractions;
using StellarImperiums.Application.Users.Exceptions;
using StellarImperiums.Application.Users.Queries;
using StellarImperiums.Domain.Users;

namespace StellarImperiums.Application.Tests.Users.Queries;

public class GetCurrentUserHandlerTests
{
    private const string ValidHash =
        "$argon2id$v=19$m=65536,t=3,p=4$0FCks4yDhpw2PqOmKg7FAw$EjBGRNLgHES+HUezNXUaQgZmM+Q/fFo2Uhnanx4DFEY";

    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly GetCurrentUserHandler _handler;

    public GetCurrentUserHandlerTests()
    {
        _handler = new GetCurrentUserHandler(_users);
    }

    [Fact]
    public async Task Handle_WithExistingUser_ReturnsProfile()
    {
        var user = User.Create(
            Username.Create("Cmdr_Vega"),
            Email.Create("vega@stellar.io"),
            PasswordHash.Create(ValidHash));
        _users.GetByIdAsync(42, Arg.Any<CancellationToken>()).Returns(user);

        var result = await _handler.Handle(new GetCurrentUserQuery(42), CancellationToken.None);

        result.Username.ShouldBe("Cmdr_Vega");
        result.Email.ShouldBe("vega@stellar.io");
        result.Role.ShouldBe("Player");
        result.RegistrationDate.ShouldBe(user.RegistrationDate);
        result.LastLoginAt.ShouldBeNull();
    }

    [Fact]
    public async Task Handle_WithUnknownUser_Throws()
    {
        _users.GetByIdAsync(42, Arg.Any<CancellationToken>()).Returns((User?)null);

        await Should.ThrowAsync<UserNotFoundException>(
            () => _handler.Handle(new GetCurrentUserQuery(42), CancellationToken.None));
    }
}
```

- [ ] **Step 2: Vérifier que la compilation échoue**

Run: `dotnet build tests/StellarImperiums.Application.Tests`
Expected: FAIL — le namespace `StellarImperiums.Application.Users.Queries` n'existe pas.

- [ ] **Step 3: Implémenter query, result et handler**

Créer `src/StellarImperiums.Application/Users/Queries/GetCurrentUserQuery.cs` :

```csharp
namespace StellarImperiums.Application.Users.Queries;

/// <summary>
/// Query issued to load the profile of the authenticated user.
/// </summary>
/// <param name="UserId">The user identifier extracted from the access token subject claim.</param>
public sealed record GetCurrentUserQuery(int UserId);
```

Créer `src/StellarImperiums.Application/Users/Queries/CurrentUserResult.cs` :

```csharp
namespace StellarImperiums.Application.Users.Queries;

/// <summary>
/// Profile of the authenticated user.
/// </summary>
/// <param name="Id">The user identifier.</param>
/// <param name="Username">The user's username.</param>
/// <param name="Email">The user's email address.</param>
/// <param name="Role">The user's role name.</param>
/// <param name="RegistrationDate">The registration timestamp (UTC).</param>
/// <param name="LastLoginAt">The last successful login timestamp (UTC), or <c>null</c>.</param>
public sealed record CurrentUserResult(
    int Id,
    string Username,
    string Email,
    string Role,
    DateTimeOffset RegistrationDate,
    DateTimeOffset? LastLoginAt);
```

Créer `src/StellarImperiums.Application/Users/Queries/GetCurrentUserHandler.cs` :

```csharp
using StellarImperiums.Application.Abstractions;
using StellarImperiums.Application.Users.Exceptions;

namespace StellarImperiums.Application.Users.Queries;

/// <summary>
/// Wolverine handler that loads the authenticated user's profile.
/// </summary>
public sealed class GetCurrentUserHandler(IUserRepository users)
{
    /// <summary>
    /// Handles the query.
    /// </summary>
    /// <param name="query">The query carrying the authenticated user identifier.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The user's profile.</returns>
    /// <exception cref="UserNotFoundException">Thrown when the user no longer exists.</exception>
    public async Task<CurrentUserResult> Handle(GetCurrentUserQuery query, CancellationToken cancellationToken)
    {
        var user = await users.GetByIdAsync(query.UserId, cancellationToken).ConfigureAwait(false)
            ?? throw new UserNotFoundException();

        return new CurrentUserResult(
            user.Id,
            user.Username.Value,
            user.Email.Value,
            user.Role.ToString(),
            user.RegistrationDate,
            user.LastLoginAt);
    }
}
```

- [ ] **Step 4: Vérifier que les tests passent**

Run: `dotnet test tests/StellarImperiums.Application.Tests --filter "FullyQualifiedName~GetCurrentUserHandlerTests"`
Expected: PASS — 2 tests verts.

- [ ] **Step 5: Commit**

```bash
git add src/StellarImperiums.Application/Users/Queries tests/StellarImperiums.Application.Tests/Users/Queries
git commit -m "feat(app): current user profile query"
```

---

### Task 8: Infrastructure — `JwtOptions` et `JwtTokenService`

**Files:**
- Create: `src/StellarImperiums.Infrastructure/Security/JwtOptions.cs`
- Create: `src/StellarImperiums.Infrastructure/Security/JwtTokenService.cs`
- Test: `tests/StellarImperiums.Infrastructure.Tests/Security/JwtTokenServiceTests.cs`

- [ ] **Step 1: Ajouter les packages**

```bash
dotnet add src/StellarImperiums.Infrastructure package Microsoft.IdentityModel.JsonWebTokens
dotnet add src/StellarImperiums.Infrastructure package Microsoft.Extensions.Options.ConfigurationExtensions
dotnet add src/StellarImperiums.Infrastructure package Microsoft.Extensions.Options.DataAnnotations
```

Vérifier que `tests/StellarImperiums.Infrastructure.Tests/StellarImperiums.Infrastructure.Tests.csproj` référence `Shouldly` ; sinon : `dotnet add tests/StellarImperiums.Infrastructure.Tests package Shouldly`.

- [ ] **Step 2: Écrire les tests qui échouent**

Créer `tests/StellarImperiums.Infrastructure.Tests/Security/JwtTokenServiceTests.cs` :

```csharp
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Shouldly;
using StellarImperiums.Domain.Users;
using StellarImperiums.Infrastructure.Security;
using System.Text;

namespace StellarImperiums.Infrastructure.Tests.Security;

public class JwtTokenServiceTests
{
    private const string SigningKey = "unit-tests-signing-key-0123456789abcdef0123456789abcdef";
    private const string ValidHash =
        "$argon2id$v=19$m=65536,t=3,p=4$0FCks4yDhpw2PqOmKg7FAw$EjBGRNLgHES+HUezNXUaQgZmM+Q/fFo2Uhnanx4DFEY";

    private readonly JwtTokenService _service = new(Options.Create(new JwtOptions
    {
        Issuer = "StellarImperiums.Tests",
        Audience = "StellarImperiums.Tests",
        AccessTokenLifetimeMinutes = 15,
        RefreshTokenLifetimeDays = 30,
        SigningKey = SigningKey
    }));

    private static User CreateUser() =>
        User.Create(
            Username.Create("Cmdr_Vega"),
            Email.Create("vega@stellar.io"),
            PasswordHash.Create(ValidHash));

    [Fact]
    public void CreateAccessToken_EmbedsExpectedClaims()
    {
        var result = _service.CreateAccessToken(CreateUser());

        var token = new JsonWebToken(result.Token);
        token.GetClaim(JwtRegisteredClaimNames.Sub).Value.ShouldBe("0");
        token.GetClaim(JwtRegisteredClaimNames.UniqueName).Value.ShouldBe("Cmdr_Vega");
        token.GetClaim("role").Value.ShouldBe("Player");
        token.TryGetClaim(JwtRegisteredClaimNames.Jti, out _).ShouldBeTrue();
        token.Issuer.ShouldBe("StellarImperiums.Tests");
        token.Audiences.ShouldContain("StellarImperiums.Tests");
    }

    [Fact]
    public void CreateAccessToken_DoesNotEmbedEmail()
    {
        var result = _service.CreateAccessToken(CreateUser());

        var token = new JsonWebToken(result.Token);
        token.TryGetClaim(JwtRegisteredClaimNames.Email, out _).ShouldBeFalse();
        result.Token.ShouldNotContain("vega@stellar.io");
    }

    [Fact]
    public void CreateAccessToken_SetsFifteenMinutesLifetime()
    {
        var result = _service.CreateAccessToken(CreateUser());

        result.ExpiresInSeconds.ShouldBe(900);
        var token = new JsonWebToken(result.Token);
        (token.ValidTo - token.ValidFrom).ShouldBe(TimeSpan.FromMinutes(15));
    }

    [Fact]
    public async Task CreateAccessToken_SignatureValidatesWithTheConfiguredKey()
    {
        var result = _service.CreateAccessToken(CreateUser());

        var validation = await new JsonWebTokenHandler().ValidateTokenAsync(result.Token,
            new TokenValidationParameters
            {
                ValidIssuer = "StellarImperiums.Tests",
                ValidAudience = "StellarImperiums.Tests",
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SigningKey)),
                ValidateLifetime = true
            });

        validation.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void GenerateRefreshToken_ProducesHighEntropyValueAndHexHash()
    {
        var material = _service.GenerateRefreshToken();

        material.Value.Length.ShouldBe(43);
        material.Hash.Length.ShouldBe(64);
        material.Hash.ShouldAllBe(c => Uri.IsHexDigit(c));
        material.ExpiresAt.ShouldBeGreaterThan(DateTimeOffset.UtcNow.AddDays(29));
    }

    [Fact]
    public void GenerateRefreshToken_ProducesUniqueValues()
    {
        var first = _service.GenerateRefreshToken();
        var second = _service.GenerateRefreshToken();

        first.Value.ShouldNotBe(second.Value);
        first.Hash.ShouldNotBe(second.Hash);
    }

    [Fact]
    public void HashRefreshToken_IsDeterministicAndMatchesGeneratedMaterial()
    {
        var material = _service.GenerateRefreshToken();

        _service.HashRefreshToken(material.Value).ShouldBe(material.Hash);
        _service.HashRefreshToken(material.Value).ShouldBe(_service.HashRefreshToken(material.Value));
    }
}
```

- [ ] **Step 3: Vérifier que la compilation échoue**

Run: `dotnet build tests/StellarImperiums.Infrastructure.Tests`
Expected: FAIL — `JwtOptions` et `JwtTokenService` n'existent pas.

- [ ] **Step 4: Implémenter options et service**

Créer `src/StellarImperiums.Infrastructure/Security/JwtOptions.cs` :

```csharp
using System.ComponentModel.DataAnnotations;

namespace StellarImperiums.Infrastructure.Security;

/// <summary>
/// Configuration of the JWT issuance and validation (bound from the <c>Jwt</c> configuration section).
/// </summary>
/// <remarks>
/// The signing key is never stored in <c>appsettings.json</c>: it is provided through
/// user-secrets in development and an environment variable in production. Validation runs
/// at startup (<c>ValidateOnStart</c>) so a missing or weak key prevents the host from booting.
/// </remarks>
public sealed class JwtOptions
{
    /// <summary>
    /// Name of the configuration section bound to these options.
    /// </summary>
    public const string SectionName = "Jwt";

    /// <summary>
    /// Gets the token issuer.
    /// </summary>
    [Required]
    public string Issuer { get; init; } = string.Empty;

    /// <summary>
    /// Gets the token audience.
    /// </summary>
    [Required]
    public string Audience { get; init; } = string.Empty;

    /// <summary>
    /// Gets the access token lifetime in minutes.
    /// </summary>
    [Range(1, 1440)]
    public int AccessTokenLifetimeMinutes { get; init; } = 15;

    /// <summary>
    /// Gets the refresh token lifetime in days.
    /// </summary>
    [Range(1, 365)]
    public int RefreshTokenLifetimeDays { get; init; } = 30;

    /// <summary>
    /// Gets the HMAC-SHA256 signing key (at least 32 characters).
    /// </summary>
    [Required]
    [MinLength(32)]
    public string SigningKey { get; init; } = string.Empty;
}
```

Créer `src/StellarImperiums.Infrastructure/Security/JwtTokenService.cs` :

```csharp
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using StellarImperiums.Application.Abstractions;
using StellarImperiums.Domain.Users;

namespace StellarImperiums.Infrastructure.Security;

/// <summary>
/// HS256 implementation of <see cref="ITokenService"/> based on <see cref="JsonWebTokenHandler"/>.
/// </summary>
/// <remarks>
/// Access tokens carry only the subject id, username, role, and a unique token id: the email
/// address is deliberately excluded because a JWT is signed but not encrypted. Refresh tokens
/// are 32 random bytes encoded as Base64Url and persisted as an uppercase hexadecimal SHA-256 hash.
/// </remarks>
public sealed class JwtTokenService(IOptions<JwtOptions> options) : ITokenService
{
    private const int RefreshTokenByteLength = 32;

    /// <inheritdoc />
    public AccessTokenResult CreateAccessToken(User user)
    {
        ArgumentNullException.ThrowIfNull(user);

        var jwtOptions = options.Value;
        var now = DateTimeOffset.UtcNow;
        var lifetime = TimeSpan.FromMinutes(jwtOptions.AccessTokenLifetimeMinutes);

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = jwtOptions.Issuer,
            Audience = jwtOptions.Audience,
            IssuedAt = now.UtcDateTime,
            NotBefore = now.UtcDateTime,
            Expires = now.Add(lifetime).UtcDateTime,
            Claims = new Dictionary<string, object>
            {
                [JwtRegisteredClaimNames.Sub] = user.Id.ToString(CultureInfo.InvariantCulture),
                [JwtRegisteredClaimNames.UniqueName] = user.Username.Value,
                ["role"] = user.Role.ToString(),
                [JwtRegisteredClaimNames.Jti] = Guid.NewGuid().ToString()
            },
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SigningKey)),
                SecurityAlgorithms.HmacSha256)
        };

        var token = new JsonWebTokenHandler().CreateToken(descriptor);
        return new AccessTokenResult(token, (int)lifetime.TotalSeconds);
    }

    /// <inheritdoc />
    public RefreshTokenMaterial GenerateRefreshToken()
    {
        var value = Base64UrlEncoder.Encode(RandomNumberGenerator.GetBytes(RefreshTokenByteLength));
        var expiresAt = DateTimeOffset.UtcNow.AddDays(options.Value.RefreshTokenLifetimeDays);
        return new RefreshTokenMaterial(value, HashRefreshToken(value), expiresAt);
    }

    /// <inheritdoc />
    public string HashRefreshToken(string refreshTokenValue)
    {
        ArgumentException.ThrowIfNullOrEmpty(refreshTokenValue);
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(refreshTokenValue)));
    }
}
```

- [ ] **Step 5: Vérifier que les tests passent**

Run: `dotnet test tests/StellarImperiums.Infrastructure.Tests --filter "FullyQualifiedName~JwtTokenServiceTests"`
Expected: PASS — 7 tests verts.

- [ ] **Step 6: Commit**

```bash
git add src/StellarImperiums.Infrastructure tests/StellarImperiums.Infrastructure.Tests/Security/JwtTokenServiceTests.cs
git commit -m "feat(infra): HS256 token service with hashed opaque refresh tokens"
```

---

### Task 9: Infrastructure — persistance (`RefreshTokenConfiguration`, repository, DI, migration)

**Files:**
- Modify: `src/StellarImperiums.Infrastructure/Persistence/StellarDbContext.cs`
- Create: `src/StellarImperiums.Infrastructure/Persistence/Configurations/RefreshTokenConfiguration.cs`
- Create: `src/StellarImperiums.Infrastructure/Persistence/Repositories/EfRefreshTokenRepository.cs`
- Modify: `src/StellarImperiums.Infrastructure/DependencyInjection.cs`
- Modify: `src/StellarImperiums.Api/Program.cs` (signature `AddInfrastructure`)
- Generate: `src/StellarImperiums.Infrastructure/Persistence/Migrations/<timestamp>_AddRefreshTokens.cs`

Vérifié par les tests d'intégration (Task 12) : la migration s'applique sur un PostgreSQL réel.

- [ ] **Step 1: Ajouter le `DbSet`**

Dans `src/StellarImperiums.Infrastructure/Persistence/StellarDbContext.cs`, ajouter `using StellarImperiums.Domain.Tokens;` puis, après la propriété `Users` :

```csharp
    /// <summary>
    /// Gets the queryable set of refresh tokens.
    /// </summary>
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
```

- [ ] **Step 2: Créer la configuration EF**

Créer `src/StellarImperiums.Infrastructure/Persistence/Configurations/RefreshTokenConfiguration.cs` :

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StellarImperiums.Domain.Tokens;
using StellarImperiums.Domain.Users;

namespace StellarImperiums.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core mapping configuration for the <see cref="RefreshToken"/> entity.
/// </summary>
/// <remarks>
/// Follows the French snake_case naming convention of the shared SQL schema. The token hash
/// is a 64-character uppercase hexadecimal SHA-256 digest with a unique index used as the
/// primary lookup path.
/// </remarks>
public sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    private const int Sha256HexLength = 64;

    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("jeton_rafraichissement");

        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id)
            .HasColumnName("id_jeton")
            .UseIdentityColumn();

        builder.Property(t => t.UserId)
            .HasColumnName("id_utilisateur")
            .IsRequired();

        builder.Property(t => t.TokenHash)
            .HasColumnName("hash_jeton")
            .HasMaxLength(Sha256HexLength)
            .IsRequired();

        builder.Property(t => t.FamilyId)
            .HasColumnName("id_famille")
            .IsRequired();

        builder.Property(t => t.CreatedAt)
            .HasColumnName("cree_le")
            .IsRequired();

        builder.Property(t => t.ExpiresAt)
            .HasColumnName("expire_le")
            .IsRequired();

        builder.Property(t => t.RevokedAt)
            .HasColumnName("revoque_le");

        builder.Property(t => t.ReplacedByTokenHash)
            .HasColumnName("remplace_par_hash")
            .HasMaxLength(Sha256HexLength);

        builder.HasIndex(t => t.TokenHash)
            .IsUnique()
            .HasDatabaseName("jeton_rafraichissement_hash_key");

        builder.HasIndex(t => t.FamilyId)
            .HasDatabaseName("idx_jeton_rafraichissement_famille");

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(t => t.UserId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_jeton_rafraichissement_utilisateur");
    }
}
```

- [ ] **Step 3: Créer le repository EF**

Créer `src/StellarImperiums.Infrastructure/Persistence/Repositories/EfRefreshTokenRepository.cs` :

```csharp
using Microsoft.EntityFrameworkCore;
using StellarImperiums.Application.Abstractions;
using StellarImperiums.Domain.Tokens;

namespace StellarImperiums.Infrastructure.Persistence.Repositories;

/// <summary>
/// Entity Framework Core implementation of <see cref="IRefreshTokenRepository"/>.
/// </summary>
/// <remarks>
/// Family revocation loads the affected tokens and applies the domain transition on each one,
/// so the revocation rules stay inside the entity instead of leaking into a bulk SQL update.
/// </remarks>
public sealed class EfRefreshTokenRepository(StellarDbContext dbContext) : IRefreshTokenRepository
{
    /// <inheritdoc />
    public Task AddAsync(RefreshToken token, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(token);
        return dbContext.RefreshTokens.AddAsync(token, cancellationToken).AsTask();
    }

    /// <inheritdoc />
    public Task<RefreshToken?> GetByHashAsync(string tokenHash, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(tokenHash);
        return dbContext.RefreshTokens
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash, cancellationToken);
    }

    /// <inheritdoc />
    public async Task RevokeFamilyAsync(Guid familyId, DateTimeOffset when, CancellationToken cancellationToken = default)
    {
        var tokens = await dbContext.RefreshTokens
            .Where(t => t.FamilyId == familyId && t.RevokedAt == null)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        foreach (var token in tokens)
        {
            token.Revoke(when);
        }
    }

    /// <inheritdoc />
    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        dbContext.SaveChangesAsync(cancellationToken);
}
```

- [ ] **Step 4: Mettre à jour la DI**

Remplacer le contenu de `src/StellarImperiums.Infrastructure/DependencyInjection.cs` par :

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StellarImperiums.Application.Abstractions;
using StellarImperiums.Infrastructure.Persistence;
using StellarImperiums.Infrastructure.Persistence.Repositories;
using StellarImperiums.Infrastructure.Security;

namespace StellarImperiums.Infrastructure;

/// <summary>
/// Wires Infrastructure services (EF Core, repositories, security) into the dependency injection container.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registers all Infrastructure services required by the Application and Api layers.
    /// </summary>
    /// <param name="services">The service collection to extend.</param>
    /// <param name="configuration">The application configuration.</param>
    /// <returns>The same <see cref="IServiceCollection"/> for chaining.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the <c>ConnectionStrings:Postgres</c> entry is missing.
    /// </exception>
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var postgresConnectionString = configuration.GetConnectionString("Postgres")
            ?? throw new InvalidOperationException(
                "ConnectionStrings:Postgres is not configured. Set it via user-secrets, environment variable, or appsettings.");

        services.AddDbContext<StellarDbContext>(options =>
            options.UseNpgsql(postgresConnectionString));

        services.AddScoped<IUserRepository, EfUserRepository>();
        services.AddScoped<IRefreshTokenRepository, EfRefreshTokenRepository>();
        services.AddSingleton<IPasswordHasher, Argon2idPasswordHasher>();

        services.AddOptions<JwtOptions>()
            .BindConfiguration(JwtOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();
        services.AddSingleton<ITokenService, JwtTokenService>();

        return services;
    }
}
```

Dans `src/StellarImperiums.Api/Program.cs`, remplacer :

```csharp
builder.Services.AddInfrastructure(postgresConnectionString);
```

par :

```csharp
builder.Services.AddInfrastructure(builder.Configuration);
```

La variable `postgresConnectionString` de `Program.cs` reste utilisée par le health check — ne pas la supprimer.

- [ ] **Step 5: Générer la migration**

```bash
dotnet tool restore
dotnet ef migrations add AddRefreshTokens --project src/StellarImperiums.Infrastructure --startup-project src/StellarImperiums.Api
```

Expected: nouveau fichier `<timestamp>_AddRefreshTokens.cs` créant la table `jeton_rafraichissement` avec les index `jeton_rafraichissement_hash_key` (unique) et `idx_jeton_rafraichissement_famille`, et la FK cascade vers `utilisateur`. Inspecter le fichier généré pour vérifier ces trois points.

- [ ] **Step 6: Vérifier la compilation et la non-régression**

Run: `dotnet build && dotnet test tests/StellarImperiums.Domain.Tests tests/StellarImperiums.Application.Tests tests/StellarImperiums.Infrastructure.Tests`
Expected: PASS — build sans erreur, tous les tests existants verts.

- [ ] **Step 7: Commit**

```bash
git add src/StellarImperiums.Infrastructure src/StellarImperiums.Api/Program.cs
git commit -m "feat(infra): refresh token persistence with EF configuration and migration"
```

---

### Task 10: Api — contracts, `AuthController` (login/refresh/logout) et mapping d'exceptions

**Files:**
- Create: `src/StellarImperiums.Api/Contracts/Auth/LoginRequest.cs`
- Create: `src/StellarImperiums.Api/Contracts/Auth/LoginResponse.cs`
- Create: `src/StellarImperiums.Api/Contracts/Auth/RefreshResponse.cs`
- Modify: `src/StellarImperiums.Api/Controllers/V1/AuthController.cs`
- Modify: `src/StellarImperiums.Api/Middleware/DomainExceptionMiddleware.cs`

Vérifié par les tests d'intégration (Task 12).

- [ ] **Step 1: Créer les contracts**

Créer `src/StellarImperiums.Api/Contracts/Auth/LoginRequest.cs` :

```csharp
namespace StellarImperiums.Api.Contracts.Auth;

/// <summary>
/// Payload of the login endpoint.
/// </summary>
/// <param name="Email">The account email address.</param>
/// <param name="Password">The account password.</param>
public sealed record LoginRequest(string Email, string Password);
```

Créer `src/StellarImperiums.Api/Contracts/Auth/LoginResponse.cs` :

```csharp
namespace StellarImperiums.Api.Contracts.Auth;

/// <summary>
/// Response of the login endpoint. The refresh token is delivered separately as an HttpOnly cookie.
/// </summary>
/// <param name="AccessToken">The serialized signed access token.</param>
/// <param name="ExpiresIn">The access token lifetime in seconds.</param>
/// <param name="TokenType">The token type to use in the Authorization header (always <c>Bearer</c>).</param>
/// <param name="MustChangePassword">Whether the user must change their password before playing.</param>
/// <param name="User">A summary of the authenticated user.</param>
public sealed record LoginResponse(
    string AccessToken,
    int ExpiresIn,
    string TokenType,
    bool MustChangePassword,
    UserSummary User);

/// <summary>
/// Summary of the authenticated user returned at login.
/// </summary>
/// <param name="Id">The user identifier.</param>
/// <param name="Username">The user's username.</param>
/// <param name="Email">The user's email address.</param>
/// <param name="Role">The user's role name.</param>
public sealed record UserSummary(int Id, string Username, string Email, string Role);
```

Créer `src/StellarImperiums.Api/Contracts/Auth/RefreshResponse.cs` :

```csharp
namespace StellarImperiums.Api.Contracts.Auth;

/// <summary>
/// Response of the refresh endpoint. The rotated refresh token is delivered as an HttpOnly cookie.
/// </summary>
/// <param name="AccessToken">The new serialized signed access token.</param>
/// <param name="ExpiresIn">The access token lifetime in seconds.</param>
/// <param name="TokenType">The token type to use in the Authorization header (always <c>Bearer</c>).</param>
public sealed record RefreshResponse(string AccessToken, int ExpiresIn, string TokenType);
```

- [ ] **Step 2: Étendre `AuthController`**

Dans `src/StellarImperiums.Api/Controllers/V1/AuthController.cs`, ajouter les usings :

```csharp
using Microsoft.AspNetCore.RateLimiting;
using StellarImperiums.Application.Tokens.Commands;
```

Puis ajouter dans la classe, après l'action `Register` :

```csharp
    private const string RefreshTokenCookieName = "refresh_token";
    private const string RefreshTokenCookiePath = "/api/v1/auth";
    private const string BearerTokenType = "Bearer";

    /// <summary>
    /// Authenticates a player with email and password.
    /// </summary>
    /// <param name="request">The login payload.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The access token and the user summary; the refresh token is set as an HttpOnly cookie.</returns>
    /// <response code="200">The credentials are valid.</response>
    /// <response code="400">The request payload failed validation.</response>
    /// <response code="401">The email or password is wrong.</response>
    /// <response code="403">The account is suspended.</response>
    /// <response code="429">Too many login attempts from this address.</response>
    [HttpPost("login")]
    [EnableRateLimiting("auth-login")]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult<LoginResponse>> Login(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken)
    {
        var command = new LoginUserCommand(request.Email, request.Password);
        var result = await messageBus
            .InvokeAsync<LoginUserResult>(command, cancellationToken)
            .ConfigureAwait(false);

        SetRefreshTokenCookie(result.RefreshTokenValue, result.RefreshTokenExpiresAt);

        var response = new LoginResponse(
            result.AccessToken,
            result.ExpiresInSeconds,
            BearerTokenType,
            result.MustChangePassword,
            new UserSummary(result.UserId, result.Username, result.Email, result.Role));

        return Ok(response);
    }

    /// <summary>
    /// Exchanges the refresh token cookie for a new access token and a rotated refresh token.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The new access token; the rotated refresh token replaces the cookie.</returns>
    /// <response code="200">The refresh token was valid and has been rotated.</response>
    /// <response code="401">The refresh token is missing, unknown, expired, revoked, or replayed.</response>
    /// <response code="403">The account is suspended.</response>
    [HttpPost("refresh")]
    [ProducesResponseType(typeof(RefreshResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<RefreshResponse>> Refresh(CancellationToken cancellationToken)
    {
        if (!Request.Cookies.TryGetValue(RefreshTokenCookieName, out var refreshTokenValue)
            || string.IsNullOrEmpty(refreshTokenValue))
        {
            return Problem(
                title: "Invalid refresh token",
                detail: "No refresh token cookie was provided.",
                statusCode: StatusCodes.Status401Unauthorized);
        }

        var result = await messageBus
            .InvokeAsync<RefreshTokenResult>(new RefreshTokenCommand(refreshTokenValue), cancellationToken)
            .ConfigureAwait(false);

        SetRefreshTokenCookie(result.RefreshTokenValue, result.RefreshTokenExpiresAt);

        return Ok(new RefreshResponse(result.AccessToken, result.ExpiresInSeconds, BearerTokenType));
    }

    /// <summary>
    /// Terminates the session by revoking the refresh token family and clearing the cookie.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>No content; logout is idempotent.</returns>
    /// <response code="204">The session is terminated (also returned when no cookie was sent).</response>
    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        if (Request.Cookies.TryGetValue(RefreshTokenCookieName, out var refreshTokenValue)
            && !string.IsNullOrEmpty(refreshTokenValue))
        {
            await messageBus
                .InvokeAsync(new LogoutCommand(refreshTokenValue), cancellationToken)
                .ConfigureAwait(false);
        }

        DeleteRefreshTokenCookie();
        return NoContent();
    }

    private void SetRefreshTokenCookie(string value, DateTimeOffset expiresAt) =>
        Response.Cookies.Append(RefreshTokenCookieName, value, BuildRefreshTokenCookieOptions(expiresAt));

    private void DeleteRefreshTokenCookie() =>
        Response.Cookies.Delete(RefreshTokenCookieName, BuildRefreshTokenCookieOptions(null));

    private static CookieOptions BuildRefreshTokenCookieOptions(DateTimeOffset? expiresAt) => new()
    {
        HttpOnly = true,
        Secure = true,
        SameSite = SameSiteMode.Strict,
        Path = RefreshTokenCookiePath,
        Expires = expiresAt
    };
```

- [ ] **Step 3: Étendre le middleware d'exceptions**

Dans `src/StellarImperiums.Api/Middleware/DomainExceptionMiddleware.cs`, ajouter le using :

```csharp
using StellarImperiums.Application.Tokens.Exceptions;
```

Puis insérer ces catches APRÈS le catch `EmailAlreadyTakenException` et AVANT le catch `DomainException` :

```csharp
        catch (InvalidCredentialsException ex)
        {
            logger.LogInformation("Login failure for {Path}.", context.Request.Path);
            await WriteProblemAsync(context, StatusCodes.Status401Unauthorized, "Invalid credentials", ex.Message).ConfigureAwait(false);
        }
        catch (RefreshTokenRejectedException ex)
        {
            logger.LogInformation("Refresh token rejected for {Path}.", context.Request.Path);
            await WriteProblemAsync(context, StatusCodes.Status401Unauthorized, "Invalid refresh token", ex.Message).ConfigureAwait(false);
        }
        catch (UserNotFoundException ex)
        {
            logger.LogInformation("Authenticated user no longer exists for {Path}.", context.Request.Path);
            await WriteProblemAsync(context, StatusCodes.Status401Unauthorized, "Unauthorized", ex.Message).ConfigureAwait(false);
        }
        catch (UserSuspendedException ex)
        {
            logger.LogInformation("Suspended account rejected for {Path}.", context.Request.Path);
            await WriteProblemAsync(context, StatusCodes.Status403Forbidden, "Account suspended", ex.Message).ConfigureAwait(false);
        }
```

- [ ] **Step 4: Vérifier la compilation**

Run: `dotnet build src/StellarImperiums.Api`
Expected: PASS (l'attribut `EnableRateLimiting` compile sans la policy — elle sera enregistrée en Task 11).

- [ ] **Step 5: Commit**

```bash
git add src/StellarImperiums.Api
git commit -m "feat(api): login, refresh and logout endpoints with HttpOnly refresh cookie"
```

---

### Task 11: Api — `UsersController`, JwtBearer, rate limiter, configuration

**Files:**
- Create: `src/StellarImperiums.Api/Contracts/Users/MeResponse.cs`
- Create: `src/StellarImperiums.Api/Controllers/V1/UsersController.cs`
- Modify: `src/StellarImperiums.Api/Program.cs`
- Modify: `src/StellarImperiums.Api/appsettings.json`

- [ ] **Step 1: Ajouter le package JwtBearer**

```bash
dotnet add src/StellarImperiums.Api package Microsoft.AspNetCore.Authentication.JwtBearer
```

- [ ] **Step 2: Créer le contract et le controller**

Créer `src/StellarImperiums.Api/Contracts/Users/MeResponse.cs` :

```csharp
namespace StellarImperiums.Api.Contracts.Users;

/// <summary>
/// Profile of the authenticated user returned by the me endpoint.
/// </summary>
/// <param name="Id">The user identifier.</param>
/// <param name="Username">The user's username.</param>
/// <param name="Email">The user's email address.</param>
/// <param name="Role">The user's role name.</param>
/// <param name="RegistrationDate">The registration timestamp (UTC).</param>
/// <param name="LastLoginAt">The last successful login timestamp (UTC), or <c>null</c>.</param>
public sealed record MeResponse(
    int Id,
    string Username,
    string Email,
    string Role,
    DateTimeOffset RegistrationDate,
    DateTimeOffset? LastLoginAt);
```

Créer `src/StellarImperiums.Api/Controllers/V1/UsersController.cs` :

```csharp
using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StellarImperiums.Api.Contracts.Users;
using StellarImperiums.Application.Users.Queries;
using Wolverine;

namespace StellarImperiums.Api.Controllers.V1;

/// <summary>
/// Endpoints exposing user profiles.
/// </summary>
[ApiController]
[Route("api/v1/users")]
[Produces("application/json")]
public sealed class UsersController(IMessageBus messageBus) : ControllerBase
{
    /// <summary>
    /// Returns the profile of the authenticated user.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The authenticated user's profile.</returns>
    /// <response code="200">The profile of the authenticated user.</response>
    /// <response code="401">The access token is missing, invalid, or its subject no longer exists.</response>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(MeResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<MeResponse>> GetMe(CancellationToken cancellationToken)
    {
        var subject = User.FindFirstValue("sub");
        if (!int.TryParse(subject, CultureInfo.InvariantCulture, out var userId))
        {
            return Unauthorized();
        }

        var result = await messageBus
            .InvokeAsync<CurrentUserResult>(new GetCurrentUserQuery(userId), cancellationToken)
            .ConfigureAwait(false);

        return Ok(new MeResponse(
            result.Id,
            result.Username,
            result.Email,
            result.Role,
            result.RegistrationDate,
            result.LastLoginAt));
    }
}
```

- [ ] **Step 3: Configurer `Program.cs`**

Dans `src/StellarImperiums.Api/Program.cs`, ajouter les usings :

```csharp
using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using StellarImperiums.Infrastructure.Security;
```

Après `builder.Services.AddControllers();`, insérer :

```csharp
var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
    ?? throw new InvalidOperationException(
        "The Jwt configuration section is missing. Set Jwt:SigningKey via user-secrets or environment variable.");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtOptions.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SigningKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30),
            NameClaimType = JwtRegisteredClaimNames.UniqueName,
            RoleClaimType = "role"
        };
    });
builder.Services.AddAuthorization();

var loginPermitLimit = builder.Configuration.GetValue("RateLimiting:AuthLogin:PermitLimit", 5);
var loginWindowSeconds = builder.Configuration.GetValue("RateLimiting:AuthLogin:WindowSeconds", 60);

builder.Services.AddRateLimiter(limiter =>
{
    limiter.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    limiter.AddPolicy("auth-login", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = loginPermitLimit,
            Window = TimeSpan.FromSeconds(loginWindowSeconds)
        }));
});
```

Puis remplacer la séquence middleware :

```csharp
app.UseMiddleware<DomainExceptionMiddleware>();

app.MapControllers();
```

par :

```csharp
app.UseMiddleware<DomainExceptionMiddleware>();

app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
```

- [ ] **Step 4: Compléter `appsettings.json`**

Dans `src/StellarImperiums.Api/appsettings.json`, ajouter la section (au même niveau que `Logging` ; PAS de `SigningKey` ici) :

```json
  "Jwt": {
    "Issuer": "StellarImperiums",
    "Audience": "StellarImperiums",
    "AccessTokenLifetimeMinutes": 15,
    "RefreshTokenLifetimeDays": 30
  }
```

Configurer la clé de dev via user-secrets (générer une valeur aléatoire, ne pas réutiliser celle-ci ailleurs qu'en local) :

```bash
dotnet user-secrets init --project src/StellarImperiums.Api
dotnet user-secrets set "Jwt:SigningKey" "$(openssl rand -base64 48)" --project src/StellarImperiums.Api
```

Sous PowerShell sans openssl : `[Convert]::ToBase64String((1..48 | ForEach-Object { Get-Random -Maximum 256 }))` ou toute chaîne aléatoire ≥ 32 caractères.

- [ ] **Step 5: Vérifier compilation et démarrage**

Run: `dotnet build && dotnet test tests/StellarImperiums.Domain.Tests tests/StellarImperiums.Application.Tests tests/StellarImperiums.Infrastructure.Tests`
Expected: PASS.

- [ ] **Step 6: Commit**

```bash
git add src/StellarImperiums.Api
git commit -m "feat(api): protected me endpoint, JWT bearer validation and login rate limiting"
```

---

### Task 12: Tests d'intégration — Testcontainers PostgreSQL

**Files:**
- Modify: `tests/StellarImperiums.Api.IntegrationTests/StellarImperiums.Api.IntegrationTests.csproj` (packages)
- Create: `tests/StellarImperiums.Api.IntegrationTests/StellarApiFactory.cs`
- Create: `tests/StellarImperiums.Api.IntegrationTests/AuthFlowTests.cs`
- Create: `tests/StellarImperiums.Api.IntegrationTests/RateLimitingTests.cs`

Prérequis : Docker Desktop démarré.

- [ ] **Step 1: Ajouter les packages**

```bash
dotnet add tests/StellarImperiums.Api.IntegrationTests package Microsoft.AspNetCore.Mvc.Testing
dotnet add tests/StellarImperiums.Api.IntegrationTests package Testcontainers.PostgreSql
dotnet add tests/StellarImperiums.Api.IntegrationTests package Shouldly
```

- [ ] **Step 2: Créer la factory**

Créer `tests/StellarImperiums.Api.IntegrationTests/StellarApiFactory.cs` :

```csharp
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using StellarImperiums.Infrastructure.Persistence;
using Testcontainers.PostgreSql;

namespace StellarImperiums.Api.IntegrationTests;

public class StellarApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:17-alpine")
        .Build();

    protected virtual int LoginPermitLimit => 1000;

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
        using var scope = Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<StellarDbContext>().Database.MigrateAsync();
    }

    public new async Task DisposeAsync()
    {
        await base.DisposeAsync();
        await _postgres.DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("ConnectionStrings:Postgres", _postgres.GetConnectionString());
        builder.UseSetting("Jwt:Issuer", "StellarImperiums.Tests");
        builder.UseSetting("Jwt:Audience", "StellarImperiums.Tests");
        builder.UseSetting("Jwt:SigningKey", "integration-tests-signing-key-0123456789abcdef0123456789abcdef");
        builder.UseSetting("RateLimiting:AuthLogin:PermitLimit", LoginPermitLimit.ToString());
    }

    public HttpClient CreateApiClient(bool handleCookies = true) =>
        CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
            HandleCookies = handleCookies
        });
}

[CollectionDefinition("api")]
public sealed class ApiCollection : ICollectionFixture<StellarApiFactory>;
```

Note : `BaseAddress` en https est indispensable — le cookie est marqué `Secure`, le conteneur de cookies du client le rejetterait en http.

- [ ] **Step 3: Écrire les tests de flux**

Créer `tests/StellarImperiums.Api.IntegrationTests/AuthFlowTests.cs` :

```csharp
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Shouldly;

namespace StellarImperiums.Api.IntegrationTests;

[Collection("api")]
public class AuthFlowTests(StellarApiFactory factory)
{
    private sealed record RegisterPayload(string Username, string Email, string Password);
    private sealed record LoginPayload(string Email, string Password);

    private static RegisterPayload NewAccount()
    {
        var suffix = Guid.NewGuid().ToString("N")[..10];
        return new RegisterPayload($"Cmdr_{suffix}", $"{suffix}@stellar.io", "P@ssword12345!");
    }

    private static async Task<JsonElement> ReadJsonAsync(HttpResponseMessage response) =>
        JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;

    [Fact]
    public async Task FullJourney_Register_Login_Me_Refresh_Logout()
    {
        var client = factory.CreateApiClient();
        var account = NewAccount();

        var register = await client.PostAsJsonAsync("/api/v1/auth/register", account);
        register.StatusCode.ShouldBe(HttpStatusCode.Created);

        var login = await client.PostAsJsonAsync(
            "/api/v1/auth/login", new LoginPayload(account.Email, account.Password));
        login.StatusCode.ShouldBe(HttpStatusCode.OK);
        login.Headers.TryGetValues("Set-Cookie", out var cookies).ShouldBeTrue();
        cookies!.ShouldContain(c => c.StartsWith("refresh_token=") && c.Contains("httponly"), customMessage: string.Join('\n', cookies!));
        var loginBody = await ReadJsonAsync(login);
        var accessToken = loginBody.GetProperty("accessToken").GetString();
        accessToken.ShouldNotBeNullOrEmpty();
        loginBody.GetProperty("tokenType").GetString().ShouldBe("Bearer");
        loginBody.GetProperty("user").GetProperty("username").GetString().ShouldBe(account.Username);

        using var meRequest = new HttpRequestMessage(HttpMethod.Get, "/api/v1/users/me");
        meRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        var me = await client.SendAsync(meRequest);
        me.StatusCode.ShouldBe(HttpStatusCode.OK);
        var meBody = await ReadJsonAsync(me);
        meBody.GetProperty("email").GetString().ShouldBe(account.Email);
        meBody.GetProperty("lastLoginAt").ValueKind.ShouldNotBe(JsonValueKind.Null);

        var refresh = await client.PostAsync("/api/v1/auth/refresh", null);
        refresh.StatusCode.ShouldBe(HttpStatusCode.OK);
        var refreshBody = await ReadJsonAsync(refresh);
        refreshBody.GetProperty("accessToken").GetString().ShouldNotBeNullOrEmpty();

        var logout = await client.PostAsync("/api/v1/auth/logout", null);
        logout.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var refreshAfterLogout = await client.PostAsync("/api/v1/auth/refresh", null);
        refreshAfterLogout.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_WithWrongPassword_Returns401()
    {
        var client = factory.CreateApiClient();
        var account = NewAccount();
        await client.PostAsJsonAsync("/api/v1/auth/register", account);

        var login = await client.PostAsJsonAsync(
            "/api/v1/auth/login", new LoginPayload(account.Email, "Wrong-Password-99!"));

        login.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_WithUnknownEmail_Returns401()
    {
        var client = factory.CreateApiClient();

        var login = await client.PostAsJsonAsync(
            "/api/v1/auth/login", new LoginPayload("ghost@stellar.io", "P@ssword12345!"));

        login.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Me_WithoutToken_Returns401()
    {
        var client = factory.CreateApiClient();

        var me = await client.GetAsync("/api/v1/users/me");

        me.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Refresh_WithReplayedRotatedToken_RevokesWholeFamily()
    {
        var rawClient = factory.CreateApiClient(handleCookies: false);
        var account = NewAccount();
        await rawClient.PostAsJsonAsync("/api/v1/auth/register", account);

        var login = await rawClient.PostAsJsonAsync(
            "/api/v1/auth/login", new LoginPayload(account.Email, account.Password));
        var firstCookie = ExtractRefreshCookie(login);

        var firstRefresh = await SendRefreshAsync(rawClient, firstCookie);
        firstRefresh.StatusCode.ShouldBe(HttpStatusCode.OK);
        var secondCookie = ExtractRefreshCookie(firstRefresh);

        var replay = await SendRefreshAsync(rawClient, firstCookie);
        replay.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);

        var familyDead = await SendRefreshAsync(rawClient, secondCookie);
        familyDead.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    private static string ExtractRefreshCookie(HttpResponseMessage response)
    {
        response.Headers.TryGetValues("Set-Cookie", out var values).ShouldBeTrue();
        var cookie = values!.First(c => c.StartsWith("refresh_token="));
        return cookie.Split(';')[0];
    }

    private static Task<HttpResponseMessage> SendRefreshAsync(HttpClient client, string cookie)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/refresh");
        request.Headers.Add("Cookie", cookie);
        return client.SendAsync(request);
    }
}
```

- [ ] **Step 4: Écrire le test de rate limiting**

Créer `tests/StellarImperiums.Api.IntegrationTests/RateLimitingTests.cs` :

```csharp
using System.Net;
using System.Net.Http.Json;
using Shouldly;

namespace StellarImperiums.Api.IntegrationTests;

public sealed class RateLimitedApiFactory : StellarApiFactory
{
    protected override int LoginPermitLimit => 5;
}

public class RateLimitingTests(RateLimitedApiFactory factory) : IClassFixture<RateLimitedApiFactory>
{
    private sealed record LoginPayload(string Email, string Password);

    [Fact]
    public async Task Login_BeyondFiveAttemptsPerMinute_Returns429()
    {
        var client = factory.CreateApiClient();
        var payload = new LoginPayload("ratelimit@stellar.io", "Wrong-Password-99!");

        for (var attempt = 1; attempt <= 5; attempt++)
        {
            var response = await client.PostAsJsonAsync("/api/v1/auth/login", payload);
            response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized, $"attempt {attempt}");
        }

        var sixth = await client.PostAsJsonAsync("/api/v1/auth/login", payload);
        sixth.StatusCode.ShouldBe(HttpStatusCode.TooManyRequests);
    }
}
```

- [ ] **Step 5: Lancer les tests d'intégration**

Run: `dotnet test tests/StellarImperiums.Api.IntegrationTests`
Expected: PASS — 6 tests verts (le premier run télécharge l'image `postgres:17-alpine`).

- [ ] **Step 6: Commit**

```bash
git add tests/StellarImperiums.Api.IntegrationTests
git commit -m "test(api): end-to-end authentication flows on a real PostgreSQL container"
```

---

### Task 13: Finalisation — format, suite complète, push, PR

- [ ] **Step 1: Vérifier le format et la suite complète (parité CI)**

```bash
dotnet format --verify-no-changes
dotnet build
dotnet test
```

Expected: format sans diff, build sans warning bloquant, tous les tests verts (~120). Si `dotnet format` signale des écarts : `dotnet format` puis commit `style: apply dotnet format`.

- [ ] **Step 2: Vérifier l'absence de vulnérabilités sur les nouveaux packages**

```bash
dotnet list package --vulnerable --include-transitive
```

Expected: aucune vulnérabilité. Sinon, monter la version du package concerné.

- [ ] **Step 3: Mettre à jour le fichier `.http`**

Ajouter à `src/StellarImperiums.Api/StellarImperiums.Api.http` :

```
### Login
POST {{HostAddress}}/api/v1/auth/login
Content-Type: application/json

{
  "email": "vega@stellar.io",
  "password": "P@ssword12345!"
}

### Refresh (le cookie refresh_token doit être présent)
POST {{HostAddress}}/api/v1/auth/refresh

### Me
GET {{HostAddress}}/api/v1/users/me
Authorization: Bearer {{accessToken}}

### Logout
POST {{HostAddress}}/api/v1/auth/logout
```

```bash
git add src/StellarImperiums.Api/StellarImperiums.Api.http
git commit -m "docs: http samples for the authentication endpoints"
```

- [ ] **Step 4: Push et PR**

```bash
git push -u origin feature/auth-login-jwt
gh pr create --base develop --title "JWT authentication: login, refresh rotation, logout, protected me endpoint" --body "## Summary
- POST /api/v1/auth/login: email + password, access JWT (15 min, HS256) and refresh token rotation family, timing attack mitigation, generic 401
- POST /api/v1/auth/refresh: rotation with replay detection (whole family revoked on reuse), HttpOnly Secure SameSite=Strict cookie scoped to /api/v1/auth
- POST /api/v1/auth/logout: idempotent family revocation
- GET /api/v1/users/me: first [Authorize] endpoint, sub claim lookup
- Fixed-window rate limiting on login (5/min/IP, configurable)
- RefreshToken rich domain entity, jeton_rafraichissement table + migration
- ~50 unit tests + 6 end-to-end tests on Testcontainers PostgreSQL

## Design
docs/superpowers/specs/2026-06-06-auth-login-jwt-design.md"
```

La PR attend le GO explicite de Pierrick avant tout merge (`--no-ff` vers develop).

---

## Self-Review (effectuée à l'écriture du plan)

- **Couverture spec** : §2 décisions → Tasks 1, 8, 10, 11 ; §3.1 → Task 1 ; §3.2 → Tasks 2-7 ; §3.3 → Tasks 8-9 ; §3.4 → Tasks 10-11 ; §4 flux → Tasks 4-6, 10 ; §5 sécurité → Tasks 8, 10, 11 ; §6 tests → chaque task TDD + Task 12. Hors périmètre (§7) respecté : aucun endpoint reset/change-password.
- **Placeholders** : aucun TBD/TODO ; chaque step code contient le code complet.
- **Cohérence des types** : `RefreshToken.Create(int, string, Guid, DateTimeOffset, DateTimeOffset?)`, `Rotate(string, DateTimeOffset, DateTimeOffset)`, `RefreshTokenMaterial(Value, Hash, ExpiresAt)`, `LoginUserResult` à 9 champs — identiques entre tasks d'implémentation et tasks de test.
