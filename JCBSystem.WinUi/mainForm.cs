using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using System.Threading.Tasks;
using System.Collections.Generic;
using JCBSystem.Core;
using JCBSystem.Core.common.CRUD;
using JCBSystem.Core.common.FormCustomization;
using JCBSystem.Core.common.Helpers;
using JCBSystem.Core.common.Interfaces;
using JCBSystem.Core.common.Logics;
using JCBSystem.Domain.DTO.Auth;
using JCBSystem.Domain.DTO.Users;
using JCBSystem.Users;


namespace JCBSystem
{
    public partial class MainForm : Form
    {

        //TextInfo textInfo = new CultureInfo("en-US", false).TextInfo;

        private readonly RegistryKeys registryKeys;
        private readonly CheckIfRecordExists checkIfRecordExists;
        private readonly GetFieldsValues getFieldsValues;   
        private readonly IDataManager dataManager;
        private readonly Pagination pagination;

        private readonly FormFactory formFactory;

        private Dictionary<string, Form> openForms = new Dictionary<string, Form>();
        private TabControl tabControlMain = new TabControl();


        public MainForm(FormFactory formFactory, 
                        RegistryKeys registryKeys, 
                        CheckIfRecordExists checkIfRecordExists, 
                        GetFieldsValues getFieldsValues,
                        IDataManager dataManager, 
                        Pagination pagination)
        {
            this.formFactory = formFactory;
            this.registryKeys = registryKeys;
            this.checkIfRecordExists = checkIfRecordExists;
            this.getFieldsValues = getFieldsValues;
            this.dataManager = dataManager;
            this.pagination = pagination;
            InitializeComponent();
            InitializeTabControl();
           _ = GetSession();
        }
        

        #region -- TAB CONTROLLER --
        private void InitializeTabControl()
        {
            tabControlMain = new TabControl
            {
                Dock = DockStyle.Fill,
                DrawMode = TabDrawMode.OwnerDrawFixed,
                SizeMode = TabSizeMode.Normal, // I-set sa Normal para ma-customize ang width ng bawat tab
                Padding = new System.Drawing.Point(45, 8) // Magdagdag ng padding para sa mas magandang itsura
            };

            tabControlMain.DrawItem += TabControlMain_DrawItem;
            tabControlMain.MouseDown += TabControlMain_MouseDown;

            mainPanel.Controls.Add(tabControlMain);
        }

        private void TabControlMain_DrawItem(object sender, DrawItemEventArgs e)
        {
            var tab = tabControlMain.TabPages[e.Index];
            var rect = e.Bounds;
            var text = tab.Text;

            // Tukuyin ang kulay ng background at text base sa kung selected ang tab o hindi
            System.Drawing.Color backColor;
            System.Drawing.Color textColor;

            if (e.State == DrawItemState.Selected)
            {
                backColor = System.Drawing.Color.White; // Selected tab background
                textColor = System.Drawing.Color.Black; // Selected tab text
            }
            else
            {
                backColor = System.Drawing.Color.Gray; // Unselected tab background
                textColor = System.Drawing.Color.White; // Unselected tab text
            }

            // I-fill ang background ng tab
            using (var backBrush = new System.Drawing.SolidBrush(backColor))
            {
                e.Graphics.FillRectangle(backBrush, rect);
            }

            // Gumamit ng Graphics para i-measure ang width ng text
            using (var textBrush = new System.Drawing.SolidBrush(textColor))
            using (var font = new System.Drawing.Font("Arial", 10, System.Drawing.FontStyle.Bold))
            {
                SizeF textSize = e.Graphics.MeasureString(text, font);
                int tabWidth = (int)textSize.Width + 30; // Magdagdag ng padding para sa close button

                // I-update ang width ng tab
                tabControlMain.TabPages[e.Index].Width = tabWidth;

                // I-draw ang text
                e.Graphics.DrawString(text, font, textBrush, rect.X + 5, rect.Y + 7);
            }

            // I-draw ang close button (X)
            var closeRect = new System.Drawing.Rectangle(rect.Right - 18, rect.Top + 6, 12, 12);
            using (var pen = new System.Drawing.Pen(textColor, 2)) // Gamitin ang textColor para sa close button
            {
                e.Graphics.DrawLine(pen, closeRect.Left, closeRect.Top, closeRect.Right, closeRect.Bottom);
                e.Graphics.DrawLine(pen, closeRect.Right, closeRect.Top, closeRect.Left, closeRect.Bottom);
            }

            e.DrawFocusRectangle();
        }

