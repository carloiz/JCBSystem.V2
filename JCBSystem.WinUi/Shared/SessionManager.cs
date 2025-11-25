using JCBSystem.Core.common.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JCBSystem.WinUi.Shared
{
    public class SessionManager : ISessionManager
    {
        public bool IsLoggedIn { get; private set; }
        public string UserNumber { get; private set; }

        public event Action SessionChanged;

        public void OnUserLog(bool isLogin = false, string userNumber = null)
        {
            IsLoggedIn = isLogin;
            UserNumber = userNumber;

            // Notify all listeners (MainForm)
            SessionChanged?.Invoke();
        }
    }
}
