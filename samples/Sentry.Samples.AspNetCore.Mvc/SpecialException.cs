using System;

namespace Samples.AspNetCore.Mvc
{
    public class SpecialException : Exception
    {
        public bool IsSpecial { get; set; } = true;

        public SpecialException(string message)
            : base(message)
        { }

        public SpecialException()
        {
        }

        public SpecialException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
