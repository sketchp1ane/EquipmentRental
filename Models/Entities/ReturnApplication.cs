using EquipmentRental.Models;

namespace EquipmentRental.Models.Entities;

public class ReturnApplication
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public string ApplicantId { get; set; } = string.Empty;
    public DateOnly ActualReturnDate { get; set; }
    public string? ConditionDesc { get; set; }
    public ReturnApplicationStatus Status { get; set; } = ReturnApplicationStatus.PendingEvaluation;
    public DateTime CreatedAt { get; set; }

    public DispatchOrder Order { get; set; } = null!;
    public ApplicationUser Applicant { get; set; } = null!;
    public ReturnEvaluation? Evaluation { get; set; }
}
