using JCBSystem.Core.common.FormCustomization;
using JCBSystem.Services.Authentication.Login.Commands;
using System;
using System.Windows.Forms;

namespace JCBSystem
{
    public partial class LoginForm : Form
    {
        private readonly ServiceLoginCommand serviceLoginCommand;
        private MainForm mainForm;

        public LoginForm(ServiceLoginCommand serviceLoginCommand)
        {
            InitializeComponent();
            this.serviceLoginCommand = serviceLoginCommand;
        }

        public void Initialize(MainForm mainForm)
        {
            this.mainForm = mainForm;
        }

        private async void loginBtn_Click(object sender, EventArgs e)
        {
            serviceLoginCommand.Initialize(txtUsername.Text);

            await serviceLoginCommand.HandleAsync();

            mainForm.OnUserLog(true, txtUsername.Text);

            FormHelper.CloseFormWithFade(this);
        }
    }
}
