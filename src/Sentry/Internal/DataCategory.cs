namespace Sentry.Internal
{
    internal record DataCategory : Enumeration
    {
        public static DataCategory Attachment = new("attachment");
        public static DataCategory Default = new("default");
        public static DataCategory Error = new("error");
        public static DataCategory Internal = new("internal");
        public static DataCategory Security = new("security");
        public static DataCategory Session = new("session");
        public static DataCategory Transaction = new("transaction");

        private DataCategory(string value) : base(value)
        {
        }
    }
}
