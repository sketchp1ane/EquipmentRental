namespace EquipmentRental.Models;

public enum EquipmentStatus
{
    PendingReview = 0,
    Idle = 1,
    InUse = 2,
    Maintenance = 3,
    Scrapped = 4
}

public enum QualificationType
{
    ProductCertificate = 1,
    FactoryInspectionReport = 2,
    SpecialEquipmentCert = 3,
    AnnualInspectionReport = 4,
    InsuranceCertificate = 5,
    InstallationQualification = 6
}

public enum AuditAction { Pass = 1, Reject = 2 }

public enum DispatchRequestStatus { Pending = 0, Scheduled = 1, Cancelled = 2 }

public enum DispatchOrderStatus
{
    Unsigned = 0,
    Signed = 1,
    InProgress = 2,
    Complete = 3,
    Terminated = 4
}

public enum ContractStatus { Draft = 0, AwaitingSignature = 1, Signed = 2, Terminated = 3 }

public enum SafetyBriefingStatus { Draft = 0, Completed = 1 }

public enum OverallInspectionStatus { Normal = 0, Abnormal = 1 }

public enum FaultSeverity { Minor = 1, Medium = 2, Severe = 3 }

public enum FaultStatus { Pending = 0, InProgress = 1, Closed = 2 }

public enum ReturnApplicationStatus { PendingEvaluation = 0, Complete = 1 }
