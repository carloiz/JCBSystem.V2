using System;

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
