using EquipmentRental.Data;

namespace EquipmentRental.Services;

public class ReturnService(AppDbContext db)
{
    private readonly AppDbContext _db = db;
}
