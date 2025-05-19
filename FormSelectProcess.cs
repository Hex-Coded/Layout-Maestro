using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using WindowPlacementManager.Services;

namespace WindowPlacementManager
{
    public partial class FormSelectProcess : Form
    {
        public Process SelectedProcess { get; private set; }
        public IntPtr SelectedWindowHandle { get; private set; }
        public string SelectedWindowTitle { get; private set; }

        private bool _isSelfElevated;

        public class WindowSelectionInfo
        {
            public IntPtr HWnd { get; set; }
            public string Title { get; set; }
            public string ProcessName { get; set; }
            public int ProcessId { get; set; }
            public bool IsElevated { get; set; }
            public bool AccessDeniedCheckingElevation { get; set; }

            public override string ToString() => $"[{ProcessName} ({ProcessId})] {Title}";
        }

        public FormSelectProcess()
        {
            InitializeComponent();
            _isSelfElevated = ProcessPrivilegeChecker.IsCurrentProcessElevated();
            SetupAdminHint();
        }

        private void SetupAdminHint()
        {
            if(this.pictureBoxAdminHint == null || this.toolTipAdminHint == null)
            {
                Debug.WriteLine("Admin hint UI elements not initialized in FormSelectProcess designer.");
                return;
            }
            this.pictureBoxAdminHint.Visible = true;
            if(_isSelfElevated)
            {
                this.pictureBoxAdminHint.Image = SystemIcons.Information.ToBitmap();
                this.toolTipAdminHint.ToolTipIcon = ToolTipIcon.Info;
                this.toolTipAdminHint.ToolTipTitle = "Administrator Privileges";
                string hintText = "Window Placement Manager is running with Administrator privileges.\nThis allows for better interaction with other applications.";
                this.toolTipAdminHint.SetToolTip(this.pictureBoxAdminHint, hintText);
            }
            else
            {
                this.pictureBoxAdminHint.Image = SystemIcons.Warning.ToBitmap();
                this.toolTipAdminHint.ToolTipIcon = ToolTipIcon.Warning;
                this.toolTipAdminHint.ToolTipTitle = "Administrator Privileges";
                string hintText = "Window Placement Manager is NOT running as Administrator.\n" +
                                  "This may limit its ability to:\n" +
                                  " • Get executable paths for elevated/admin processes.\n" +
                                  " • Launch or manage windows of elevated/admin processes.\n" +
                                  " • Reliably determine admin status for all processes (may show N/A).\n\n" +
                                  "For full functionality, consider running as Administrator.";
                this.toolTipAdminHint.SetToolTip(this.pictureBoxAdminHint, hintText);
            }
        }

        void FormSelectProcess_Load(object sender, EventArgs e) => LoadWindowsToList();

        private void LoadWindowsToList()
        {
            listViewProcesses.Items.Clear();
            List<WindowSelectionInfo> windows = new List<WindowSelectionInfo>();

            Native.EnumWindows((hWnd, lParam) =>
            {
                if(!Native.IsWindowVisible(hWnd)) return true;

                string windowTitle = Native.GetWindowTitle(hWnd);
                if(string.IsNullOrWhiteSpace(windowTitle) || windowTitle.Length < 3) return true;

                Native.GetWindowThreadProcessId(hWnd, out uint pid);
                if(pid == 0) return true;

                string procName = "N/A";
                bool isElevated = false;
                bool accessDeniedChecking = false;

                try
                {
                    using(Process process = Process.GetProcessById((int)pid))
                    {
                        procName = process.ProcessName;
                        if(!process.HasExited)
                        {
                            isElevated = ProcessPrivilegeChecker.IsProcessElevated(process.Id, out accessDeniedChecking);
                        }
                    }
                }
                catch(ArgumentException) { return true; }
                catch(Exception ex)
                {
                    Debug.WriteLine($"Error processing PID {(int)pid} ('{procName}'): {ex.Message}");
                }

                windows.Add(new WindowSelectionInfo
                {
                    HWnd = hWnd,
                    Title = windowTitle,
                    ProcessName = procName,
                    ProcessId = (int)pid,
                    IsElevated = isElevated,
                    AccessDeniedCheckingElevation = accessDeniedChecking
                });
                return true;
            }, IntPtr.Zero);

            foreach(var winInfo in windows.OrderBy(w => w.ProcessName).ThenBy(w => w.Title))
            {
                var item = new ListViewItem(winInfo.ProcessName);
                item.SubItems.Add(winInfo.ProcessId.ToString());
                item.SubItems.Add(winInfo.Title);

                string adminStatusText = winInfo.IsElevated ? "Yes" : "No";
                if(winInfo.AccessDeniedCheckingElevation && !winInfo.IsElevated)
                {
                    adminStatusText = "N/A";
                    item.ToolTipText = "Could not definitively determine admin status (Access Denied or other issue).";
                }
                item.SubItems.Add(adminStatusText);

                if(winInfo.IsElevated && !_isSelfElevated) item.ForeColor = Color.DarkGoldenrod ;
                else if(adminStatusText == "N/A" && !_isSelfElevated) item.ForeColor = Color.OrangeRed;

                item.Tag = winInfo;
                listViewProcesses.Items.Add(item);
            }

            if(listViewProcesses.Items.Count > 0)
            {
                listViewProcesses.AutoResizeColumns(ColumnHeaderAutoResizeStyle.ColumnContent);
                listViewProcesses.AutoResizeColumns(ColumnHeaderAutoResizeStyle.HeaderSize);
                if(listViewProcesses.Columns.Count > 2 && listViewProcesses.Columns[2].Width < 350) listViewProcesses.Columns[2].Width = 350;
                if(listViewProcesses.Columns.Count > 3)
                {
                    listViewProcesses.Columns[3].Width = Math.Max(65, listViewProcesses.Columns[3].Width);
                    listViewProcesses.Columns[3].TextAlign = HorizontalAlignment.Center;
                }
            }
        }

        void buttonRefresh_Click(object sender, EventArgs e) => LoadWindowsToList();
        void listViewProcesses_SelectedIndexChanged(object sender, EventArgs e) => buttonSelect.Enabled = listViewProcesses.SelectedItems.Count > 0;

        void ConfirmSelection()
        {
            if(listViewProcesses.SelectedItems.Count > 0)
            {
                var selectedWinInfo = listViewProcesses.SelectedItems[0].Tag as WindowSelectionInfo;
                if(selectedWinInfo != null)
                {
                    try
                    {
                        SelectedProcess = Process.GetProcessById(selectedWinInfo.ProcessId);
                        if(SelectedProcess.HasExited) throw new ArgumentException("Process has exited.");
                    }
                    catch(ArgumentException ex)
                    {
                        MessageBox.Show($"The process '{selectedWinInfo.ProcessName}' (PID: {selectedWinInfo.ProcessId}) is no longer running or accessible.\n{ex.Message}", "Process Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        LoadWindowsToList(); return;
                    }
                    SelectedWindowHandle = selectedWinInfo.HWnd;
                    SelectedWindowTitle = selectedWinInfo.Title;
                    this.DialogResult = DialogResult.OK;
                    this.Close();
                }
            }
        }
        void buttonSelect_Click(object sender, EventArgs e) => ConfirmSelection();
        void listViewProcesses_DoubleClick(object sender, EventArgs e) => ConfirmSelection();
    }
}