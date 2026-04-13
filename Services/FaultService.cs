using EquipmentRental.Data;

namespace EquipmentRental.Services;

public class FaultService(AppDbContext db)
{
    private readonly AppDbContext _db = db;
}
