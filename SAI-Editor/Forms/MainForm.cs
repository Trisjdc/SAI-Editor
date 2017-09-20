﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using MySql.Data.MySqlClient;
using SAI_Editor.Classes;
using SAI_Editor.Classes.Database.Classes;
using SAI_Editor.Enumerators;
using SAI_Editor.Forms.SearchForms;
using SAI_Editor.Properties;
using System.IO;
using System.Reflection;
using System.Net;
using System.Threading;
using System.Runtime.InteropServices;
using Newtonsoft.Json;
using SAI_Editor.Classes.CustomControls;
using SAI_Editor.Classes.Serialization;

namespace SAI_Editor.Forms
{
    public partial class MainForm : Form
    {
        public int expandAndContractSpeed = 5, lastSelectedWorkspaceIndex = 0;
        public bool runningConstructor = false;
        private bool contractingToLoginForm = false, expandingToMainForm = false, adjustedLoginSettings = false;
        private int originalHeight = 0, originalWidth = 0, oldWidthTabControlWorkspaces = 0, oldHeightTabControlWorkspaces = 0;
        private int MainFormWidth = (int)SaiEditorSizes.MainFormWidth, MainFormHeight = (int)SaiEditorSizes.MainFormHeight;
        private List<SmartScript> lastDeletedSmartScripts = new List<SmartScript>(), smartScriptsOnClipBoard = new List<SmartScript>();
        private string applicationVersion = String.Empty;

        public UserControlSAI userControl = null;

        public MainForm()
        {
            InitializeComponent();

            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint, true);
            ResizeRedraw = true;
        }

