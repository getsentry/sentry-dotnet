namespace Sentry;

internal partial class BindableSentryOptions
{
    public DataCollectionOptions? DataCollection { get; set; }

    public class DataCollectionOptions
    {
        public bool? UserInfo { get; set; }
        public KeyValueBehavior Cookies { get; } = new();
        public HttpHeaderOptions HttpHeaders { get; } = new();
        public List<string>? HttpBodies { get; set; }
        public KeyValueBehavior UrlQueryParams { get; } = new();
        public GraphQLOptions GraphQL { get; } = new();
        public GenAIOptions GenAI { get; } = new();
        public McpOptions Mcp { get; } = new();
        public bool? DatabaseQueryData { get; set; }
        public bool? Queues { get; set; }
        public bool? FilePaths { get; set; }
        public KeyValueBehavior StackFrameVariables { get; } = new();
        public int? FrameContextLines { get; set; }

        public void ApplyTo(Sentry.DataCollectionOptions options)
        {
            options.UserInfo = UserInfo ?? options.UserInfo;
            options.Cookies = Cookies.ToBehavior() ?? options.Cookies;
            options.HttpBodies = ParseHttpBodies() ?? options.HttpBodies;
            options.UrlQueryParams = UrlQueryParams.ToBehavior() ?? options.UrlQueryParams;
            options.DatabaseQueryData = DatabaseQueryData ?? options.DatabaseQueryData;
            options.Queues = Queues ?? options.Queues;
            options.FilePaths = FilePaths ?? options.FilePaths;
            options.StackFrameVariables = StackFrameVariables.ToBehavior() ?? options.StackFrameVariables;
            options.FrameContextLines = FrameContextLines ?? options.FrameContextLines;
            HttpHeaders.ApplyTo(options.HttpHeaders);
            GraphQL.ApplyTo(options.GraphQL);
            GenAI.ApplyTo(options.GenAI);
            Mcp.ApplyTo(options.Mcp);
        }

        private HttpBodyTypes? ParseHttpBodies()
        {
            if (HttpBodies is null)
            {
                return null;
            }

            var result = HttpBodyTypes.None;
            foreach (var entry in HttpBodies)
            {
                foreach (var part in entry.Split(','))
                {
                    if (Enum.TryParse<HttpBodyTypes>(part.Trim(), ignoreCase: true, out var parsed))
                    {
                        result |= parsed;
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Configuration surrogate for <see cref="KeyValueCollectionBehavior"/>, which has no public constructor.
        /// A <c>Terms</c> list without a <c>Mode</c> is treated as <see cref="KeyValueCollectionMode.DenyList"/>,
        /// since that is the default behavior for every key-value category.
        /// </summary>
        public class KeyValueBehavior
        {
            public KeyValueCollectionMode? Mode { get; set; }
            public List<string>? Terms { get; set; }

            internal KeyValueCollectionBehavior? ToBehavior()
            {
                if (Mode is null && Terms is null)
                {
                    return null;
                }

                var terms = Terms?.ToArray() ?? [];
                return (Mode ?? KeyValueCollectionMode.DenyList) switch
                {
                    KeyValueCollectionMode.Off => KeyValueCollectionBehavior.Off,
                    KeyValueCollectionMode.AllowList => KeyValueCollectionBehavior.AllowList(terms),
                    _ => KeyValueCollectionBehavior.DenyList(terms)
                };
            }
        }

        public class HttpHeaderOptions
        {
            public KeyValueBehavior Request { get; } = new();
            public KeyValueBehavior Response { get; } = new();

            public void ApplyTo(Sentry.DataCollectionOptions.HttpHeaderOptions options)
            {
                options.Request = Request.ToBehavior() ?? options.Request;
                options.Response = Response.ToBehavior() ?? options.Response;
            }
        }

        public class GraphQLOptions
        {
            public bool? Document { get; set; }
            public bool? Variables { get; set; }

            public void ApplyTo(Sentry.DataCollectionOptions.GraphQLOptions options)
            {
                options.Document = Document ?? options.Document;
                options.Variables = Variables ?? options.Variables;
            }
        }

        public class GenAIOptions
        {
            public bool? Inputs { get; set; }
            public bool? Outputs { get; set; }

            public void ApplyTo(Sentry.DataCollectionOptions.GenAIOptions options)
            {
                options.Inputs = Inputs ?? options.Inputs;
                options.Outputs = Outputs ?? options.Outputs;
            }
        }

        public class McpOptions
        {
            public bool? Inputs { get; set; }
            public bool? Outputs { get; set; }

            public void ApplyTo(Sentry.DataCollectionOptions.McpOptions options)
            {
                options.Inputs = Inputs ?? options.Inputs;
                options.Outputs = Outputs ?? options.Outputs;
            }
        }
    }
}
