using EquipmentRental.Data;

namespace EquipmentRental.Services;

public class ReportService(AppDbContext db)
{
    private readonly AppDbContext _db = db;
}
