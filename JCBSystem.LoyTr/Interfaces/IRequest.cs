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
    public interface IRequest { }

    /// <summary>
    /// Interface para sa requests na may return value
    /// </summary>
    public interface IRequest<TResponse> : IRequest { }

}
