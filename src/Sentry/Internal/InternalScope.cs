using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using Sentry.Extensibility;
using Sentry.Protocol;

namespace Sentry.Internal
{
    internal class InternalScope : Scope
    {
        public InternalScope(IScopeOptions options) : base(options)
        {
        }

        ///// <summary>
        ///// A list of exception processors
        ///// </summary>
        //internal ImmutableList<ISentryEventExceptionProcessor> ExceptionProcessors { get; set; }

        ///// <summary>
        ///// A list of event processors
        ///// </summary>
        //internal ImmutableList<ISentryEventProcessor> EventProcessors { get; set; }

        ///// <summary>
        ///// A list of providers of <see cref="ISentryEventProcessor"/>
        ///// </summary>
        //internal ImmutableList<Func<IEnumerable<ISentryEventProcessor>>> EventProcessorsProviders { get; set; }

        ///// <summary>
        ///// A list of providers of <see cref="ISentryEventExceptionProcessor"/>
        ///// </summary>
        //internal ImmutableList<Func<IEnumerable<ISentryEventExceptionProcessor>>> ExceptionProcessorsProviders { get; set; }
    }
}
