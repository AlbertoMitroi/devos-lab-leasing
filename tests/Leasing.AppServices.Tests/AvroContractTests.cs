using FluentAssertions;
using Lab.Intake.Contracts;
using Lab.Leasing.Contracts;

namespace Leasing.AppServices.Tests;

public class AvroContractTests
{
    [Theory]
    [InlineData("LeaseApplicationSubmitted.avsc")]
    [InlineData("LeaseApplicationEnriched.avsc")]
    [InlineData("LeaseContractActivated.avsc")]
    public void Record_classes_match_their_schema_files(string file)
    {
        var schema = Avro.Schema.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Avro", "Schemas", file)));

        Avro.Schema expected = file switch
        {
            "LeaseApplicationSubmitted.avsc" => LeaseApplicationSubmitted._SCHEMA,
            "LeaseApplicationEnriched.avsc" => LeaseApplicationEnriched._SCHEMA,
            _ => LeaseContractActivated._SCHEMA,
        };
        schema.Should().Be(expected);
    }
}
