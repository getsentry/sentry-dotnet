namespace Sentry;
{
    public readonly record struct HttpStatusCodeRange
    {
        public int Start { get; init; }

        public int End { get; init; }

        public HttpStatusCodeRange(int statusCode)
        {
            Start = statusCode;
            End = statusCode;
        }

        public HttpStatusCodeRange(int start, int end)
        {
            Start = Math.Min(start, end);
            End = Math.Max(start, end);
        }

        public static implicit operator HttpStatusCodeRange((int Start, int End) range) => new(range.Start, range.End);

        public static implicit operator HttpStatusCodeRange(int statusCode)
        {
            return new HttpStatusCodeRange(statusCode);
        }

        public static implicit operator HttpStatusCodeRange(HttpStatusCode statusCode)
        {
            return new HttpStatusCodeRange((int)statusCode);
        }

        public static implicit operator HttpStatusCodeRange((HttpStatusCode start, HttpStatusCode end) range)
        {
            return new HttpStatusCodeRange((int)range.start, (int)range.end);
        }

        public bool Contains(int statusCode)
            => statusCode >= Start && statusCode <= End;

        public bool Contains(HttpStatusCode statusCode) => Contains((int)statusCode);
    }
}
