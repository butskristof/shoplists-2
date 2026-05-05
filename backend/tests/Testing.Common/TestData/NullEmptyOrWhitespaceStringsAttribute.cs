namespace Shoplists.Testing.Common.TestData;

public sealed class NullEmptyOrWhitespaceStringsAttribute : DataSourceGeneratorAttribute<string?>
{
    protected override IEnumerable<Func<string?>> GenerateDataSources(
        DataGeneratorMetadata dataGeneratorMetadata
    )
    {
        // char.IsWhiteSpace recognises three whitespace categories: space-like
        // (regular space, NBSP, ...), line/paragraph separators (U+2028, U+2029),
        // and ASCII control (tab, newline, ...). The cases below cover null +
        // empty + one realistic representative from each common category;
        // line/paragraph separators are skipped as exotic.
        yield return () => null;
        yield return () => "";
        yield return () => "   "; // ASCII space (space-like)
        yield return () => "\t"; // tab (ASCII control)
        yield return () => "\u00A0"; // NBSP - realistic from copy-pasted input (space-like)
    }
}
