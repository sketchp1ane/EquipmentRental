using EquipmentRental.Data;

namespace EquipmentRental.Services;

public class QualificationService(AppDbContext db)
{
    private readonly AppDbContext _db = db;
}
