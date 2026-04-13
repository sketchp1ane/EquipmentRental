using EquipmentRental.Data;

namespace EquipmentRental.Services;

public class DispatchService(AppDbContext db)
{
    private readonly AppDbContext _db = db;
}
