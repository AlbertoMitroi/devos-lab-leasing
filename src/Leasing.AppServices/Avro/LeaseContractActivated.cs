// Avro specific record for Avro/Schemas/LeaseContractActivated.avsc (kept by hand, the schema is the contract).
using Avro;
using Avro.Specific;

namespace Lab.Leasing.Contracts;

public sealed class LeaseContractActivated : ISpecificRecord
{
    public static readonly Schema _SCHEMA = Schema.Parse("""
        {"type":"record","name":"LeaseContractActivated","namespace":"Lab.Leasing.Contracts","fields":[
        {"name":"ContractNumber","type":"string"},{"name":"LeaseFlowId","type":"string"},{"name":"ApplicationId","type":"string"},
        {"name":"CompanyNumber","type":"string"},{"name":"MonthlyPayment","type":"double"},{"name":"TermMonths","type":"int"},
        {"name":"ActivatedAtUnixMs","type":"long"}]}
        """);

    public Schema Schema => _SCHEMA;

    public string ContractNumber { get; set; } = "";

    public string LeaseFlowId { get; set; } = "";

    public string ApplicationId { get; set; } = "";

    public string CompanyNumber { get; set; } = "";

    public double MonthlyPayment { get; set; }

    public int TermMonths { get; set; }

    public long ActivatedAtUnixMs { get; set; }

    public object Get(int fieldPos) => fieldPos switch
    {
        0 => ContractNumber,
        1 => LeaseFlowId,
        2 => ApplicationId,
        3 => CompanyNumber,
        4 => MonthlyPayment,
        5 => TermMonths,
        6 => ActivatedAtUnixMs,
        _ => throw new AvroRuntimeException($"Bad index {fieldPos} in Get()"),
    };

    public void Put(int fieldPos, object fieldValue)
    {
        switch (fieldPos)
        {
            case 0: ContractNumber = (string)fieldValue; break;
            case 1: LeaseFlowId = (string)fieldValue; break;
            case 2: ApplicationId = (string)fieldValue; break;
            case 3: CompanyNumber = (string)fieldValue; break;
            case 4: MonthlyPayment = (double)fieldValue; break;
            case 5: TermMonths = (int)fieldValue; break;
            case 6: ActivatedAtUnixMs = (long)fieldValue; break;
            default: throw new AvroRuntimeException($"Bad index {fieldPos} in Put()");
        }
    }
}
