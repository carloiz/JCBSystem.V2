using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JCBSystem.LoyTr.Interfaces
{
    /// <summary>
    /// Marker interface para sa lahat ng requests (Commands at Queries)
    /// </summary>
    public interface ILoyTrRequest { }

    /// <summary>
    /// Interface para sa requests na may return value
    /// </summary>
    public interface ILoyTrRequest<TResponse> : ILoyTrRequest { }

}
