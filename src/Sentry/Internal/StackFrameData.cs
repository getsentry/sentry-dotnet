using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Sentry.Internal
{
    // https://github.com/mono/mono/blob/d336d6be307dfea8b7a07268270c6d885db9d399/mcs/tools/mono-symbolicate/StackFrameData.cs
    internal class StackFrameData
    {
        static readonly Regex _regex = new(
            @"\w*at (?<Method>.+) *(\[0x(?<IL>.+)\]|<0x.+ \+ 0x(?<NativeOffset>.+)>( (?<MethodIndex>\d+)|)) in <(?<MVID>[^>#]+)(#(?<AOTID>[^>]+)|)>:0");

        public string TypeFullName { get; }
        public string MethodSignature { get; }
        public int Offset { get; }
        public bool IsILOffset { get; }
        public uint MethodIndex { get; }
        public string Line { get; }
        public string Mvid { get; }
        public string Aotid { get; }

        private StackFrameData(
            string line,
            string typeFullName,
            string methodSig,
            int offset,
            bool isILOffset,
            uint methodIndex,
            string mvid,
            string aotid)
        {
            Line = line;
            TypeFullName = typeFullName;
            MethodSignature = methodSig;
            Offset = offset;
            IsILOffset = isILOffset;
            MethodIndex = methodIndex;
            Mvid = mvid;
            Aotid = aotid;
        }

        public StackFrameData Relocate(string typeName, string methodName)
            => new(Line, typeName, methodName, Offset, IsILOffset, MethodIndex, Mvid, Aotid);

        public static bool TryParse(string line, [NotNullWhen(true)] out StackFrameData? stackFrame)
        {
            stackFrame = default;

            var match = _regex.Match(line);
            if (!match.Success)
            {
                return false;
            }

            var methodStr = match.Groups["Method"].Value.Trim();
            if (!ExtractSignatures(methodStr, out var typeFullName, out var methodSignature))
            {
                return false;
            }

            var isILOffset = !string.IsNullOrEmpty(match.Groups["IL"].Value);
            var offsetVarName = isILOffset ? "IL" : "NativeOffset";
            var offset = int.Parse(
                match.Groups[offsetVarName].Value,
                NumberStyles.HexNumber,
                CultureInfo.InvariantCulture);

            uint methodIndex = 0xffffff;
            if (!string.IsNullOrEmpty(match.Groups["MethodIndex"].Value))
            {
                methodIndex = uint.Parse(match.Groups["MethodIndex"].Value, CultureInfo.InvariantCulture);
            }

            stackFrame = new StackFrameData(
                line,
                typeFullName,
                methodSignature,
                offset,
                isILOffset,
                methodIndex,
                match.Groups["MVID"].Value,
                match.Groups["AOTID"].Value);

            return true;
        }

        private static bool ExtractSignatures(
            string str,
            [NotNullWhen(true)] out string? typeFullName,
            [NotNullWhen(true)] out string? methodSignature)
        {
            typeFullName = null;
            methodSignature = null;

            var methodNameEnd = str.IndexOf('(');
            if (methodNameEnd == -1)
            {
                return false;
            }

            var typeNameEnd = str.LastIndexOf('.', methodNameEnd);
            if (typeNameEnd == -1)
            {
                return false;
            }

            // Adjustment for Type..ctor ()
            if (typeNameEnd > 0 && str[typeNameEnd - 1] == '.')
            {
                --typeNameEnd;
            }

            typeFullName = str.Substring(0, typeNameEnd);
            // Remove generic parameters
            typeFullName = Regex.Replace(typeFullName, @"\[[^\[\]]*\]$", "");
            typeFullName = Regex.Replace(typeFullName, @"\<[^\[\]]*\>$", "");

            methodSignature = str.Substring(typeNameEnd + 1);

            return true;
        }
    }
}
