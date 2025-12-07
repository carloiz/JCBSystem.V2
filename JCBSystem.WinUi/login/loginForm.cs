using JCBSystem.Core.common.FormCustomization;
using JCBSystem.LoyTr;
using JCBSystem.LoyTr.Interfaces;
using JCBSystem.Services.Authentication.Login.Commands;
using System;
using System.Windows.Forms;

namespace JCBSystem.Login
{
    public partial class LoginForm : Form
    {
        private readonly ILoyTr loyTr;

        public LoginForm(ILoyTr loyTr)
        {
            InitializeComponent();
            this.loyTr = loyTr;
        }

        private async void loginBtn_Click(object sender, EventArgs e)
        {
            await loyTr.SendAsync(new ServiceLoginCommand
            {
                Form = this,
                Username = txtUsername.Text,   
                Password = txtPassword.Text,    
            });
        }
    }
}
