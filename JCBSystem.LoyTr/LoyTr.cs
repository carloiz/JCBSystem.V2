using JCBSystem.LoyTr.Handlers;
using JCBSystem.LoyTr.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JCBSystem.LoyTr
{
    public class LoyTr : ILoyTr
    {
        private readonly IServiceProvider _serviceProvider;

        public LoyTr(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public async Task SendAsync<TRequest>(TRequest request) where TRequest : ILoyTrRequest
        {
            var handlerType = typeof(ILoyTrHandler<>).MakeGenericType(request.GetType());
            var handler = _serviceProvider.GetService(handlerType);

            if (handler == null)
                throw new InvalidOperationException($"No handler registered for {request.GetType().Name}");

            var method = handlerType.GetMethod("HandleAsync");
            await (Task)method.Invoke(handler, new object[] { request });
        }

        public async Task<TResponse> SendAsync<TResponse>(ILoyTrRequest<TResponse> request)
        {
            var requestType = request.GetType();
            var handlerType = typeof(ILoyTrHandler<,>).MakeGenericType(requestType, typeof(TResponse));
            var handler = _serviceProvider.GetService(handlerType);

            if (handler == null)
                throw new InvalidOperationException($"No handler registered for {requestType.Name}");

            var method = handlerType.GetMethod("HandleAsync");
            return await (Task<TResponse>)method.Invoke(handler, new object[] { request });
        }
    }
}