        private void TabControlMain_MouseDown(object sender, MouseEventArgs e)
        {
            for (int i = 0; i < tabControlMain.TabPages.Count; i++)
            {
                var tabRect = tabControlMain.GetTabRect(i);
                var closeRect = new System.Drawing.Rectangle(tabRect.Right - 18, tabRect.Top + 6, 12, 12);

                if (closeRect.Contains(e.Location)) // Check kung na-click ang (X)
                {
                    CloseTab(i);
                    break;
                }
            }
        }

        private void CloseTab(int index)
        {
            var tabPage = tabControlMain.TabPages[index];
            var formName = tabPage.Name;

            if (openForms.ContainsKey(formName))
            {
                FormHelper.CloseFormWithFade(openForms[formName]);
                openForms.Remove(formName);
            }

            // Ang magdi-dispose sa form ay TabControl mismo
            tabControlMain.TabPages.RemoveAt(index);
        }


        public void OpenFormInTab(Form form, string title)
        {
            if (openForms.ContainsKey(title))
            {
                tabControlMain.SelectedTab = tabControlMain.TabPages[title];
                return;
            }

            form.TopLevel = false;
            form.FormBorderStyle = FormBorderStyle.None;
            form.Dock = DockStyle.Fill;

            var tabPage = new TabPage(title) { Name = title };
            tabPage.Controls.Add(form);
            tabControlMain.TabPages.Add(tabPage);
            tabControlMain.SelectedTab = tabPage;

            // I-update ang width ng tab base sa haba ng title
            using (Graphics g = tabControlMain.CreateGraphics())
            {
                SizeF textSize = g.MeasureString(title, tabControlMain.Font);
                int tabWidth = (int)textSize.Width + 30; // Magdagdag ng padding para sa close button

                // I-update ang width ng tab
                tabPage.Width = tabWidth;
            }

            openForms[title] = form;
            FormHelper.OpenFormWithFade(form, false);
        } 
        #endregion


        public void userIsLogin(string userNumber)
        {
            usernameLbl.Text = userNumber;
            panel1.Visible = true;
            UsersBtn.Visible = true;
            SettingsBtn.Visible = true;
            mainPanel.Visible = true;
        }


        public void userIsLogout()
        {
            panel1.Visible = false;
            UsersBtn.Visible = false;
            SettingsBtn.Visible = false;
            mainPanel.Visible = false;

            loginForm loginForm = new loginForm(registryKeys, getFieldsValues, dataManager, this);
            loginForm.MdiParent = this; // Set parent
            FormHelper.OpenFormWithFade(loginForm, false);

            // Gamitin ang `Shown` event para i-focus ang textbox kapag visible na ang form
            loginForm.Shown += (s, e) => loginForm.txtUsername.Focus();

        }

        public async Task GetSession()
        {
            await dataManager.CommitAndRollbackMethod(async (connection, transaction) =>
            {
                await ProcessSession(connection, transaction); // Tawagin ang Process method na may transaction at connection
            });
        }

        private async Task ProcessSession(IDbConnection connection, IDbTransaction transaction)
        {
            var userRegistInfo = registryKeys.GetRegistLocalSession<RegistUserDto>();

            string token = userRegistInfo.AuthToken;
            string usernumber = userRegistInfo.UserNumber;
            string userlevel = userRegistInfo.UserLevel;

            if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(usernumber) || string.IsNullOrEmpty(userlevel))
            {
                userIsLogout();
                return;
            }

            token = await DataProtectorHelper.Unprotect(token);
            usernumber = await DataProtectorHelper.Unprotect(usernumber);
            userlevel = await DataProtectorHelper.Unprotect(userlevel);

