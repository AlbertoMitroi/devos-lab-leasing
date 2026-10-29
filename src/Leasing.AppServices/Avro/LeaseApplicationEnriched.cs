// Avro specific record for Avro/Schemas/LeaseApplicationEnriched.avsc, copied from the lab-intake schema (the producer owns it).
using Avro;
using Avro.Specific;

namespace Lab.Intake.Contracts;

public sealed class LeaseApplicationEnriched : ISpecificRecord
{
    public static readonly Schema _SCHEMA = Schema.Parse("""
        {"type":"record","name":"LeaseApplicationEnriched","namespace":"Lab.Intake.Contracts","fields":[
        {"name":"ApplicationId","type":"string"},{"name":"Sector","type":"string"},{"name":"YearsTrading","type":"int"}]}
        """);

    public Schema Schema => _SCHEMA;

    public string ApplicationId { get; set; } = "";

    public string Sector { get; set; } = "";

    public int YearsTrading { get; set; }

    public object Get(int fieldPos) => fieldPos switch
    {
        0 => ApplicationId,
        1 => Sector,
        2 => YearsTrading,
        _ => throw new AvroRuntimeException($"Bad index {fieldPos} in Get()"),
    };

    public void Put(int fieldPos, object fieldValue)
    {
        switch (fieldPos)
        {
            case 0: ApplicationId = (string)fieldValue; break;
            case 1: Sector = (string)fieldValue; break;
            case 2: YearsTrading = (int)fieldValue; break;
            default: throw new AvroRuntimeException($"Bad index {fieldPos} in Put()");
        }
    }
}
