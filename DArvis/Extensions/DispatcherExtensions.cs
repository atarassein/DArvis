using System.Windows.Threading;
using DArvis.Threading;

namespace DArvis.Extensions
{
    public static class DispatcherExtensions
    {
        public static UIThreadAwaitable SwitchToUIThread(this Dispatcher dispatcher)
        {
            return new UIThreadAwaitable(dispatcher);
        }
    }
}
