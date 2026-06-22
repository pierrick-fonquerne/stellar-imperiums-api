using Shouldly;
using StellarImperiums.Domain.Users;
using StellarImperiums.Domain.Users.Exceptions;

namespace StellarImperiums.Domain.Tests.Users;

public class UserTests
{
    private const string ValidHash =
        "$argon2id$v=19$m=65536,t=3,p=4$0FCks4yDhpw2PqOmKg7FAw$EjBGRNLgHES+HUezNXUaQgZmM+Q/fFo2Uhnanx4DFEY";

    private static User CreateStandardUser() => User.Create(
        Username.Create("Cmdr_Vega"),
        Email.Create("vega@stellar.io"),
        PasswordHash.Create(ValidHash));

    [Fact]
    public void Create_WithValidData_InitializesProperties()
    {
        var before = DateTimeOffset.UtcNow;
        var user = CreateStandardUser();
        var after = DateTimeOffset.UtcNow;

        user.Username.Value.ShouldBe("Cmdr_Vega");
        user.Email.Value.ShouldBe("vega@stellar.io");
        user.Role.ShouldBe(UserRole.Player);
        user.IsSuspended.ShouldBeFalse();
        user.MustChangePassword.ShouldBeFalse();
        user.LastLoginAt.ShouldBeNull();
        user.PasswordResetToken.ShouldBeNull();
        user.PasswordResetTokenExpiresAt.ShouldBeNull();
        user.RegistrationDate.ShouldBeInRange(before, after);
    }

    [Fact]
    public void Create_WithAdminRole_AssignsTheRole()
    {
        var admin = User.Create(
            Username.Create("admin"),
            Email.Create("admin@stellar.io"),
            PasswordHash.Create(ValidHash),
            role: UserRole.Admin,
            mustChangePassword: true);

        admin.Role.ShouldBe(UserRole.Admin);
        admin.MustChangePassword.ShouldBeTrue();
    }

    [Fact]
    public void Create_WithNullArguments_Throws()
    {
        Should.Throw<ArgumentNullException>(() => User.Create(null!, Email.Create("a@b.io"), PasswordHash.Create(ValidHash)));
        Should.Throw<ArgumentNullException>(() => User.Create(Username.Create("abc"), null!, PasswordHash.Create(ValidHash)));
        Should.Throw<ArgumentNullException>(() => User.Create(Username.Create("abc"), Email.Create("a@b.io"), null!));
    }

    [Fact]
    public void ChangePassword_UpdatesHashAndResetsFlags()
    {
        var user = User.Create(
            Username.Create("Cmdr_Vega"),
            Email.Create("vega@stellar.io"),
            PasswordHash.Create(ValidHash),
            mustChangePassword: true);

        user.StartPasswordReset("token-abc", DateTimeOffset.UtcNow.AddHours(1));

        var newHash = PasswordHash.Create(ValidHash.Replace("EjBGRNLgHES", "ZzZzZzZzZzZ"));
        user.ChangePassword(newHash);

        user.PasswordHash.ShouldBe(newHash);
        user.MustChangePassword.ShouldBeFalse();
        user.PasswordResetToken.ShouldBeNull();
        user.PasswordResetTokenExpiresAt.ShouldBeNull();
    }

    [Fact]
    public void Suspend_FromActiveToSuspended_OK()
    {
        var user = CreateStandardUser();

        user.Suspend();

        user.IsSuspended.ShouldBeTrue();
    }

    [Fact]
    public void Suspend_AlreadySuspended_Throws()
    {
        var user = CreateStandardUser();
        user.Suspend();

        Should.Throw<UserAlreadySuspendedException>(() => user.Suspend());
    }

    [Fact]
    public void Reactivate_FromSuspendedToActive_OK()
    {
        var user = CreateStandardUser();
        user.Suspend();

        user.Reactivate();

        user.IsSuspended.ShouldBeFalse();
    }

    [Fact]
    public void Reactivate_NotSuspended_Throws()
    {
        var user = CreateStandardUser();

        Should.Throw<UserNotSuspendedException>(() => user.Reactivate());
    }

    [Fact]
    public void RecordLogin_UpdatesLastLoginAt()
    {
        var user = CreateStandardUser();
        var when = new DateTimeOffset(2026, 5, 24, 12, 0, 0, TimeSpan.Zero);

        user.RecordLogin(when);

        user.LastLoginAt.ShouldBe(when);
    }

    [Fact]
    public void StartPasswordReset_WithValidToken_StoresValues()
    {
        var user = CreateStandardUser();
        var expiresAt = DateTimeOffset.UtcNow.AddHours(1);

        user.StartPasswordReset("token-xyz", expiresAt);

        user.PasswordResetToken.ShouldBe("token-xyz");
        user.PasswordResetTokenExpiresAt.ShouldBe(expiresAt);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void StartPasswordReset_WithEmptyToken_Throws(string? token)
    {
        var user = CreateStandardUser();

        Should.Throw<InvalidPasswordResetException>(() =>
            user.StartPasswordReset(token!, DateTimeOffset.UtcNow.AddHours(1)));
    }

    [Fact]
    public void StartPasswordReset_WithPastExpiry_Throws()
    {
        var user = CreateStandardUser();

        Should.Throw<InvalidPasswordResetException>(() =>
            user.StartPasswordReset("token", DateTimeOffset.UtcNow.AddMinutes(-1)));
    }

    [Fact]
    public void CompletePasswordReset_WithValidNonExpiredToken_ChangesPasswordAndClearsToken()
    {
        var user = CreateStandardUser();
        var baseTime = DateTimeOffset.UtcNow;
        user.StartPasswordReset("token-hash", baseTime.AddHours(1));
        var newHash = PasswordHash.Create(ValidHash.Replace("EjBGRNLgHES", "ZzZzZzZzZzZ"));

        user.CompletePasswordReset(newHash, baseTime.AddMinutes(30));

        user.PasswordHash.ShouldBe(newHash);
        user.PasswordResetToken.ShouldBeNull();
        user.PasswordResetTokenExpiresAt.ShouldBeNull();
    }

    [Fact]
    public void CompletePasswordReset_WithExpiredToken_ThrowsInvalidPasswordReset()
    {
        var user = CreateStandardUser();
        var baseTime = DateTimeOffset.UtcNow;
        var originalHash = user.PasswordHash;
        user.StartPasswordReset("token-hash", baseTime.AddHours(1));
        var newHash = PasswordHash.Create(ValidHash.Replace("EjBGRNLgHES", "ZzZzZzZzZzZ"));

        Should.Throw<InvalidPasswordResetException>(() =>
            user.CompletePasswordReset(newHash, baseTime.AddHours(2)));

        user.PasswordHash.ShouldBe(originalHash);
    }

    [Fact]
    public void CompletePasswordReset_WithNoActiveToken_ThrowsInvalidPasswordReset()
    {
        var user = CreateStandardUser();
        var newHash = PasswordHash.Create(ValidHash.Replace("EjBGRNLgHES", "ZzZzZzZzZzZ"));

        Should.Throw<InvalidPasswordResetException>(() =>
            user.CompletePasswordReset(newHash, DateTimeOffset.UtcNow));
    }
}
