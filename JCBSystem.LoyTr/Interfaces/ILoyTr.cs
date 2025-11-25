using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JCBSystem.LoyTr.Interfaces
{
    public interface ILoyTr
    {
        Task SendAsync<TRequest>(TRequest request) where TRequest : ILoyTrRequest;
        Task<TResponse> SendAsync<TResponse>(ILoyTrRequest<TResponse> request);
    }

}
