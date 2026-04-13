using EquipmentRental.Data;

namespace EquipmentRental.Services;

public class EquipmentService(AppDbContext db)
{
    private readonly AppDbContext _db = db;
}
