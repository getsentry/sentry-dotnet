class SpansConverter : WriteOnlyJsonConverter<IReadOnlyCollection<Span>>
{
    public override void Write(VerifyJsonWriter writer, IReadOnlyCollection<Span> spans)
    {
        var ordered = spans
            .OrderBy(x => x.StartTimestamp)
            .ToList();

        writer.WriteStartArray();

        foreach (var span in ordered)
        {
            writer.Serialize(span);
        }

        writer.WriteEndArray();
    }
}
