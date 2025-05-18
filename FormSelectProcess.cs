using System;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;

namespace WindowPositioner
{
    public partial class FormSelectProcess : Form
    {
        public Process SelectedProcess { get; private set; }
        public IntPtr SelectedWindowHandle { get; private set; }
        public string SelectedWindowTitle { get; private set; }


        public FormSelectProcess()
        {
            InitializeComponent();
        }

        private void FormSelectProcess_Load(object sender, EventArgs e)
        {
            PopulateProcessList();
        }

        private void PopulateProcessList()
        {
            listViewProcesses.Items.Clear();
            var processesWithWindows = Process.GetProcesses()
                .Where(p => p.MainWindowHandle != IntPtr.Zero && !string.IsNullOrEmpty(p.MainWindowTitle))
                .OrderBy(p => p.ProcessName)
                .ThenBy(p => p.MainWindowTitle);

            foreach(var proc in processesWithWindows)
            {
                try
                {
                    var item = new ListViewItem(proc.ProcessName);
                    item.SubItems.Add(proc.Id.ToString());
                    item.SubItems.Add(proc.MainWindowTitle);
                    item.Tag = new Tuple<Process, IntPtr, string>(proc, proc.MainWindowHandle, proc.MainWindowTitle);
                    listViewProcesses.Items.Add(item);
                }
                catch(Exception ex)
                {
                    Debug.WriteLine($"Error listing process {proc.Id}: {ex.Message}");
                }
            }
            if(listViewProcesses.Items.Count > 0)
            {
                listViewProcesses.AutoResizeColumns(ColumnHeaderAutoResizeStyle.HeaderSize);
                listViewProcesses.Columns[2].Width = Math.Max(300, listViewProcesses.Columns[2].Width);
            }
        }

        private void buttonRefresh_Click(object sender, EventArgs e)
        {
            PopulateProcessList();
        }

        private void listViewProcesses_SelectedIndexChanged(object sender, EventArgs e)
        {
            buttonSelect.Enabled = listViewProcesses.SelectedItems.Count > 0;
        }

        private void ConfirmSelection()
        {
            if(listViewProcesses.SelectedItems.Count > 0)
            {
                var selection = (Tuple<Process, IntPtr, string>)listViewProcesses.SelectedItems[0].Tag;
                SelectedProcess = selection.Item1;
                SelectedWindowHandle = selection.Item2;
                SelectedWindowTitle = selection.Item3;
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
        }

        private void buttonSelect_Click(object sender, EventArgs e)
        {
            ConfirmSelection();
        }

        private void listViewProcesses_DoubleClick(object sender, EventArgs e)
        {
            ConfirmSelection();
        }
    }
}