        private async void MainForm_Load(object sender, EventArgs e)
        {
            runningConstructor = true;

            Version version = Assembly.GetExecutingAssembly().GetName().Version;
            applicationVersion = "v" + version.Major + "." + version.Minor + "." + version.Build;
            SetFormTitle("SAI-Editor " + applicationVersion + ": Login");

            menuStrip.Visible = false; //! Doing this in main code so we can actually see the menustrip in designform
            pictureBoxDonate.Visible = false;

            //ImageList imgList = new ImageList();
            //imgList.Images.Add()
            //imgList.TransparentColor = Color.White;
            //pictureBoxDonate.Image = imgList.Images[0];

            Width = (int)SaiEditorSizes.LoginFormWidth;
            Height = (int)SaiEditorSizes.LoginFormHeight;

            originalHeight = Height;
            originalWidth = Width;

            if (Settings.Default.LastFormExtraWidth > 0)
                MainFormWidth += Settings.Default.LastFormExtraWidth;

            if (Settings.Default.LastFormExtraHeight > 0)
                MainFormHeight += Settings.Default.LastFormExtraHeight;

            if (MainFormWidth > SystemInformation.VirtualScreen.Width)
                MainFormWidth = SystemInformation.VirtualScreen.Width;

            if (MainFormHeight > SystemInformation.VirtualScreen.Height)
                MainFormHeight = SystemInformation.VirtualScreen.Height;

            tabControlWorkspaces.DisplayStyle = TabStyle.VisualStudio;
            tabControlWorkspaces.DisplayStyleProvider.ShowTabCloser = true;

            //! HAS to be called before try-catch block
            tabControlWorkspaces.TabPages.Clear(); //! We only have it in the designer to get an idea of how stuff looks

            tabControlWorkspaces.Visible = false;
            customPanelLogin.Visible = true;

            customPanelLogin.Location = new Point(9, 8);

            if (oldWidthTabControlWorkspaces == 0)
                oldWidthTabControlWorkspaces = (int)SaiEditorSizes.TabControlWorkspaceWidth;

            if (oldHeightTabControlWorkspaces == 0)
                oldHeightTabControlWorkspaces = (int)SaiEditorSizes.TabControlWorkspaceHeight;

            //! We first load the information and then change the parameter fields
            await SAI_Editor_Manager.Instance.LoadSQLiteDatabaseInfo();

            if (Settings.Default.HidePass)
                textBoxPassword.PasswordChar = '●';

            try
            {
                textBoxHost.Text = Settings.Default.Host;
                textBoxUsername.Text = Settings.Default.User;
                textBoxPassword.Text = SAI_Editor_Manager.Instance.GetPasswordSetting();
                textBoxWorldDatabase.Text = Settings.Default.Database;
                textBoxPort.Text = Settings.Default.Port > 0 ? Settings.Default.Port.ToString() : String.Empty;
                expandAndContractSpeed = Settings.Default.AnimationSpeed;
                radioButtonConnectToMySql.Checked = Settings.Default.UseWorldDatabase;
                radioButtonDontUseDatabase.Checked = !Settings.Default.UseWorldDatabase;
                SAI_Editor_Manager.Instance.Expansion = (WowExpansion)Settings.Default.WowExpansionIndex;

                menuItemRevertQuery.Enabled = Settings.Default.UseWorldDatabase;
                searchForAQuestToolStripMenuItem1.Enabled = Settings.Default.UseWorldDatabase;
                searchForACreatureEntryToolStripMenuItem.Enabled = Settings.Default.UseWorldDatabase;
                searchForACreatureGuidToolStripMenuItem.Enabled = Settings.Default.UseWorldDatabase;
                searchForAGameobjectEntryToolStripMenuItem.Enabled = Settings.Default.UseWorldDatabase;
                searchForAGameobjectGuidToolStripMenuItem.Enabled = Settings.Default.UseWorldDatabase;
                searchForAGameEventToolStripMenuItem.Enabled = Settings.Default.UseWorldDatabase;
                searchForAnItemEntryToolStripMenuItem.Enabled = Settings.Default.UseWorldDatabase;
                searchForACreatureSummonsIdToolStripMenuItem.Enabled = Settings.Default.UseWorldDatabase;
                searchForAnEquipmentTemplateToolStripMenuItem.Enabled = Settings.Default.UseWorldDatabase;
                searchForAWaypointToolStripMenuItem.Enabled = Settings.Default.UseWorldDatabase;
                searchForANpcTextToolStripMenuItem.Enabled = Settings.Default.UseWorldDatabase;
                searchForAGossipMenuOptionToolStripMenuItem.Enabled = Settings.Default.UseWorldDatabase;
                searchForAGossipOptionIdToolStripMenuItem.Enabled = Settings.Default.UseWorldDatabase;
                adjustedLoginSettings = true;
            }
            catch (Exception)
            {
                MessageBox.Show("Something went wrong when loading the settings.", "Something went wrong!", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            Dictionary<int, SAIUserControlState> states = null;

            if (tabControlWorkspaces.TabPages.Count == 0)
            {
                CreateTabControl(true);
                tabControlWorkspaces.SelectedIndex = 0;

                if (!String.IsNullOrWhiteSpace(Settings.Default.LastStaticInfoPerTab))
                {
                    states = SAIUserControlState.StatesFromJson(Settings.Default.LastStaticInfoPerTab, userControl);

                    if (states != null && states.Count > 0)
                    {
                        userControl.States.Add(states.First().Value);
                        userControl.CurrentState = states.First().Value;
                    }
                }
                else
                {
                    userControl.States.Add(userControl.DefaultState);
                    userControl.CurrentState = userControl.DefaultState;
                }
            }

            try
            {
                userControl.checkBoxListActionlistsOrEntries.Enabled = Settings.Default.UseWorldDatabase;
                userControl.buttonGenerateComments.Enabled = userControl.ListView.Items.Count > 0 && Settings.Default.UseWorldDatabase;
                userControl.buttonSearchForEntryOrGuid.Enabled = Settings.Default.UseWorldDatabase;
                menuItemGenerateComment.Enabled = userControl.ListView.Items.Count > 0 && Settings.Default.UseWorldDatabase;
            }
            catch (Exception)
            {
                MessageBox.Show("Something went wrong when loading the settings.", "Something went wrong!", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            userControl.tabControlParameters.AutoScrollOffset = new Point(5, 5);

            //! Static scrollbar to the parameters tabpage windows
            foreach (TabPage page in userControl.tabControlParameters.TabPages)
            {
                page.HorizontalScroll.Enabled = false;
                page.HorizontalScroll.Visible = false;
                page.AutoScroll = true;
                page.AutoScrollMinSize = new Size(page.Width, page.Height);
            }

            if (!String.IsNullOrEmpty(Settings.Default.LastStaticInfoPerTab) && states != null)
            {
                int ctr = 0;

                foreach (var kvp in states.Skip(1))
                {
                    userControl.States.Add(kvp.Value);

                    CreateTabControl();
                    ctr++;
                }

                tabControlWorkspaces.SelectedIndex = 0;
                userControl.States.First().Load();
                userControl.CurrentState = userControl.States.First();
            }

            if (Settings.Default.AutoConnect)
            {
                checkBoxAutoConnect.Checked = true;

                if (Settings.Default.UseWorldDatabase)
                {
                    SAI_Editor_Manager.Instance.connString = new MySqlConnectionStringBuilder();
                    SAI_Editor_Manager.Instance.connString.Server = Settings.Default.Host;
                    SAI_Editor_Manager.Instance.connString.UserID = Settings.Default.User;
                    SAI_Editor_Manager.Instance.connString.Port = Settings.Default.Port;
                    SAI_Editor_Manager.Instance.connString.Database = Settings.Default.Database;

                    if (Settings.Default.Password.Length > 0)
                        SAI_Editor_Manager.Instance.connString.Password = SAI_Editor_Manager.Instance.GetPasswordSetting();// Settings.Default.Password.ToSecureString().EncryptString(Encoding.Unicode.GetBytes(Settings.Default.Entropy));

                    SAI_Editor_Manager.Instance.ResetWorldDatabase(true);
                }

                if (!Settings.Default.UseWorldDatabase || SAI_Editor_Manager.Instance.worldDatabase.CanConnectToDatabase(SAI_Editor_Manager.Instance.connString, false))
                {
                    SAI_Editor_Manager.Instance.ResetWorldDatabase(Settings.Default.UseWorldDatabase);
                    buttonConnect.PerformClick();

                    if (Settings.Default.InstantExpand)
                        StartExpandingToMainForm(true);
                }
            }

            runningConstructor = false;
        }

        private void SetSizable(bool sizable)
        {
            if (sizable)
            {
                FormBorderStyle = System.Windows.Forms.FormBorderStyle.Sizable;
                MinimumSize = new Size(966, 542);
                tabControlWorkspaces.Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom | AnchorStyles.Top;
            }
            else
            {
                FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
                MinimumSize = new Size(0, 0);
                tabControlWorkspaces.Anchor = AnchorStyles.Left | AnchorStyles.Top;
            }
        }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams handleParam = base.CreateParams;
                handleParam.ExStyle |= 0x02000000; // WS_EX_COMPOSITED       
                return handleParam;
            }
        }

        protected override void WndProc(ref Message m)
        {
            //! Don't allow moving the window while we are expanding or contracting. This is required because
            //! the window often breaks and has an incorrect size in the end if the application had been moved
            //! while expanding or contracting.
            if (((m.Msg == 274 && m.WParam.ToInt32() == 61456) || (m.Msg == 161 && m.WParam.ToInt32() == 2)) && (expandingToMainForm || contractingToLoginForm))
                return;

            base.WndProc(ref m);
        }

        [DllImportAttribute("user32.dll")]
        public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImportAttribute("user32.dll")]
        public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImportAttribute("user32.dll")]
        public static extern bool SetForegroundWindow(IntPtr hWnd);

