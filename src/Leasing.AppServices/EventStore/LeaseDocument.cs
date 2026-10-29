namespace Leasing.AppServices.EventStore;

/// <summary>Marten document of an active lease.</summary>
public sealed class LeaseDocument
{
    public Guid Id { get; set; }

    public string ContractNumber { get; set; } = "";

    public string ApplicationId { get; set; } = "";

    public string ApplicantName { get; set; } = "";

    public string CompanyNumber { get; set; } = "";

    public string AssetType { get; set; } = "";

    public decimal MonthlyPayment { get; set; }

    public int TermMonths { get; set; }

    public string SignedBy { get; set; } = "";

    public DateTimeOffset ActivatedAt { get; set; }
}
