using JCBSystem.LoyTr.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JCBSystem.LoyTr.Handlers
{
    /// <summary>
    /// Interface para sa handlers na walang return value
    /// </summary>
    public interface ILoyTrHandler<in TRequest> where TRequest : ILoyTrRequest
    {
        Task HandleAsync(TRequest request);
    }

    /// <summary>
    /// Interface para sa handlers na may return value
    /// </summary>
    public interface ILoyTrHandler<in TRequest, TResponse> where TRequest : ILoyTrRequest<TResponse>
    {
        Task<TResponse> HandleAsync(TRequest request);
    }
}
