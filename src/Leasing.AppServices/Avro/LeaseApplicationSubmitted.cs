// Avro specific record for Avro/Schemas/LeaseApplicationSubmitted.avsc, copied from the lab-intake schema (the producer owns it).
using Avro;
using Avro.Specific;

namespace Lab.Intake.Contracts;

public sealed class LeaseApplicationSubmitted : ISpecificRecord
{
    public static readonly Schema _SCHEMA = Schema.Parse("""
        {"type":"record","name":"LeaseApplicationSubmitted","namespace":"Lab.Intake.Contracts","fields":[
        {"name":"ApplicationId","type":"string"},{"name":"ApplicantName","type":"string"},{"name":"CompanyNumber","type":"string"},
        {"name":"AssetType","type":"string"},{"name":"AssetValue","type":"double"},{"name":"TermMonths","type":"int"},
        {"name":"SubmittedAtUnixMs","type":"long"}]}
        """);

    public Schema Schema => _SCHEMA;

    public string ApplicationId { get; set; } = "";

    public string ApplicantName { get; set; } = "";

    public string CompanyNumber { get; set; } = "";

    public string AssetType { get; set; } = "";

    public double AssetValue { get; set; }

    public int TermMonths { get; set; }

    public long SubmittedAtUnixMs { get; set; }

    public object Get(int fieldPos) => fieldPos switch
    {
        0 => ApplicationId,
        1 => ApplicantName,
        2 => CompanyNumber,
        3 => AssetType,
        4 => AssetValue,
        5 => TermMonths,
        6 => SubmittedAtUnixMs,
        _ => throw new AvroRuntimeException($"Bad index {fieldPos} in Get()"),
    };

    public void Put(int fieldPos, object fieldValue)
    {
        switch (fieldPos)
        {
            case 0: ApplicationId = (string)fieldValue; break;
            case 1: ApplicantName = (string)fieldValue; break;
            case 2: CompanyNumber = (string)fieldValue; break;
            case 3: AssetType = (string)fieldValue; break;
            case 4: AssetValue = (double)fieldValue; break;
            case 5: TermMonths = (int)fieldValue; break;
            case 6: SubmittedAtUnixMs = (long)fieldValue; break;
            default: throw new AvroRuntimeException($"Bad index {fieldPos} in Put()");
        }
    }
}