            if (JwtTokenHelper.IsTokenExpired(token))
            {
                bool isExist = await checkIfRecordExists.ExecuteAsync(
                    new List<object> { usernumber },
                    "Users",
                    "UserNumber = # AND IsSessionActive = true"
                );

                if (!isExist)
                {
                    userIsLogout();
                    throw new KeyNotFoundException("User not found in Session.");
                }

                var userDto = new UserUpdateDto
                {
                    UserNumber = usernumber, // always have this for Primary Key
                    IsSessionActive = false,
                    CurrentToken = null
                };

                await dataManager.UpdateAsync(
                    entity: userDto,
                    tableName: "Users",
                    connection: connection,
                    transaction: transaction,
                    primaryKey: "UserNumber"
                );

                await registryKeys.DeleteRegistLocalSession<RegistUserDto>();

                userIsLogout();
                
                transaction.Commit(); // Commit changes

                Console.WriteLine("Token Expired","",MessageBoxButtons.OK,MessageBoxIcon.Exclamation);
                return;
            }

            userIsLogin(usernumber);

        }


        private void CloseApp_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void UsersBtn_Click(object sender, EventArgs e)
        {
            var form = formFactory.Create<UsersListForm>();
            OpenFormInTab(form, "Users");
        }

        private void SettingsBtn_Click(object sender, EventArgs e)
        {
            var form = formFactory.Create<loginForm>();
            OpenFormInTab(form, "Settings");
        }

        private async void logoutBtn_Click(object sender, EventArgs e)
        {
            await ServiceLogout();
        }


        public async Task ServiceLogout()
        {
            await dataManager.CommitAndRollbackMethod(async (connection, transaction) =>
            {
                await ProcessLogout(connection, transaction); // Tawagin ang Process method na may transaction at connection
            });
        }

        private async Task ProcessLogout(IDbConnection connection, IDbTransaction transaction)
        {

            var userRegistInfo = registryKeys.GetRegistLocalSession<RegistUserDto>();

            string token = userRegistInfo.AuthToken;
            string usernumber = userRegistInfo.UserNumber;
            string userlevel = userRegistInfo.UserLevel;

            if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(usernumber) || string.IsNullOrEmpty(userlevel))
            {
                userIsLogout();
                return;
            }

            token = await DataProtectorHelper.Unprotect(token);
            usernumber = await DataProtectorHelper.Unprotect(usernumber);
            userlevel = await DataProtectorHelper.Unprotect(userlevel);


            Dictionary<string, object> GetValues = await
                getFieldsValues.ExecuteAsync(
                    new List<object> { usernumber }, // Parameters
                    "Users",
                    new List<string> { "UserNumber", "IsSessionActive", "CurrentToken" }, // this is for like SUM(Quantity) As TotalQuantity
                    new List<string> { "UserNumber", "IsSessionActive", "CurrentToken" }, // this is fix where the name of field
                    "UserNumber = #"
                );

            string userNumber =
                GetValues.ContainsKey("UserNumber") &&
                !string.IsNullOrEmpty(GetValues["UserNumber"]?.ToString())
                ? Convert.ToString(GetValues["UserNumber"])
                : string.Empty;

            string userToken =
                GetValues.ContainsKey("CurrentToken") &&
                !string.IsNullOrEmpty(GetValues["CurrentToken"]?.ToString())
                ? Convert.ToString(GetValues["CurrentToken"])
                : string.Empty;

            bool userSession =
                GetValues.ContainsKey("IsSessionActive") &&
                !string.IsNullOrEmpty(GetValues["IsSessionActive"]?.ToString())
                ? Convert.ToBoolean(GetValues["IsSessionActive"])
                : false;


            if (userNumber == null)
            {
                throw new KeyNotFoundException("User not found.");
            }

            if (userSession == false)
            {
                throw new KeyNotFoundException("The user is already inactive.");
            }

            if (!PasswordHelper.VerifyPassword(token, userToken))
            {
                // Optionally, handle the case where the token does not match
                throw new KeyNotFoundException("Token does not match.");
            }

            var userDto = new UserUpdateDto
            {
                UserNumber = usernumber, // always have this for Primary Key
                IsSessionActive = false,
                CurrentToken = null
            };

            await dataManager.UpdateAsync(
                entity: userDto,
                tableName: "Users",
                connection: connection,
                transaction: transaction,
                primaryKey: "UserNumber"
            );


            // Call the method to delete registry values
            await registryKeys.DeleteRegistLocalSession<RegistUserDto>();


            transaction.Commit(); // Commit changes
            userIsLogout();

        }

    }
}
