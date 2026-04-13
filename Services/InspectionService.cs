using EquipmentRental.Data;

namespace EquipmentRental.Services;

public class InspectionService(AppDbContext db)
{
    private readonly AppDbContext _db = db;
}
