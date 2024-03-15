using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Xunit.Sdk;

namespace Microsoft.Maui.DeviceTests
{
    /// <summary>	
    /// Usage Example
    /// [Theory]
    /// [Repeat(100)]
    /// public async Task TheSameImageSourceReturnsTheSameBitmap(int _)
    /// </summary>
    public sealed class RepeatAttribute : DataAttribute

/* Unmerged change from project 'TestUtils.DeviceTests(net8.0-ios)'
Before:
		readonly int _count;
After:
        private readonly int _count;
*/

/* Unmerged change from project 'TestUtils.DeviceTests(net8.0-maccatalyst)'
Before:
		readonly int _count;
After:
        private readonly int _count;
*/
    {
        private readonly int _count;

        public RepeatAttribute(int count)

/* Unmerged change from project 'TestUtils.DeviceTests(net8.0-ios)'
Before:
			this._count = count;
After:
			_count = count;
*/

/* Unmerged change from project 'TestUtils.DeviceTests(net8.0-maccatalyst)'
Before:
			this._count = count;
After:
			_count = count;
*/
        {
            _count = count;
        }

        public override IEnumerable<object[]> GetData(MethodInfo testMethod)
        {
            foreach (var iterationNumber in Enumerable.Range(start: 1, count: _count))
            {
                yield return new object[] { iterationNumber };
            }
        }
    }
}
