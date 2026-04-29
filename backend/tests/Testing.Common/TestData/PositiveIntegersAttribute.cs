namespace Shoplists.Testing.Common.TestData;

public sealed class PositiveIntegersAttribute(bool includeZero = false)
    : DataSourceGeneratorAttribute<int>
{
    protected override IEnumerable<Func<int>> GenerateDataSources(
        DataGeneratorMetadata dataGeneratorMetadata
    )
    {
        if (includeZero)
            yield return () => 0;
        yield return () => 1;
        yield return () => int.MaxValue;
    }
}
