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
        private MainForm mainForm;

        public LoginForm(ILoyTr loyTr)
        {
            InitializeComponent();
            this.loyTr = loyTr;
        }

        public void Initialize(MainForm mainForm)
        {
            this.mainForm = mainForm;
        }

        private async void loginBtn_Click(object sender, EventArgs e)
        {

            await loyTr.SendAsync(new ServiceLoginCommand
            {
                Username = txtUsername.Text,    
            });

            mainForm.OnUserLog(true, txtUsername.Text);

            FormHelper.CloseFormWithFade(this);
        }
    }
}
