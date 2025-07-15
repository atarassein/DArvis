using System;

namespace DArvis.Services
{
    public interface IServiceProvider : IDisposable
    {
        bool IsRegistered<T>();

        T GetService<T>();
    }
}
