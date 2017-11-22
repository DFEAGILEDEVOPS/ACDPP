using System;

namespace Dashboard.NetStandard.Core.Interfaces
{
    public interface IClassQueue 
    {
        T Peek<T>(out string messageIdentifier);

        T Dequeue<T>(out string messageIdentifier);

        void Enqueue<T>(T instance);

        void Delete(string messageIdentifier);

        void ProcessNext<T>(Action<T> action);

    }
}