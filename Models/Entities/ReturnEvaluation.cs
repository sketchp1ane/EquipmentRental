namespace EquipmentRental.Models.Entities;

public class ReturnEvaluation
{
    public int Id { get; set; }
    public int ReturnAppId { get; set; }
    public string EvaluatorId { get; set; } = string.Empty;
    public int AppearanceScore { get; set; }
    public int FunctionScore { get; set; }
    public string? DamageDesc { get; set; }
    public decimal Deduction { get; set; }
    public decimal RefundAmount { get; set; }
    public string? Remark { get; set; }
    public DateTime EvaluatedAt { get; set; }

    public ReturnApplication ReturnApplication { get; set; } = null!;
    public ApplicationUser Evaluator { get; set; } = null!;
}