        private void timerExpandOrContract_Tick(object sender, EventArgs e)
        {
            if (expandingToMainForm)
            {
                MaximumSize = new Size(0, 0);
                MinimumSize = new Size(0, 0);

                if (Height < MainFormHeight)
                {
                    Height += expandAndContractSpeed;

                    if (Height > MainFormHeight)
                        timerExpandOrContract_Tick(sender, e);
                }
                else
                {
                    Height = MainFormHeight;

                    if (Width >= MainFormWidth && timerExpandOrContract.Enabled) //! If both finished
                    {
                        Width = MainFormWidth;
                        timerExpandOrContract.Enabled = false;
                        expandingToMainForm = false;
                        FinishedExpandingOrContracting(true);
                    }
                }

                if (Width < MainFormWidth)
                {
                    Width += expandAndContractSpeed;

                    if (Width > MainFormWidth)
                        timerExpandOrContract_Tick(sender, e);
                }
                else
                {
                    Width = MainFormWidth;

                    if (Height >= MainFormHeight && timerExpandOrContract.Enabled) //! If both finished
                    {
                        Height = MainFormHeight;
                        timerExpandOrContract.Enabled = false;
                        expandingToMainForm = false;
                        FinishedExpandingOrContracting(true);
                    }
                }
            }
            else if (contractingToLoginForm)
            {
                if (Height > originalHeight)
                    Height -= expandAndContractSpeed;
                else
                {
                    Height = originalHeight;

                    if (Width <= originalWidth && timerExpandOrContract.Enabled) //! If both finished
                    {
                        Width = originalWidth;
                        timerExpandOrContract.Enabled = false;
                        contractingToLoginForm = false;
                        FinishedExpandingOrContracting(false);
                    }
                }

                if (Width > originalWidth)
                    Width -= expandAndContractSpeed;
                else
                {
                    Width = originalWidth;

                    if (Height <= originalHeight && timerExpandOrContract.Enabled) //! If both finished
                    {
                        Height = originalHeight;
                        timerExpandOrContract.Enabled = false;
                        contractingToLoginForm = false;
                        FinishedExpandingOrContracting(false);
                    }
                }
            }

            Invalidate();
            Update();
        }

