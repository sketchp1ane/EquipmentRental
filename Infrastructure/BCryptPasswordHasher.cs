using EquipmentRental.Models.Entities;
using Microsoft.AspNetCore.Identity;

namespace EquipmentRental.Infrastructure;

public class BCryptPasswordHasher : IPasswordHasher<ApplicationUser>
{
    private const int WorkFactor = 12;

    public string HashPassword(ApplicationUser user, string password)
        => BCrypt.Net.BCrypt.HashPassword(password, WorkFactor);

    public PasswordVerificationResult VerifyHashedPassword(
        ApplicationUser user, string hashedPassword, string providedPassword)
    {
        if (!BCrypt.Net.BCrypt.Verify(providedPassword, hashedPassword))
            return PasswordVerificationResult.Failed;

        bool needsRehash = BCrypt.Net.BCrypt.PasswordNeedsRehash(hashedPassword, WorkFactor);
        return needsRehash
            ? PasswordVerificationResult.SuccessRehashNeeded
            : PasswordVerificationResult.Success;
    }
}
