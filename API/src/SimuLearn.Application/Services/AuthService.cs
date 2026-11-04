using System.Net.Mail;
using Microsoft.EntityFrameworkCore;
using SimuLearn.Application.Abstractions;
using SimuLearn.Application.Contracts;
using SimuLearn.Domain.Common;
using SimuLearn.Domain.Identity;

namespace SimuLearn.Application.Services;

public sealed class AuthService(
    IApplicationDbContext db,
    IPasswordHasher passwordHasher,
    ITokenService tokenService)
{
    private const int MinimumPasswordLength = 10;

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken)
    {
        if (request.Role is not (RoleName.Student or RoleName.Teacher))
        {
            throw new ForbiddenException("Self-registration is only available for students and teachers.");
        }

        var errors = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);

        var email = NormalizeEmail(request.Email);
        if (!IsValidEmail(email))
        {
            errors["email"] = ["Enter a valid email address."];
        }

        var displayName = request.DisplayName?.Trim() ?? string.Empty;
        if (displayName.Length is < 2 or > 120)
        {
            errors["displayName"] = ["Enter a display name between 2 and 120 characters."];
        }

        ValidatePassword(request.Password, errors);

        if (!string.IsNullOrWhiteSpace(request.GuardianEmail) && !IsValidEmail(NormalizeEmail(request.GuardianEmail)))
        {
            errors["guardianEmail"] = ["Enter a valid guardian email address."];
        }

        var age = AgeInYears(request.DateOfBirth);
        if (age < 13 && !request.GuardianConsent)
        {
            errors["guardianConsent"] =
                ["A guardian must provide consent before an account can be created for a learner under 13."];
        }

        if (request.GuardianConsent && string.IsNullOrWhiteSpace(request.GuardianEmail))
        {
            errors["guardianEmail"] = ["A guardian email is required when guardian consent is recorded."];
        }

        if (errors.Count > 0)
        {
            throw new DomainValidationException("The registration details are not valid.", errors);
        }

        if (await db.Users.AnyAsync(u => u.NormalizedEmail == email, cancellationToken))
        {
            throw new ConflictException("An account with that email already exists.", "email_in_use");
        }

        if (request.InstitutionId is { } institutionId
            && !await db.Institutions.AnyAsync(i => i.Id == institutionId && i.IsActive, cancellationToken))
        {
            throw new NotFoundException("Institution", institutionId);
        }

        var user = new User
        {
            Email = request.Email.Trim(),
            NormalizedEmail = email,
            DisplayName = displayName,
            PasswordHash = passwordHasher.Hash(request.Password),
            DateOfBirth = request.DateOfBirth,
            GuardianEmail = request.GuardianConsent ? NormalizeEmail(request.GuardianEmail!) : null,
            GuardianConsentAt = request.GuardianConsent ? DateTimeOffset.UtcNow : null,
            InstitutionId = request.InstitutionId,
            Roles = [new UserRole { Role = request.Role }],
        };

        db.Users.Add(user);
        await db.SaveChangesAsync(cancellationToken);

        return await IssueTokenAsync(user, cancellationToken);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        var email = NormalizeEmail(request.Email);
        var user = await db.Users
            .Include(u => u.Roles)
            .Include(u => u.Institution)
            .SingleOrDefaultAsync(u => u.NormalizedEmail == email, cancellationToken);

        if (user is null || !passwordHasher.Verify(request.Password ?? string.Empty, user.PasswordHash))
        {
            // Deliberately identical response for unknown accounts and wrong passwords.
            throw new UnauthorizedException();
        }

        if (user.Status == UserStatus.Disabled)
        {
            throw new ForbiddenException("This account has been disabled. Contact your administrator.");
        }

        user.LastSignInAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);

        return await IssueTokenAsync(user, cancellationToken);
    }

    public async Task<UserProfile> GetProfileAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await db.Users
            .AsNoTracking()
            .Include(u => u.Roles)
            .Include(u => u.Institution)
            .SingleOrDefaultAsync(u => u.Id == userId, cancellationToken)
            ?? throw new NotFoundException("User", userId);

        return ToProfile(user);
    }

    public static UserProfile ToProfile(User user) =>
        new(
            user.Id,
            user.Email,
            user.DisplayName,
            user.Roles.Select(r => r.Role).OrderBy(r => r).ToArray(),
            user.Status,
            user.InstitutionId,
            user.Institution?.Name,
            user.GuardianEmail,
            user.GuardianConsentAt,
            user.CreatedAt);

    private async Task<AuthResponse> IssueTokenAsync(User user, CancellationToken cancellationToken)
    {
        var issuedAt = DateTimeOffset.UtcNow;
        var token = tokenService.CreateAccessToken(user, issuedAt);

        if (user.Institution is null && user.InstitutionId is { } institutionId)
        {
            user.Institution = await db.Institutions.FindAsync([institutionId], cancellationToken);
        }

        return new AuthResponse(token.Value, token.ExpiresAt, ToProfile(user));
    }

    private static void ValidatePassword(string? password, IDictionary<string, string[]> errors)
    {
        if (string.IsNullOrWhiteSpace(password) || password.Length < MinimumPasswordLength)
        {
            errors["password"] = [$"Use a password of at least {MinimumPasswordLength} characters."];
            return;
        }

        if (password.Length > 128)
        {
            errors["password"] = ["Use a password of no more than 128 characters."];
            return;
        }

        var hasLetter = password.Any(char.IsLetter);
        var hasDigit = password.Any(char.IsDigit);
        if (!hasLetter || !hasDigit)
        {
            errors["password"] = ["Include at least one letter and one number in the password."];
        }
    }

    private static int AgeInYears(DateOnly? dateOfBirth)
    {
        if (dateOfBirth is not { } dob)
        {
            return 18;
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        if (dob > today)
        {
            return 0;
        }

        var age = today.Year - dob.Year;
        return dob > today.AddYears(-age) ? age - 1 : age;
    }

    public static string NormalizeEmail(string? email) =>
        (email ?? string.Empty).Trim().ToLowerInvariant();

    private static bool IsValidEmail(string email)
    {
        if (email.Length is 0 or > 254 || email.Contains(',', StringComparison.Ordinal))
        {
            return false;
        }

        return MailAddress.TryCreate(email, out var parsed)
               && string.Equals(parsed.Address, email, StringComparison.OrdinalIgnoreCase);
    }
}