        private void buttonConnect_Click(object sender, EventArgs e)
        {
            if (radioButtonConnectToMySql.Checked)
            {
                if (String.IsNullOrEmpty(textBoxHost.Text))
                {
                    MessageBox.Show("The host field has to be filled!", "An error has occurred!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                if (String.IsNullOrEmpty(textBoxUsername.Text))
                {
                    MessageBox.Show("The username field has to be filled!", "An error has occurred!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                if (textBoxPassword.Text.Length > 0 && String.IsNullOrEmpty(textBoxPassword.Text))
                {
                    MessageBox.Show("The password field can not consist of only whitespaces!", "An error has occurred!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                if (String.IsNullOrEmpty(textBoxWorldDatabase.Text))
                {
                    MessageBox.Show("The world database field has to be filled!", "An error has occurred!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                if (String.IsNullOrEmpty(textBoxPort.Text))
                {
                    MessageBox.Show("The port field has to be filled!", "An error has occurred!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                SAI_Editor_Manager.Instance.connString = new MySqlConnectionStringBuilder();
                SAI_Editor_Manager.Instance.connString.Server = textBoxHost.Text;
                SAI_Editor_Manager.Instance.connString.UserID = textBoxUsername.Text;
                SAI_Editor_Manager.Instance.connString.Port = CustomConverter.ToUInt32(textBoxPort.Text);
                SAI_Editor_Manager.Instance.connString.Database = textBoxWorldDatabase.Text;

                if (textBoxPassword.Text.Length > 0)
                    SAI_Editor_Manager.Instance.connString.Password = textBoxPassword.Text;

                SAI_Editor_Manager.Instance.ResetWorldDatabase(true);
            }

            buttonConnect.Enabled = false;

            Settings.Default.UseWorldDatabase = radioButtonConnectToMySql.Checked;
            Settings.Default.Save();

            if (!radioButtonConnectToMySql.Checked || SAI_Editor_Manager.Instance.worldDatabase.CanConnectToDatabase(SAI_Editor_Manager.Instance.connString))
            {
                StartExpandingToMainForm(Settings.Default.InstantExpand);
                HandleUseWorldDatabaseSettingChanged();
            }

            buttonConnect.Enabled = true;
        }

        private void StartExpandingToMainForm(bool instant = false)
        {
            if (radioButtonConnectToMySql.Checked)
            {
                RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider();
                byte[] buffer = new byte[1024];
                rng.GetBytes(buffer);
                string salt = BitConverter.ToString(buffer);
                rng.Dispose();

                Settings.Default.Entropy = salt;
                Settings.Default.Host = textBoxHost.Text;
                Settings.Default.User = textBoxUsername.Text;
                Settings.Default.Password = textBoxPassword.Text.Length == 0 ? String.Empty : textBoxPassword.Text.ToSecureString().EncryptString(Encoding.Unicode.GetBytes(salt));
                Settings.Default.Database = textBoxWorldDatabase.Text;
                Settings.Default.AutoConnect = checkBoxAutoConnect.Checked;
                Settings.Default.Port = CustomConverter.ToUInt32(textBoxPort.Text);
                Settings.Default.UseWorldDatabase = true;
                Settings.Default.Save();
            }

            ResetFieldsToDefault();

            if (radioButtonConnectToMySql.Checked)
                SetFormTitle("SAI-Editor " + applicationVersion + " - Connection: " + textBoxUsername.Text + ", " + textBoxHost.Text + ", " + textBoxPort.Text);
            else
                SetFormTitle("SAI-Editor " + applicationVersion + " - Creator-only mode, no database connection");

            if (instant)
            {
                Width = MainFormWidth;
                Height = MainFormHeight;
                FinishedExpandingOrContracting(true);
            }
            else
            {
                SAI_Editor_Manager.FormState = FormState.FormStateExpandingOrContracting;
                timerExpandOrContract.Enabled = true;
                expandingToMainForm = true;
            }

            customPanelLogin.Visible = false;

            userControl.panelStaticTooltipTypes.Visible = false;
            userControl.panelStaticTooltipParameters.Visible = false;
        }

        private void ResetFieldsToDefault()
        {
            userControl.ResetFieldsToDefault();
        }

        private void StartContractingToLoginForm(bool instant = false)
        {
            SetSizable(false);

            SetFormTitle("SAI-Editor " + applicationVersion + ": Login");

            if (Settings.Default.ShowTooltipsStaticly)
                userControl.ListView.Height += (int)SaiEditorSizes.ListViewHeightContract;

            if (instant)
            {
                Width = originalWidth;
                Height = originalHeight;
                FinishedExpandingOrContracting(false);
            }
            else
            {
                SAI_Editor_Manager.FormState = FormState.FormStateExpandingOrContracting;
                timerExpandOrContract.Enabled = true;
                contractingToLoginForm = true;
            }

            tabControlWorkspaces.Visible = false;
            menuStrip.Visible = false;
            pictureBoxDonate.Visible = false;
        }

        private void buttonClear_Click(object sender, EventArgs e)
        {
            textBoxHost.Text = String.Empty;
            textBoxUsername.Text = String.Empty;
            textBoxPassword.Text = String.Empty;
            textBoxWorldDatabase.Text = String.Empty;
            textBoxPort.Text = String.Empty;
            checkBoxAutoConnect.Checked = false;
            radioButtonConnectToMySql.Checked = true;
        }

        private void buttonCancel_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void MainForm_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.Enter:
                    switch (SAI_Editor_Manager.FormState)
                    {
                        case FormState.FormStateLogin:
                            buttonConnect.PerformClick();
                            break;
                        case FormState.FormStateMain:
                            break;
                    }
                    break;
            }
        }

        private void menuItemReconnect_Click(object sender, EventArgs e)
        {
            if (SAI_Editor_Manager.FormState != FormState.FormStateMain || userControl.contractingListView || userControl.expandingListView)
                return;

            for (int i = 0; i < Application.OpenForms.Count; ++i)
                if (Application.OpenForms[i] != this)
                    Application.OpenForms[i].Close();

            userControl.panelStaticTooltipTypes.Visible = false;
            userControl.panelStaticTooltipParameters.Visible = false;

            SaveLastUsedFields();
            ResetFieldsToDefault();

            userControl.ListViewList.ClearScripts();

            StartContractingToLoginForm(Settings.Default.InstantExpand);
        }

        private void FinishedExpandingOrContracting(bool expanding)
        {
            SAI_Editor_Manager.FormState = expanding ? FormState.FormStateMain : FormState.FormStateLogin;
            customPanelLogin.Visible = !expanding;
            tabControlWorkspaces.Visible = expanding;
            menuStrip.Visible = expanding;
            pictureBoxDonate.Visible = expanding;
            Invalidate();

            if (!expanding)
                SetHeightLoginFormBasedOnSetting();

            int width = (int)SaiEditorSizes.TabControlWorkspaceWidth + MainFormWidth - (int)SaiEditorSizes.MainFormWidth;
            int height = (int)SaiEditorSizes.TabControlWorkspaceHeight + MainFormHeight - (int)SaiEditorSizes.MainFormHeight;
            tabControlWorkspaces.Size = new Size(width, height);
            HandleTabControlWorkspacesResized();

            userControl.FinishedExpandingOrContracting(expanding);

            SetSizable(expanding);

            Update();

            HandleTabControlWorkspacesResized(true);
        }

        private void menuItemExit_Click(object sender, System.EventArgs e)
        {
            if (SAI_Editor_Manager.FormState == FormState.FormStateMain)
                TryCloseApplication();
        }

        private void TryCloseApplication()
        {
            if (!Settings.Default.PromptToQuit || DialogResult.Yes == MessageBox.Show("Are you sure you want to quit?", "Are you sure?", MessageBoxButtons.YesNo, MessageBoxIcon.Question))
                Close();
        }

        private void menuItemSettings_Click(object sender, EventArgs e)
        {
            if (SAI_Editor_Manager.FormState != FormState.FormStateMain)
                return;

            using (SettingsForm settingsForm = new SettingsForm())
                settingsForm.ShowDialog(this);
        }

        private void menuItemAbout_Click(object sender, EventArgs e)
        {
            if (SAI_Editor_Manager.FormState != FormState.FormStateMain)
                return;

            using (AboutForm aboutForm = new AboutForm())
                aboutForm.ShowDialog(this);
        }

        private void menuOptionDeleteSelectedRow_Click(object sender, EventArgs e)
        {
            CustomObjectListView listViewSmartScripts = userControl.ListView;

            if (SAI_Editor_Manager.FormState != FormState.FormStateMain || ((SmartScriptList)listViewSmartScripts.List).SelectedScript == null)
                return;

            userControl.DeleteSelectedRow();
        }

        private void menuItemCopySelectedRowListView_Click(object sender, EventArgs e)
        {
            CustomObjectListView listViewSmartScripts = userControl.ListView;

            if (SAI_Editor_Manager.FormState != FormState.FormStateMain || ((SmartScriptList)listViewSmartScripts.List).SelectedScript == null)
                return;

            smartScriptsOnClipBoard.Add(((SmartScriptList)listViewSmartScripts.List).SelectedScript.Clone());
        }

        private void menuItemPasteLastCopiedRow_Click(object sender, EventArgs e)
        {
            CustomObjectListView listViewSmartScripts = userControl.ListView;

            if (SAI_Editor_Manager.FormState != FormState.FormStateMain || ((SmartScriptList)listViewSmartScripts.List).SelectedScript == null)
                return;

            if (smartScriptsOnClipBoard.Count <= 0)
            {
                MessageBox.Show("No smart scripts have been copied in this session!", "Something went wrong!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            SmartScript newSmartScript = smartScriptsOnClipBoard.Last().Clone();
            listViewSmartScripts.List.AddScript(newSmartScript);
        }

        private async void buttonSearchWorldDb_Click(object sender, EventArgs e)
        {
            buttonSearchWorldDb.Enabled = false;

            SAI_Editor_Manager.Instance.ResetWorldDatabase(false);
            List<string> databaseNames = await SAI_Editor_Manager.Instance.GetDatabasesInConnection(textBoxHost.Text, textBoxUsername.Text, CustomConverter.ToUInt32(textBoxPort.Text), textBoxPassword.Text);

            if (databaseNames != null && databaseNames.Count > 0)
                using (SelectDatabaseForm selectDatabaseForm = new SelectDatabaseForm(databaseNames, textBoxWorldDatabase))
                    selectDatabaseForm.ShowDialog(this);

            buttonSearchWorldDb.Enabled = true;
        }

        private void testToolStripMenuItemDeleteRow_Click(object sender, EventArgs e)
        {
            userControl.DeleteSelectedRow();
        }

        private void smartAIWikiToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SAI_Editor_Manager.Instance.StartProcess("http://collab.kpsn.org/display/tc/smart_scripts");
        }

        private async void generateSQLToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (SAI_Editor_Manager.FormState != FormState.FormStateMain)
                return;

            using (SqlOutputForm sqlOutputForm = new SqlOutputForm(await userControl.GenerateSmartAiSqlFromListView(), true, await userControl.GenerateSmartAiRevertQuery()))
                sqlOutputForm.ShowDialog(this);
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (userControl != null)
                foreach (Control control in userControl.Controls)
                    control.Enabled = false;

            foreach (Control control in Controls)
                control.Enabled = false;

            if (SAI_Editor_Manager.SaveSettingsOnExit)
            {
                if (adjustedLoginSettings)
                    SaveLastUsedFields();

                if (SAI_Editor_Manager.FormState == FormState.FormStateMain)
                {
                    Settings.Default.LastFormExtraWidth = Width - (int)SaiEditorSizes.MainFormWidth;
                    Settings.Default.LastFormExtraHeight = Height - (int)SaiEditorSizes.MainFormHeight;
                    Settings.Default.Save();
                }
            }
        }

        private void SaveLastUsedFields()
        {
            Settings.Default.ShowBasicInfo = userControl.checkBoxShowBasicInfo.Checked;
            Settings.Default.LockSmartScriptId = userControl.checkBoxLockEventId.Checked;
            Settings.Default.ListActionLists = userControl.checkBoxListActionlistsOrEntries.Checked;
            Settings.Default.AllowChangingEntryAndSourceType = userControl.checkBoxAllowChangingEntryAndSourceType.Checked;
            Settings.Default.PhaseHighlighting = false;// userControl.checkBoxUsePhaseColors.Checked;
            Settings.Default.ShowTooltipsStaticly = userControl.checkBoxUseStaticTooltips.Checked;

            string lastStaticInfoPerTab = String.Empty;

            var objs = new List<object>();

            userControl.CurrentState.Save(userControl);

            int ctr = 0;

            foreach (SAIUserControlState state in userControl.States)
            {
                objs.Add(new
                {
                    Workspace = ctr++,
                    Value = JsonConvert.DeserializeObject(JsonConvert.SerializeObject(state.ToStateObjects(), new CustomStateSerializer()))
                });
            }

            Settings.Default.LastStaticInfoPerTab = JsonConvert.SerializeObject(new { Workspaces = objs }, Formatting.Indented);

            if (SAI_Editor_Manager.FormState == FormState.FormStateLogin)
            {
                RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider();
                byte[] buffer = new byte[1024];
                rng.GetBytes(buffer);
                string salt = BitConverter.ToString(buffer);
                rng.Dispose();

                Settings.Default.Entropy = salt;
                Settings.Default.Host = textBoxHost.Text;
                Settings.Default.User = textBoxUsername.Text;
                Settings.Default.Password = textBoxPassword.Text.Length == 0 ? String.Empty : textBoxPassword.Text.ToSecureString().EncryptString(Encoding.Unicode.GetBytes(salt));
                Settings.Default.Database = textBoxWorldDatabase.Text;
                Settings.Default.Port = CustomConverter.ToUInt32(textBoxPort.Text);
                Settings.Default.UseWorldDatabase = radioButtonConnectToMySql.Checked;
                Settings.Default.AutoConnect = checkBoxAutoConnect.Checked;
            }

            Settings.Default.Save();
        }

        private void menuItemRevertQuery_Click(object sender, EventArgs e)
        {
            if (SAI_Editor_Manager.FormState != FormState.FormStateMain)
                return;

            using (RevertQueryForm revertQueryForm = new RevertQueryForm())
                revertQueryForm.ShowDialog(this);
        }

        private async void menuItemGenerateCommentListView_Click(object sender, EventArgs e)
        {
            if (SAI_Editor_Manager.FormState != FormState.FormStateMain || !Settings.Default.UseWorldDatabase)
                return;

            await userControl.GenerateCommentListView();
        }

        private void menuItemDuplicateSelectedRow_Click(object sender, EventArgs e)
        {
            if (SAI_Editor_Manager.FormState != FormState.FormStateMain)
                return;

            userControl.DuplicateSelectedRow();
        }

        private void menuItemLoadSelectedEntry_Click(object sender, EventArgs e)
        {
            userControl.LoadSelectedEntry();
        }

        private void radioButtonConnectToMySql_CheckedChanged(object sender, EventArgs e)
        {
            HandleRadioButtonUseDatabaseChanged();
        }

        private void radioButtonDontUseDatabase_CheckedChanged(object sender, EventArgs e)
        {
            HandleRadioButtonUseDatabaseChanged();
        }

        private void HandleRadioButtonUseDatabaseChanged()
        {
            textBoxHost.Enabled = radioButtonConnectToMySql.Checked;
            textBoxUsername.Enabled = radioButtonConnectToMySql.Checked;
            textBoxPassword.Enabled = radioButtonConnectToMySql.Checked;
            textBoxWorldDatabase.Enabled = radioButtonConnectToMySql.Checked;
            textBoxPort.Enabled = radioButtonConnectToMySql.Checked;
            buttonSearchWorldDb.Enabled = radioButtonConnectToMySql.Checked;
            labelDontUseDatabaseWarning.Visible = !radioButtonConnectToMySql.Checked;

            Settings.Default.UseWorldDatabase = radioButtonConnectToMySql.Checked;
            Settings.Default.Save();

            SetHeightLoginFormBasedOnSetting();
        }

        private void SetHeightLoginFormBasedOnSetting()
        {
            if (SAI_Editor_Manager.FormState != FormState.FormStateMain)
            {
                if (radioButtonConnectToMySql.Checked)
                {
                    MaximumSize = new Size((int)SaiEditorSizes.MainFormWidth, (int)SaiEditorSizes.MainFormHeight);
                    Height = (int)SaiEditorSizes.LoginFormHeight;
                }
                else
                {
                    MaximumSize = new Size((int)SaiEditorSizes.MainFormWidth, (int)SaiEditorSizes.MainFormHeight);
                    Height = (int)SaiEditorSizes.LoginFormHeightShowWarning;
                }
            }
        }

        public void HandleUseWorldDatabaseSettingChanged()
        {
            radioButtonConnectToMySql.Checked = Settings.Default.UseWorldDatabase;
            radioButtonDontUseDatabase.Checked = !Settings.Default.UseWorldDatabase;

            userControl.buttonSearchForEntryOrGuid.Enabled = Settings.Default.UseWorldDatabase || userControl.comboBoxSourceType.SelectedIndex == 2;
            userControl.pictureBoxLoadScript.Enabled = userControl.textBoxEntryOrGuid.Text.Length > 0 && Settings.Default.UseWorldDatabase;
            userControl.checkBoxListActionlistsOrEntries.Enabled = Settings.Default.UseWorldDatabase;
            userControl.buttonGenerateComments.Enabled = userControl.ListView.Items.Count > 0 && Settings.Default.UseWorldDatabase;

            menuItemRevertQuery.Enabled = Settings.Default.UseWorldDatabase;
            menuItemGenerateComment.Enabled = userControl.ListView.Items.Count > 0 && Settings.Default.UseWorldDatabase;
            menuItemGenerateCommentListView.Enabled = Settings.Default.UseWorldDatabase;
            menuItemLoadSelectedEntryListView.Enabled = Settings.Default.UseWorldDatabase;
            searchForAQuestToolStripMenuItem1.Enabled = Settings.Default.UseWorldDatabase;
            searchForACreatureEntryToolStripMenuItem.Enabled = Settings.Default.UseWorldDatabase;
            searchForACreatureGuidToolStripMenuItem.Enabled = Settings.Default.UseWorldDatabase;
            searchForAGameobjectEntryToolStripMenuItem.Enabled = Settings.Default.UseWorldDatabase;
            searchForAGameobjectGuidToolStripMenuItem.Enabled = Settings.Default.UseWorldDatabase;
            searchForAGameEventToolStripMenuItem.Enabled = Settings.Default.UseWorldDatabase;
            searchForAnItemEntryToolStripMenuItem.Enabled = Settings.Default.UseWorldDatabase;
            searchForACreatureSummonsIdToolStripMenuItem.Enabled = Settings.Default.UseWorldDatabase;
            searchForAnEquipmentTemplateToolStripMenuItem.Enabled = Settings.Default.UseWorldDatabase;
            searchForAWaypointToolStripMenuItem.Enabled = Settings.Default.UseWorldDatabase;
            searchForANpcTextToolStripMenuItem.Enabled = Settings.Default.UseWorldDatabase;
            searchForAGossipMenuOptionToolStripMenuItem.Enabled = Settings.Default.UseWorldDatabase;
            searchForAGossipOptionIdToolStripMenuItem.Enabled = Settings.Default.UseWorldDatabase;

            string newTitle = "SAI-Editor " + applicationVersion + " - ";

            if (Settings.Default.UseWorldDatabase)
                newTitle += "Connection: " + Settings.Default.User + ", " + Settings.Default.Host + ", " + Settings.Default.Port.ToString();
            else
                newTitle += "Creator-only mode, no database connection";

            SetFormTitle(newTitle);
        }

        private void menuItemRetrieveLastDeletedRow_Click(object sender, EventArgs e)
        {
            if (lastDeletedSmartScripts.Count == 0)
            {
                MessageBox.Show("There are no items deleted in this session ready to be restored.", "Nothing to retrieve!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            userControl.ListViewList.AddScript(lastDeletedSmartScripts.Last());
            lastDeletedSmartScripts.Remove(lastDeletedSmartScripts.Last());
        }

        private void ShowSearchFromDatabaseForm(TextBox textBoxToChange, DatabaseSearchFormType searchType)
        {
            userControl.ShowSearchFromDatabaseForm(textBoxToChange, searchType);
        }

        private void searchForASpellToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            ShowSearchFromDatabaseForm(null, DatabaseSearchFormType.DatabaseSearchFormTypeSpell);
        }

        private void searchForAFactionToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            ShowSearchFromDatabaseForm(null, DatabaseSearchFormType.DatabaseSearchFormTypeFaction);
        }

        private void searchForAnEmoteToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            ShowSearchFromDatabaseForm(null, DatabaseSearchFormType.DatabaseSearchFormTypeEmote);
        }

        private void searchForAMapToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            ShowSearchFromDatabaseForm(null, DatabaseSearchFormType.DatabaseSearchFormTypeQuest);
        }

        private void searchForAQuestToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            ShowSearchFromDatabaseForm(null, DatabaseSearchFormType.DatabaseSearchFormTypeMap);
        }

        private void searchForAZoneToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ShowSearchFromDatabaseForm(null, DatabaseSearchFormType.DatabaseSearchFormTypeAreaOrZone);
        }

        private void searchForACreatureEntryToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ShowSearchFromDatabaseForm(null, DatabaseSearchFormType.DatabaseSearchFormTypeCreatureEntry);
        }

        private void searchForACreatureGuidToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ShowSearchFromDatabaseForm(null, DatabaseSearchFormType.DatabaseSearchFormTypeCreatureGuid);
        }

        private void searchForAGameobjectEntryToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ShowSearchFromDatabaseForm(null, DatabaseSearchFormType.DatabaseSearchFormTypeGameobjectEntry);
        }

        private void searchForAGameobjectGuidToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ShowSearchFromDatabaseForm(null, DatabaseSearchFormType.DatabaseSearchFormTypeGameobjectGuid);
        }

