using EquipmentRental.Models;
using EquipmentRental.Models.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace EquipmentRental.Data;

public class AppDbContext : IdentityDbContext<ApplicationUser>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<EquipmentCategory> EquipmentCategories { get; set; }
    public DbSet<Equipment> Equipments { get; set; }
    public DbSet<EquipmentImage> EquipmentImages { get; set; }
    public DbSet<Qualification> Qualifications { get; set; }
    public DbSet<AuditRecord> AuditRecords { get; set; }
    public DbSet<DispatchRequest> DispatchRequests { get; set; }
    public DbSet<DispatchOrder> DispatchOrders { get; set; }
    public DbSet<Contract> Contracts { get; set; }
    public DbSet<EntryVerification> EntryVerifications { get; set; }
    public DbSet<SafetyBriefing> SafetyBriefings { get; set; }
    public DbSet<BriefingParticipant> BriefingParticipants { get; set; }
    public DbSet<InspectionRecord> InspectionRecords { get; set; }
    public DbSet<InspectionImage> InspectionImages { get; set; }
    public DbSet<FaultReport> FaultReports { get; set; }
    public DbSet<FaultImage> FaultImages { get; set; }
    public DbSet<ReturnApplication> ReturnApplications { get; set; }
    public DbSet<ReturnEvaluation> ReturnEvaluations { get; set; }
    public DbSet<Notification> Notifications { get; set; }
    public DbSet<OperationLog> OperationLogs { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // ── ApplicationUser ───────────────────────────────────────────────
        builder.Entity<ApplicationUser>(e =>
        {
            e.Property(u => u.RealName).HasMaxLength(50).IsRequired();
            e.Property(u => u.IsActive).HasDefaultValue(true);
        });

        // ── EquipmentCategory ─────────────────────────────────────────────
        builder.Entity<EquipmentCategory>(e =>
        {
            e.HasKey(c => c.Id);
            e.Property(c => c.Name).HasMaxLength(50).IsRequired();
            e.Property(c => c.SortOrder).HasDefaultValue(0);
            e.HasOne(c => c.Parent)
             .WithMany(c => c.Children)
             .HasForeignKey(c => c.ParentId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // ── Equipment ─────────────────────────────────────────────────────
        builder.Entity<Equipment>(e =>
        {
            e.HasKey(eq => eq.Id);
            e.Property(eq => eq.EquipmentNo).HasMaxLength(50).IsRequired();
            e.HasIndex(eq => eq.EquipmentNo).IsUnique();
            e.Property(eq => eq.Name).HasMaxLength(100).IsRequired();
            e.Property(eq => eq.BrandModel).HasMaxLength(100).IsRequired();
            e.Property(eq => eq.FactoryNo).HasMaxLength(100);
            e.Property(eq => eq.TechSpecs).HasMaxLength(500);
            e.Property(eq => eq.OwnedBy).HasMaxLength(100).IsRequired();
            e.Property(eq => eq.OriginalValue).HasColumnType("decimal(12,2)");
            e.Property(eq => eq.Remark).HasMaxLength(500);
            e.Property(eq => eq.Status).HasDefaultValue(EquipmentStatus.PendingReview);
            e.HasOne(eq => eq.Category)
             .WithMany(c => c.Equipments)
             .HasForeignKey(eq => eq.CategoryId)
             .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(eq => eq.CreatedBy)
             .WithMany()
             .HasForeignKey(eq => eq.CreatedById)
             .OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(eq => eq.Status);
            e.HasIndex(eq => eq.CategoryId);
        });

        // ── EquipmentImage ────────────────────────────────────────────────
        builder.Entity<EquipmentImage>(e =>
        {
            e.HasKey(i => i.Id);
            e.Property(i => i.FilePath).HasMaxLength(500).IsRequired();
            e.HasOne(i => i.Equipment)
             .WithMany(eq => eq.Images)
             .HasForeignKey(i => i.EquipmentId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // ── Qualification ─────────────────────────────────────────────────
        builder.Entity<Qualification>(e =>
        {
            e.HasKey(q => q.Id);
            e.Property(q => q.CertNo).HasMaxLength(100);
            e.Property(q => q.IssuedBy).HasMaxLength(100);
            e.Property(q => q.FilePath).HasMaxLength(500);
            e.HasOne(q => q.Equipment)
             .WithMany(eq => eq.Qualifications)
             .HasForeignKey(q => q.EquipmentId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(q => q.ValidTo);
        });

        // ── AuditRecord ───────────────────────────────────────────────────
        builder.Entity<AuditRecord>(e =>
        {
            e.HasKey(a => a.Id);
            e.Property(a => a.Remark).HasMaxLength(500);
            e.HasOne(a => a.Equipment)
             .WithMany(eq => eq.AuditRecords)
             .HasForeignKey(a => a.EquipmentId)
             .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(a => a.Auditor)
             .WithMany()
             .HasForeignKey(a => a.AuditorId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // ── DispatchRequest ───────────────────────────────────────────────
        builder.Entity<DispatchRequest>(e =>
        {
            e.HasKey(dr => dr.Id);
            e.Property(dr => dr.ProjectName).HasMaxLength(100).IsRequired();
            e.Property(dr => dr.ProjectAddress).HasMaxLength(200).IsRequired();
            e.Property(dr => dr.SpecialRequirements).HasMaxLength(500);
            e.Property(dr => dr.ContactName).HasMaxLength(50).IsRequired();
            e.Property(dr => dr.ContactPhone).HasMaxLength(20).IsRequired();
            e.Property(dr => dr.Status).HasDefaultValue(DispatchRequestStatus.Pending);
            e.HasOne(dr => dr.Requester)
             .WithMany()
             .HasForeignKey(dr => dr.RequesterId)
             .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(dr => dr.Category)
             .WithMany()
             .HasForeignKey(dr => dr.CategoryId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // ── DispatchOrder ─────────────────────────────────────────────────
        builder.Entity<DispatchOrder>(e =>
        {
            e.HasKey(o => o.Id);
            e.Property(o => o.UnitPrice).HasColumnType("decimal(10,2)").IsRequired();
            e.Property(o => o.Deposit).HasColumnType("decimal(10,2)").IsRequired();
            e.Property(o => o.VerifyCode).HasMaxLength(36).IsRequired();
            e.HasIndex(o => o.VerifyCode).IsUnique();
            e.Property(o => o.Status).HasDefaultValue(DispatchOrderStatus.Unsigned);
            e.HasOne(o => o.Request)
             .WithMany(dr => dr.Orders)
             .HasForeignKey(o => o.RequestId)
             .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(o => o.Equipment)
             .WithMany(eq => eq.DispatchOrders)
             .HasForeignKey(o => o.EquipmentId)
             .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(o => o.Dispatcher)
             .WithMany()
             .HasForeignKey(o => o.DispatcherId)
             .OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(o => new { o.ActualStart, o.ActualEnd });
            e.HasIndex(o => o.Status);
        });

        // ── Contract ──────────────────────────────────────────────────────
        builder.Entity<Contract>(e =>
        {
            e.HasKey(c => c.Id);
            e.Property(c => c.ContractNo).HasMaxLength(50).IsRequired();
            e.HasIndex(c => c.ContractNo).IsUnique();
            e.Property(c => c.ScanPath).HasMaxLength(500);
            e.Property(c => c.Status).HasDefaultValue(ContractStatus.Draft);
            e.HasOne(c => c.Order)
             .WithOne(o => o.Contract)
             .HasForeignKey<Contract>(c => c.OrderId)
             .OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(c => c.OrderId).IsUnique();
        });

        // ── EntryVerification ─────────────────────────────────────────────
        builder.Entity<EntryVerification>(e =>
        {
            e.HasKey(ev => ev.Id);
            e.Property(ev => ev.FailReason).HasMaxLength(500);
            e.HasOne(ev => ev.Order)
             .WithOne(o => o.EntryVerification)
             .HasForeignKey<EntryVerification>(ev => ev.OrderId)
             .OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(ev => ev.OrderId).IsUnique();
            e.HasOne(ev => ev.Verifier)
             .WithMany()
             .HasForeignKey(ev => ev.VerifierId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // ── SafetyBriefing ────────────────────────────────────────────────
        builder.Entity<SafetyBriefing>(e =>
        {
            e.HasKey(sb => sb.Id);
            e.Property(sb => sb.Location).HasMaxLength(100).IsRequired();
            e.Property(sb => sb.ContentHtml).HasColumnType("nvarchar(max)").IsRequired();
            e.Property(sb => sb.Status).HasDefaultValue(SafetyBriefingStatus.Draft);
            e.HasOne(sb => sb.Order)
             .WithMany(o => o.SafetyBriefings)
             .HasForeignKey(sb => sb.OrderId)
             .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(sb => sb.Creator)
             .WithMany()
             .HasForeignKey(sb => sb.CreatorId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // ── BriefingParticipant ───────────────────────────────────────────
        builder.Entity<BriefingParticipant>(e =>
        {
            e.HasKey(bp => bp.Id);
            e.Property(bp => bp.Name).HasMaxLength(50).IsRequired();
            e.Property(bp => bp.JobType).HasMaxLength(50).IsRequired();
            e.Property(bp => bp.Phone).HasMaxLength(20);
            e.Property(bp => bp.ClientIp).HasMaxLength(50);
            e.HasOne(bp => bp.Briefing)
             .WithMany(sb => sb.Participants)
             .HasForeignKey(bp => bp.BriefingId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(bp => bp.SignedBy)
             .WithMany()
             .HasForeignKey(bp => bp.SignedById)
             .OnDelete(DeleteBehavior.SetNull)
             .IsRequired(false);
        });

        // ── InspectionRecord ──────────────────────────────────────────────
        builder.Entity<InspectionRecord>(e =>
        {
            e.HasKey(ir => ir.Id);
            e.Property(ir => ir.Remark).HasMaxLength(500);
            e.HasOne(ir => ir.Equipment)
             .WithMany(eq => eq.InspectionRecords)
             .HasForeignKey(ir => ir.EquipmentId)
             .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(ir => ir.Order)
             .WithMany(o => o.InspectionRecords)
             .HasForeignKey(ir => ir.OrderId)
             .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(ir => ir.Inspector)
             .WithMany()
             .HasForeignKey(ir => ir.InspectorId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // ── InspectionImage ───────────────────────────────────────────────
        builder.Entity<InspectionImage>(e =>
        {
            e.HasKey(ii => ii.Id);
            e.Property(ii => ii.FilePath).HasMaxLength(500).IsRequired();
            e.HasOne(ii => ii.InspectionRecord)
             .WithMany(ir => ir.Images)
             .HasForeignKey(ii => ii.InspectionId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // ── FaultReport ───────────────────────────────────────────────────
        builder.Entity<FaultReport>(e =>
        {
            e.HasKey(fr => fr.Id);
            e.Property(fr => fr.Description).HasMaxLength(500).IsRequired();
            e.Property(fr => fr.Resolution).HasMaxLength(500);
            e.Property(fr => fr.RepairCost).HasColumnType("decimal(10,2)");
            e.Property(fr => fr.Status).HasDefaultValue(FaultStatus.Pending);
            e.HasOne(fr => fr.Equipment)
             .WithMany(eq => eq.FaultReports)
             .HasForeignKey(fr => fr.EquipmentId)
             .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(fr => fr.Order)
             .WithMany(o => o.FaultReports)
             .HasForeignKey(fr => fr.OrderId)
             .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(fr => fr.Reporter)
             .WithMany()
             .HasForeignKey(fr => fr.ReporterId)
             .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(fr => fr.ClosedBy)
             .WithMany()
             .HasForeignKey(fr => fr.ClosedById)
             .OnDelete(DeleteBehavior.Restrict)
             .IsRequired(false);
        });

        // ── FaultImage ────────────────────────────────────────────────────
        builder.Entity<FaultImage>(e =>
        {
            e.HasKey(fi => fi.Id);
            e.Property(fi => fi.FilePath).HasMaxLength(500).IsRequired();
            e.HasOne(fi => fi.FaultReport)
             .WithMany(fr => fr.Images)
             .HasForeignKey(fi => fi.FaultReportId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // ── ReturnApplication ─────────────────────────────────────────────
        builder.Entity<ReturnApplication>(e =>
        {
            e.HasKey(ra => ra.Id);
            e.Property(ra => ra.ConditionDesc).HasMaxLength(500);
            e.Property(ra => ra.Status).HasDefaultValue(ReturnApplicationStatus.PendingEvaluation);
            e.HasOne(ra => ra.Order)
             .WithOne(o => o.ReturnApplication)
             .HasForeignKey<ReturnApplication>(ra => ra.OrderId)
             .OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(ra => ra.OrderId).IsUnique();
            e.HasOne(ra => ra.Applicant)
             .WithMany()
             .HasForeignKey(ra => ra.ApplicantId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // ── ReturnEvaluation ──────────────────────────────────────────────
        builder.Entity<ReturnEvaluation>(e =>
        {
            e.HasKey(re => re.Id);
            e.Property(re => re.DamageDesc).HasMaxLength(500);
            e.Property(re => re.Remark).HasMaxLength(500);
            e.Property(re => re.Deduction).HasColumnType("decimal(10,2)").HasDefaultValue(0m);
            e.Property(re => re.RefundAmount).HasColumnType("decimal(10,2)").IsRequired();
            e.HasOne(re => re.ReturnApplication)
             .WithOne(ra => ra.Evaluation)
             .HasForeignKey<ReturnEvaluation>(re => re.ReturnAppId)
             .OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(re => re.ReturnAppId).IsUnique();
            e.HasOne(re => re.Evaluator)
             .WithMany()
             .HasForeignKey(re => re.EvaluatorId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // ── Notification ──────────────────────────────────────────────────
        builder.Entity<Notification>(e =>
        {
            e.HasKey(n => n.Id);
            e.Property(n => n.Title).HasMaxLength(100).IsRequired();
            e.Property(n => n.Content).HasMaxLength(500).IsRequired();
            e.Property(n => n.RelatedUrl).HasMaxLength(200);
            e.Property(n => n.IsRead).HasDefaultValue(false);
            e.HasOne(n => n.Recipient)
             .WithMany()
             .HasForeignKey(n => n.RecipientId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(n => new { n.RecipientId, n.IsRead });
        });

        // ── OperationLog ──────────────────────────────────────────────────
        builder.Entity<OperationLog>(e =>
        {
            e.HasKey(ol => ol.Id);
            e.Property(ol => ol.Action).HasMaxLength(50).IsRequired();
            e.Property(ol => ol.EntityType).HasMaxLength(50).IsRequired();
            e.Property(ol => ol.EntityId).HasMaxLength(50).IsRequired();
            e.Property(ol => ol.Detail).HasMaxLength(1000);
            e.Property(ol => ol.ClientIp).HasMaxLength(50);
            e.HasOne(ol => ol.User)
             .WithMany()
             .HasForeignKey(ol => ol.UserId)
             .OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(ol => new { ol.UserId, ol.OccurredAt });
        });
    }
}
