using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace WindowPlacementManager;

public partial class FormSelectProcess : Form
{
    public Process SelectedProcess { get; private set; }
    public IntPtr SelectedWindowHandle { get; private set; }
    public string SelectedWindowTitle { get; private set; }

    public class WindowSelectionInfo
    {
        public IntPtr HWnd { get; set; }
        public string Title { get; set; }
        public string ProcessName { get; set; }
        public int ProcessId { get; set; }

        public override string ToString() => $"[{ProcessName} ({ProcessId})] {Title}";
    }

    private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool IsWindowVisible(IntPtr hWnd);


    public FormSelectProcess() => InitializeComponent();

    void FormSelectProcess_Load(object sender, EventArgs e) => LoadWindowsToList();

    private void LoadWindowsToList()
    {
        listViewProcesses.Items.Clear();
        List<WindowSelectionInfo> windows = new List<WindowSelectionInfo>();

        EnumWindows((hWnd, lParam) =>
        {
            if(!IsWindowVisible(hWnd))
                return true;

            StringBuilder titleBuilder = new StringBuilder(256);
            GetWindowText(hWnd, titleBuilder, titleBuilder.Capacity);
            string windowTitle = titleBuilder.ToString();

            if(string.IsNullOrWhiteSpace(windowTitle) || windowTitle.Length < 2)
                return true;

            GetWindowThreadProcessId(hWnd, out uint pid);
            if(pid == 0)
                return true;

            string procName = "N/A";
            Process process = null;
            try
            {
                process = Process.GetProcessById((int)pid);
                procName = process.ProcessName;
            }
            catch
            {
                return true;
            }

            windows.Add(new WindowSelectionInfo
            {
                HWnd = hWnd,
                Title = windowTitle,
                ProcessName = procName,
                ProcessId = (int)pid
            });
            return true;
        }, IntPtr.Zero);

        foreach(var winInfo in windows.OrderBy(w => w.ProcessName).ThenBy(w => w.Title))
        {
            var item = new ListViewItem(winInfo.ProcessName);
            item.SubItems.Add(winInfo.ProcessId.ToString());
            item.SubItems.Add(winInfo.Title);
            item.Tag = winInfo;
            listViewProcesses.Items.Add(item);
        }

        if(listViewProcesses.Items.Count > 0)
        {
            listViewProcesses.AutoResizeColumns(ColumnHeaderAutoResizeStyle.HeaderSize);
            if(listViewProcesses.Columns.Count > 2)
            {
                listViewProcesses.Columns[2].Width = Math.Max(350, listViewProcesses.Columns[2].Width);
            }
            else
            {
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
                }
                catch(ArgumentException)
                {
                    MessageBox.Show($"The process '{selectedWinInfo.ProcessName}' (PID: {selectedWinInfo.ProcessId}) is no longer running.", "Process Exited", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    LoadWindowsToList();
                    return;
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