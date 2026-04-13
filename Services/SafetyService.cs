using EquipmentRental.Data;

namespace EquipmentRental.Services;

public class SafetyService(AppDbContext db)
{
    private readonly AppDbContext _db = db;
}
