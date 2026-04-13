using EquipmentRental.Data;

namespace EquipmentRental.Services;

public class VerificationService(AppDbContext db)
{
    private readonly AppDbContext _db = db;
}
