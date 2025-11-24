using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JCBSystem.Core.common.Interfaces
{
    public interface ISessionManager
    {
        bool IsLoggedIn { get; }
        string UserNumber { get; }

        event Action SessionChanged;

        void OnUserLog(bool isLogin = false, string userNumber = null);
    }

}
