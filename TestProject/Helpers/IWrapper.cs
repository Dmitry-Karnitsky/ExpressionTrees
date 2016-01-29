using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestProject.Helpers
{
    public interface IWrapper<TWrapped> : IDisposable
    {
        TReturn Call<TReturn>(Func<TWrapped, TReturn> expression);
        void Call(Action<TWrapped> expression);
        TWrapped Instance { get; }
    }
}
