namespace Sentry
{
    public record HttpStatusCodeRange
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

        public static implicit operator HttpStatusCodeRange((int start, int end) range)
        {
            return new HttpStatusCodeRange(range.start, range.end);
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
