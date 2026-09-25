namespace Unity.Pipeline.Commands
{
    /// <summary>
    /// Marker for a command parameter type that should be advertised to clients as a nested JSON
    /// <em>object</em> schema in <c>GET /api/commands</c>, instead of collapsing to <c>string</c>.
    ///
    /// Convention for command authors: when a command needs a structured (multi-field) argument,
    /// declare a small DTO that implements this interface and take it as a single parameter. The
    /// <see cref="JsonSchemaGenerator"/> reflects over the type's public, writable fields and
    /// properties to emit <c>{ "type": "object", "properties": { ... } }</c> (recursing into nested
    /// <see cref="IStructuredCommandInput"/> members and arrays/lists of them). At runtime the value
    /// deserializes automatically via Newtonsoft in
    /// <c>BasePipelineServer.ExtractCommandParameters</c>, so no extra wiring is needed.
    ///
    /// Member metadata:
    /// <list type="bullet">
    /// <item><description>Annotate members with <c>[CliArg(name, description, Required = ...)]</c> to control the
    /// property name, description, and whether it appears in the schema's <c>required</c> array
    /// (mirrors how command parameters are described).</description></item>
    /// <item><description>Without a <c>[CliArg]</c>, the member name (or its Newtonsoft <c>[JsonProperty]</c> name)
    /// is used and the member is optional.</description></item>
    /// <item><description><c>[JsonIgnore]</c> members are omitted from the schema.</description></item>
    /// </list>
    /// </summary>
    public interface IStructuredCommandInput
    {
    }
}
