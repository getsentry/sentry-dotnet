using Sentry.Infrastructure;

namespace Sentry;

/// <summary>
/// Controls which categories of data the SDK's integrations collect automatically.
/// </summary>
/// <remarks>
/// Assigning this option at all opts into these defaults, which collect every category including user identity
/// and HTTP bodies, and causes the deprecated <see cref="SentryOptions.SendDefaultPii"/> to be ignored. Set an
/// individual category to restrict it.
/// <para>
/// This only gates data the SDK gathers by itself. Data you provide explicitly - a <see cref="SentryUser"/> set on
/// the scope, tags, or anything added in <see cref="SentryOptions.SetBeforeSend(System.Func{SentryEvent, SentryHint, SentryEvent})"/>
/// - is always sent. Where an integration exposes its own data-collection option, that option takes precedence
/// over the matching category here.
/// </para>
/// <para>This API is experimental, and it may change in the future.</para>
/// </remarks>
/// <seealso href="https://develop.sentry.dev/sdk/foundations/client/data-collection/"/>
[Experimental(DiagnosticId.ExperimentalFeature)]
public class DataCollectionOptions
{

    /// <summary>
    /// Whether integrations may populate user identity information automatically, including
    /// <see cref="SentryUser.Id"/>, <see cref="SentryUser.Email"/>, <see cref="SentryUser.Username"/> and the
    /// user's IP address. Defaults to <c>true</c>. A user you set yourself is never affected.
    /// </summary>
    public bool? UserInfo { get; set; }

    /// <summary>
    /// How cookies are collected. Defaults to <see cref="KeyValueCollectionBehavior.DenyList"/>.
    /// </summary>
    public KeyValueCollectionBehavior? Cookies { get; set; }

    /// <summary>
    /// How request and response headers are collected.
    /// </summary>
    public HttpHeaderOptions HttpHeaders { get; } = new();

    /// <summary>
    /// Which HTTP bodies are attached. Defaults to <see cref="HttpBodyTypes.All"/>. Where an integration limits
    /// the size of a body it collects - such as <c>SentryAspNetCoreOptions.MaxRequestBodySize</c> - that limit
    /// still applies.
    /// </summary>
    public HttpBodyTypes? HttpBodies { get; set; }

    /// <summary>
    /// How URL query parameters are collected, wherever a query string appears.
    /// Defaults to <see cref="KeyValueCollectionBehavior.DenyList"/>.
    /// </summary>
    public KeyValueCollectionBehavior? UrlQueryParams { get; set; }

    /// <summary>
    /// How GraphQL documents and variables are collected.
    /// </summary>
    public GraphQLOptions GraphQL { get; } = new();

    /// <summary>
    /// How generative AI prompt and completion content is collected.
    /// </summary>
    public GenAIOptions GenAI { get; } = new();

    /// <summary>
    /// How Model Context Protocol request and response content is collected.
    /// </summary>
    public McpOptions Mcp { get; } = new();

    /// <summary>
    /// Whether data associated with database queries is collected - bound query parameters, payloads for write
    /// operations, and returned result data. Defaults to <c>true</c>.
    /// </summary>
    /// <remarks>
    /// Sanitized query statements and structural metadata - the database system, query summary, operation name and
    /// the table acted upon - are always collected.
    /// </remarks>
    public bool? DatabaseQueryData { get; set; }

    /// <summary>
    /// Whether arguments passed to queued tasks are collected. Defaults to <c>true</c>.
    /// </summary>
    public bool? Queues { get; set; }

    /// <summary>
    /// Whether file and directory paths collected by file-system instrumentation are attached.
    /// Defaults to <c>true</c>.
    /// </summary>
    /// <remarks>
    /// This does not control source code locations in stack frames, profile frames or debug images, and it does not
    /// gate attachment names or paths you supply yourself.
    /// </remarks>
    public bool? FilePaths { get; set; }

    /// <summary>
    /// How local variable values captured within stack frames are collected. Defaults to collecting all variables.
    /// Assign <c>false</c> to collect none, or a behavior to filter variables by name.
    /// </summary>
    public KeyValueCollectionBehavior? StackFrameVariables { get; set; }

    /// <summary>
    /// The number of source code lines to include above and below each stack frame. Defaults to <c>5</c>.
    /// Set to <c>0</c> to attach no surrounding lines.
    /// </summary>
    public int? FrameContextLines { get; set; }

    /// <summary>
    /// How request and response headers are collected.
    /// </summary>
    public sealed class HttpHeaderOptions
    {
        /// <summary>
        /// How headers of incoming requests are collected.
        /// Defaults to <see cref="KeyValueCollectionBehavior.DenyList"/>.
        /// </summary>
        public KeyValueCollectionBehavior? Request { get; set; }

        /// <summary>
        /// How headers of responses are collected.
        /// Defaults to <see cref="KeyValueCollectionBehavior.DenyList"/>.
        /// </summary>
        public KeyValueCollectionBehavior? Response { get; set; }

    }

    /// <summary>
    /// How GraphQL documents and variables are collected.
    /// </summary>
    public sealed class GraphQLOptions
    {
        /// <summary>
        /// Whether the GraphQL document is collected. Defaults to <c>true</c>.
        /// </summary>
        public bool? Document { get; set; }

        /// <summary>
        /// Whether the variables passed to GraphQL operations are collected. Defaults to <c>true</c>.
        /// </summary>
        public bool? Variables { get; set; }

    }

    /// <summary>
    /// How generative AI prompt and completion content is collected. Metadata such as the model name and token
    /// counts is always collected.
    /// </summary>
    public sealed class GenAIOptions
    {
        /// <summary>
        /// Whether the content of generative AI inputs is collected - system instructions, prompt messages, tool
        /// definitions and tool call arguments. Defaults to <c>true</c>.
        /// </summary>
        public bool? Inputs { get; set; }

        /// <summary>
        /// Whether the content of generative AI outputs is collected - completion text and tool call results.
        /// Defaults to <c>true</c>.
        /// </summary>
        public bool? Outputs { get; set; }

    }

    /// <summary>
    /// How Model Context Protocol request and response content is collected. Metadata such as the method, tool and
    /// prompt names is always collected.
    /// </summary>
    /// <remarks>
    /// Content collected by MCP instrumentation is gated by these options even where it is stored on a
    /// <c>gen_ai.*</c> attribute, so <see cref="GenAIOptions"/> does not apply to it.
    /// </remarks>
    public sealed class McpOptions
    {
        /// <summary>
        /// Whether the content of incoming MCP requests is collected - tool call arguments, prompt arguments and
        /// the message payload of logging notifications. Defaults to <c>true</c>.
        /// </summary>
        public bool? Inputs { get; set; }

        /// <summary>
        /// Whether the content of MCP responses is collected - tool result content and prompt result messages.
        /// Defaults to <c>true</c>.
        /// </summary>
        public bool? Outputs { get; set; }

    }
}
