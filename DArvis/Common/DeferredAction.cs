using System;

namespace DArvis.Common
{
    public readonly record struct DeferredAction(Action Action, DateTime ExecutionTime) { }
}
