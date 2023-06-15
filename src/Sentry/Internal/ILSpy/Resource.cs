using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sentry.Internal.ILSpy;

internal enum ResourceType
{
    Linked,
    Embedded,
    AssemblyLinked,
}

internal abstract class Resource
{
    public virtual ResourceType ResourceType => ResourceType.Embedded;
    public virtual ManifestResourceAttributes Attributes => ManifestResourceAttributes.Public;
    public abstract string Name { get; }
    public abstract Stream? TryOpenStream();
    public abstract long? TryGetLength();
}
