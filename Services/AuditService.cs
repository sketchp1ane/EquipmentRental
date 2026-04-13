using EquipmentRental.Data;

namespace EquipmentRental.Services;

public class AuditService(AppDbContext db)
{
    private readonly AppDbContext _db = db;
}