        private void searchForASoundToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ShowSearchFromDatabaseForm(null, DatabaseSearchFormType.DatabaseSearchFormTypeSound);
        }

        private void searchForAnAreatriggerToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ShowSearchFromDatabaseForm(null, DatabaseSearchFormType.DatabaseSearchFormTypeAreaTrigger);
        }

        private void searchForAGameEventToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ShowSearchFromDatabaseForm(null, DatabaseSearchFormType.DatabaseSearchFormTypeGameEvent);
        }

        private void searchForAnItemEntryToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ShowSearchFromDatabaseForm(null, DatabaseSearchFormType.DatabaseSearchFormTypeItemEntry);
        }

        private void searchForACreatureSummonsIdToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ShowSearchFromDatabaseForm(null, DatabaseSearchFormType.DatabaseSearchFormTypeSummonsId);
        }

        private void searchForATaxiPathToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ShowSearchFromDatabaseForm(null, DatabaseSearchFormType.DatabaseSearchFormTypeTaxiPath);
        }

        private void searchForAnEquipmentTemplateToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ShowSearchFromDatabaseForm(null, DatabaseSearchFormType.DatabaseSearchFormTypeEquipTemplate);
        }

        private void searchForAWaypointToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ShowSearchFromDatabaseForm(null, DatabaseSearchFormType.DatabaseSearchFormTypeWaypoint);
        }

        private void searchForANpcTextToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ShowSearchFromDatabaseForm(null, DatabaseSearchFormType.DatabaseSearchFormTypeNpcText);
        }

        private void searchForAGossipOptionIdToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ShowSearchFromDatabaseForm(null, DatabaseSearchFormType.DatabaseSearchFormTypeGossipMenuOptionMenuId);
        }

        private void searchForAGossipMenuOptionToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ShowSearchFromDatabaseForm(null, DatabaseSearchFormType.DatabaseSearchFormTypeGossipMenuOptionId);
        }

        private void conditionEditorToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (SAI_Editor_Manager.FormState != FormState.FormStateMain)
                return;

            foreach (Form form in Application.OpenForms)
            {
                if (form.Name == "ConditionForm")
                {
                    form.BringToFront();
                    form.Show(); //! Show the form in case it's hidden
                    (form as ConditionForm).formHidden = false;
                    return;
                }
            }

            ConditionForm conditionForm = new ConditionForm();
            conditionForm.formHidden = false;
            conditionForm.Show();
        }

        Dictionary<string, Type> searchEventHandlers = new Dictionary<string, Type>()
        {
            {"Search for gameobject flags", typeof(MultiSelectForm<GoFlags>)},
            {"Search for unit flags", typeof(MultiSelectForm<UnitFlags>)},
            {"Search for unit flags 2", typeof(MultiSelectForm<UnitFlags2>)},
            {"Search for dynamic flags", typeof(MultiSelectForm<DynamicFlags>)},
            {"Search for npc flags", typeof(MultiSelectForm<NpcFlags>)},
            {"Search for unit stand flags", typeof(SingleSelectForm<UnitStandStateType>)},
            {"Search for unit bytes1 flags", typeof(MultiSelectForm<UnitBytes1_Flags>)},
            {"Search for SAI event flags", typeof(MultiSelectForm<SmartEventFlags>)},
            {"Search for SAI phase masks", typeof(MultiSelectForm<SmartPhaseMasks>)},
            {"Search for SAI cast flags", typeof(MultiSelectForm<SmartCastFlags>)},
            {"Search for SAI templates", typeof(SingleSelectForm<SmartAiTemplates>)},
            {"Search for SAI respawn conditions", typeof(SingleSelectForm<SmartRespawnCondition>)},
            {"Search for SAI event types", typeof(SingleSelectForm<SmartEvent>)},
            {"Search for SAI action types", typeof(SingleSelectForm<SmartAction>)},
            {"Search for SAI target types", typeof(SingleSelectForm<SmartTarget>)},
            {"Search for SAI actionlist timer update types", typeof(SingleSelectForm<SmartActionlistTimerUpdateType>)},
            {"Search for gameobject states", typeof(SingleSelectForm<GoStates>)},
            {"Search for react states", typeof(SingleSelectForm<ReactState>)},
            {"Search for sheath states", typeof(SingleSelectForm<SheathState>)},
            {"Search for movement generator types", typeof(SingleSelectForm<MovementGeneratorType>)},
            {"Search for spell schools", typeof(SingleSelectForm<SpellSchools>)},
            {"Search for power types", typeof(SingleSelectForm<PowerTypes>)},
            {"Search for unit stand state types", typeof(SingleSelectForm<UnitStandStateType>)},
            {"Search for temp summon types", typeof(SingleSelectForm<TempSummonType>)},
        };

        private void searchForFlagsMenuItem_Click(object sender, EventArgs e)
        {
            using (Form selectForm = (Form)Activator.CreateInstance(searchEventHandlers[((ToolStripItem)sender).Text], new object[] { null }))
                selectForm.ShowDialog(this);
        }

        private void tabControlWorkspaces_SelectedIndexChanged(object sender, EventArgs e)
        {
            //! New workspace is being created
            if (tabControlWorkspaces.SelectedTab != null && tabControlWorkspaces.SelectedTab.Text == "+")
            {
                if (tabControlWorkspaces.TabPages.Count > (int)MiscEnumerators.MaxWorkSpaceCount)
                {
                    MessageBox.Show("You can't have more than " + (int)MiscEnumerators.MaxWorkSpaceCount +
                        " different workspaces open at the same time. This limit is created to avoid start-up delays.",
                        "Workspace limit", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);

                    tabControlWorkspaces.SelectedIndex = lastSelectedWorkspaceIndex;
                    return;
                }

                userControl.AddWorkSpace();

                CreateTabControl();
            }

            if (lastSelectedWorkspaceIndex < tabControlWorkspaces.TabPages.Count)
                tabControlWorkspaces.TabPages[lastSelectedWorkspaceIndex].Controls.Remove(userControl);

            tabControlWorkspaces.TabPages[tabControlWorkspaces.SelectedIndex].Controls.Add(userControl);

            if (tabControlWorkspaces.SelectedIndex < userControl.States.Count)
                userControl.CurrentState = userControl.States[tabControlWorkspaces.SelectedIndex];

            lastSelectedWorkspaceIndex = tabControlWorkspaces.SelectedIndex;

            if (userControl.ListView.Objects.Cast<object>().Count<object>() > 0)
                userControl.ListView.SelectObject(userControl.ListView.Objects.Cast<object>().ElementAt(0));

            userControl.ListView.Select();
            userControl.ListView.Focus();
        }

        private void CreateTabControl(bool first = false, bool addWorkspace = false)
        {
            if (tabControlWorkspaces.TabPages.Count > (int)MiscEnumerators.MaxWorkSpaceCount)
                return;

            if (!first)
                tabControlWorkspaces.TabPages.RemoveAt(tabControlWorkspaces.TabPages.Count - 1);

            UserControlSAI userControlSAI;

            if (first && userControl == null)
            {
                userControlSAI = new UserControlSAI();
                userControlSAI.Parent = this;
                userControlSAI.LoadUserControl();
            }
            else
                userControlSAI = userControl;

            TabPage newPage = new TabPage();
            newPage.Text = "Workspace " + (tabControlWorkspaces.TabPages.Count + 1);
            newPage.Controls.Add(userControlSAI);

            for (int i = 0; i < tabControlWorkspaces.TabPages.Count; i++)
            {
                if (tabControlWorkspaces.TabPages[i].Text == "+")
                {
                    tabControlWorkspaces.TabPages.RemoveAt(i);
                    break;
                }
            }

            tabControlWorkspaces.TabPages.Add(newPage);
            tabControlWorkspaces.TabPages.Add(new TabPage("+"));

            if (addWorkspace)
                userControlSAI.AddWorkSpace();

            if (first && userControl == null)
                userControl = userControlSAI;

            if (!first)
                tabControlWorkspaces.SelectedIndex = tabControlWorkspaces.TabPages.Count - 2;
        }

        private void pictureBoxDonate_Click(object sender, EventArgs e)
        {
            try
            {
                Process.Start("https://www.paypal.com/cgi-bin/webscr" +
                    "?cmd=" + "_donations" +
                    "&business=jasper.rietrae@gmail.com" +
                    "&lc=NL" +
                    "&item_name=Donating to the creator of SAI-Editor" +
                    "&currency_code=USD" +
                    "&bn=PP%2dDonationsBF");
            }
            catch
            {
                MessageBox.Show("Something went wrong attempting to open the donation page.", "Something went wrong!", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        void SetFormTitle(string title)
        {
            Text = title;

            switch (SAI_Editor_Manager.Instance.Expansion)
            {
                case WowExpansion.ExpansionWotlk:
                    Text += ": Wrath of the Lich King";
                    break;
                case WowExpansion.ExpansionCata:
                    Text += ": Cataclysm";
                    break;
                case WowExpansion.ExpansionMop:
                    Text += ": Mists of Pandaria";
                    break;
                case WowExpansion.ExpansionWod:
                    Text += ": Warlords of Draenor";
                    break;
                default:
                    Text += ": ERROR - No expansion!";
                    break;
            }
        }

        private void settingsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (SAI_Editor_Manager.FormState != FormState.FormStateMain)
                return;

            using (SettingsForm settingsForm = new SettingsForm())
                settingsForm.ShowDialog(this);
        }

        private void tabControlWorkspaces_TabClosing(object sender, TabControlCancelEventArgs e)
        {
            tabControlWorkspaces.SelectedIndex = e.TabPageIndex > 0 ? e.TabPageIndex - 1 : 0;
            userControl.States.RemoveAt(e.TabPageIndex);

            List<TabPage> tabPages = new List<TabPage>();

            //! .Count - 1 because we don't need to rename the "+" tab.
            for (int i = 0; i < tabControlWorkspaces.TabPages.Count - 1; ++i)
                if (i != e.TabPageIndex)
                    tabPages.Add(tabControlWorkspaces.TabPages[i]);

            for (int i = 0; i < tabPages.Count; ++i)
                tabPages[i].Text = "Workspace " + (i + 1).ToString();

            tabControlWorkspaces.Invalidate();
            tabControlWorkspaces.Update();
        }

        private void tabControlWorkspaces_SizeChanged(object sender, EventArgs e)
        {
            HandleTabControlWorkspacesResized(true);
        }

        private void HandleTabControlWorkspacesResized(bool fromResize = false)
        {
            //! This happens on Windows 7 when minimizing for some reason
            if (tabControlWorkspaces.Width == 0 && tabControlWorkspaces.Height == 0)
                return;

            SynchronizeSizeOfUserControlAndListView(fromResize);
        }

        private void SynchronizeSizeOfUserControlAndListView(bool fromResize = false)
        {
            userControl.Width = tabControlWorkspaces.Width;
            userControl.Height = tabControlWorkspaces.Height;

            //! Not sure why but height is really off...
            int contractHeightFromTabControl = 252, contractWidthFromTabControl = (int)SaiEditorSizes.StaticTooltipsPadding;

            if (fromResize && userControl.checkBoxUseStaticTooltips.Checked)
                contractHeightFromTabControl += 60 + 12; //! Height of two panels plus some extra padding

            userControl.ListView.Width = tabControlWorkspaces.Width - contractWidthFromTabControl;
            userControl.ListView.Height = tabControlWorkspaces.Height - contractHeightFromTabControl;

            userControl.panelStaticTooltipTypes.Width = tabControlWorkspaces.Width - (int)SaiEditorSizes.StaticTooltipsPadding;
            userControl.panelStaticTooltipParameters.Width = tabControlWorkspaces.Width - (int)SaiEditorSizes.StaticTooltipsPadding;

            int increaseY = tabControlWorkspaces.Height - oldHeightTabControlWorkspaces;
            userControl.panelStaticTooltipTypes.Location = new Point(userControl.panelStaticTooltipTypes.Location.X, userControl.panelStaticTooltipTypes.Location.Y + increaseY);
            userControl.panelStaticTooltipParameters.Location = new Point(userControl.panelStaticTooltipParameters.Location.X, userControl.panelStaticTooltipParameters.Location.Y + increaseY);

            oldHeightTabControlWorkspaces = tabControlWorkspaces.Height;
            oldWidthTabControlWorkspaces = tabControlWorkspaces.Width;
        }

        private void menuItemReportIssue_Click(object sender, EventArgs e)
        {
            SAI_Editor_Manager.Instance.StartProcess("https://github.com/Discover-/SAI-Editor/issues/new");
        }

        private void menuItemGiveFeedback_Click(object sender, EventArgs e)
        {
            SAI_Editor_Manager.Instance.StartProcess("http://jasper-rietrae.com/#contact/");
        }

        private void MainForm_Resize(object sender, EventArgs e)
        {
            Invalidate();
            Update();
        }

        private void menuItemViewAllPastebins_Click(object sender, EventArgs e)
        {
            if (SAI_Editor_Manager.FormState != FormState.FormStateMain)
                return;

            using (ViewAllPastebinsForm viewAllPastebinsForm = new ViewAllPastebinsForm())
                viewAllPastebinsForm.ShowDialog(this);
        }
    }
}
