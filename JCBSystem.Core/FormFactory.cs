using System;
using System.Windows.Forms;
using Microsoft.Extensions.DependencyInjection;

namespace JCBSystem.Core
{
    public class FormFactory
    {
        private readonly IServiceProvider _provider;
        public FormFactory(IServiceProvider provider)
        {
            _provider = provider;
        }

        public T Create<T>() where T : Form
        {
            return _provider.GetRequiredService<T>();
        }
    }
}
