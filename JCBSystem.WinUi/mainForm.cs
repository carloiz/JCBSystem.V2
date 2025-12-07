using System;
using System.Windows.Forms;
using JCBSystem.Core;
using JCBSystem.Core.common.FormCustomization;
using JCBSystem.Core.common.Interfaces;
using JCBSystem.Login;
using JCBSystem.LoyTr.Interfaces;
using JCBSystem.Services.Authentication.Login.Commands;
using JCBSystem.Services.MainDashboard.Queries;
using JCBSystem.Users;
using JCBSystem.WinUi.Shared;
using Microsoft.Extensions.DependencyInjection;

namespace JCBSystem
{
    public partial class MainForm : Form
    {
        //TextInfo textInfo = new CultureInfo("en-US", false).TextInfo;
        private readonly ISessionManager sessionManager;
        private readonly TabController tabController;
        private readonly IServiceProvider serviceProvider;
        private readonly ILoyTr loyTr;

        public MainForm(IServiceProvider serviceProvider, 
                        ILoyTr loyTr,
                        ISessionManager sessionManager,
                        TabController tabController)
        {
            this.serviceProvider = serviceProvider;
            this.loyTr = loyTr;
            this.sessionManager = sessionManager;
            this.tabController = tabController;
            sessionManager.SessionChanged += ApplySessionState;
            InitializeComponent();
            this.tabController.Initialize(mainPanel);
            _ = this.loyTr.SendAsync(new GetSessionQuery());
        }
    
        public void ApplySessionState()
        {
            OnUserLog(sessionManager.IsLoggedIn, sessionManager.UserNumber);
        }

        public void OnUserLog(bool isLogin = false, string userNumber = null)
        {
            panel1.Visible = isLogin;
            UsersBtn.Visible = isLogin;
            SettingsBtn.Visible = isLogin;
            mainPanel.Visible = isLogin;

            if (isLogin)
            {
                usernameLbl.Text = userNumber;
            }
            else
            {
                var loginForm = serviceProvider.GetRequiredService<LoginForm>();
                loginForm.MdiParent = this; // Set parent
                FormHelper.OpenFormWithFade(loginForm, false);

                // Gamitin ang `Shown` event para i-focus ang textbox kapag visible na ang form
                loginForm.Shown += (s, e) => loginForm.txtUsername.Focus();
            }
        }


        private void CloseApp_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void UsersBtn_Click(object sender, EventArgs e)
        {
            var form = serviceProvider.GetRequiredService<UsersListForm>();
            tabController.OpenFormInTab(form, "Users");
        }

        private void SettingsBtn_Click(object sender, EventArgs e)
        {
            var form = serviceProvider.GetRequiredService<LoginForm>();
            tabController.OpenFormInTab(form, "Settings");
        }

        private async void logoutBtn_Click(object sender, EventArgs e)
        {
            await loyTr.SendAsync(new ServiceLogoutCommand());  
        }
    }
}
