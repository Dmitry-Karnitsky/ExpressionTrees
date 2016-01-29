using System;

namespace TestProject.Helpers
{
    public interface IWrapper : IDisposable
    {
        TReturn Call<TReturn>(Func<object, TReturn> expression);
        void Call(Action<object> expression);
        object Instance { get; }
    }
}
