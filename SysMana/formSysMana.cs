using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing.Imaging;
using System.IO;
using System.Diagnostics;
using System.Runtime.InteropServices;
using CoreAudioApi;
using GenericForms;

namespace SysMana
{
    public partial class formSysMana : Form
    {
        const double VERSION = 1.1;

        [DllImport("kernel32.dll")]
        static extern uint WinExec(string lpCmdLine, uint uCmdShow);
        public const uint SW_NORMAL = 1;

        #region Full Screen Check
        [DllImport("user32.dll")]
        static extern IntPtr GetForegroundWindow();

        // For Windows Mobile, replace user32.dll with coredll.dll
        [DllImport("user32.dll", SetLastError = true)]
        static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll")]
        static extern IntPtr GetShellWindow();

        [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        public static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint cButtons, uint dwExtraInfo);

        [DllImport("user32.dll", SetLastError = true)]
        static extern IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter, string lpszClass, string lpszWindow);

        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

        private const int MOUSEEVENTF_MIDDLEDOWN = 0x0020;
        private const int MOUSEEVENTF_MIDDLEUP = 0x0040;

        [DllImport("user32.dll")]
        static extern int GetSystemMetrics(int smIndex);

        public const int SM_CXSCREEN = 0;
        public const int SM_CYSCREEN = 1;

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool GetWindowRect(IntPtr hWnd, out W32RECT lpRect);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

        [StructLayout(LayoutKind.Sequential)]
        public struct W32RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        public enum DesktopWindow
        {
            ProgMan,
            SHELLDLL_DefViewParent,
            SHELLDLL_DefView,
            SysListView32
        }

        public static IntPtr GetDesktopWindow(DesktopWindow desktopWindow)
        {
            IntPtr _ProgMan = GetShellWindow();
            IntPtr _SHELLDLL_DefViewParent = _ProgMan;
            IntPtr _SHELLDLL_DefView = FindWindowEx(_ProgMan, IntPtr.Zero, "SHELLDLL_DefView", null);
            IntPtr _SysListView32 = FindWindowEx(_SHELLDLL_DefView, IntPtr.Zero, "SysListView32", "FolderView");

            if (_SHELLDLL_DefView == IntPtr.Zero)
            {
                EnumWindows((hwnd, lParam) =>
                {
                    const int maxChars = 256;
                    StringBuilder className = new StringBuilder(maxChars);

                    if (GetClassName(hwnd, className, maxChars) > 0 && className.ToString() == "WorkerW")
                    {
                        IntPtr child = FindWindowEx(hwnd, IntPtr.Zero, "SHELLDLL_DefView", null);
                        if (child != IntPtr.Zero)
                        {
                            _SHELLDLL_DefViewParent = hwnd;
                            _SHELLDLL_DefView = child;
                            _SysListView32 = FindWindowEx(child, IntPtr.Zero, "SysListView32", "FolderView"); ;
                            return false;
                        }
                    }
                    return true;
                }, IntPtr.Zero);
            }

            switch (desktopWindow)
            {
                case DesktopWindow.ProgMan:
                    return _ProgMan;
                case DesktopWindow.SHELLDLL_DefViewParent:
                    return _SHELLDLL_DefViewParent;
                case DesktopWindow.SHELLDLL_DefView:
                    return _SHELLDLL_DefView;
                case DesktopWindow.SysListView32:
                    return _SysListView32;
                default:
                    return IntPtr.Zero;
            }
        }

        bool fullScreenActive()
        {
            int scrX = GetSystemMetrics(SM_CXSCREEN), scrY = GetSystemMetrics(SM_CYSCREEN);

            IntPtr handle = GetForegroundWindow();
            if (handle == IntPtr.Zero || handle.Equals(GetDesktopWindow(DesktopWindow.SHELLDLL_DefViewParent)))
                return false;

            W32RECT wRect;
            if (!GetWindowRect(handle, out wRect))
                return false;

            return scrX == (wRect.Right - wRect.Left) && scrY == (wRect.Bottom - wRect.Top);
        }
        #endregion

        #region Send Window to Back
        [DllImport("user32.dll")]
        static extern bool SetWindowPos(
            IntPtr hWnd,
            IntPtr hWndInsertAfter,
            int X,
            int Y,
            int cx,
            int cy,
            uint uFlags);

        const UInt32 SWP_NOSIZE = 0x0001;
        const UInt32 SWP_NOMOVE = 0x0002;

        static readonly IntPtr HWND_BOTTOM = new IntPtr(1); 
        #endregion

        MMDevice audioDevice;
        DataSources data;
        List<Meter> meters;

        formSetup setup;
        Font font;
        VertAlign align;

        Meter clockMeter;
        bool mousedown = false, mouseOverClock = false, initialized = false, showChangelog;
        Point prevPos;
        int fixedH, prevX, prevY, updateNotifs;
        

        public Image LoadImg(string path)
        {
            if (!File.Exists(path))
                return null;

            Image img = Image.FromFile(path);

            if (img.GetFrameCount(new FrameDimension(img.FrameDimensionsList[0])) > 1)
                ImageAnimator.Animate(img, new EventHandler(this.OnFrameChanged));

            return img;
        }

        public void DisposeImg(Image img)
        {
            try
            {
                if (img != null)
                {
                    if (img.GetFrameCount(new FrameDimension(img.FrameDimensionsList[0])) > 1)
                        ImageAnimator.StopAnimate(img, new EventHandler(this.OnFrameChanged));

                    img.Dispose();
                }
            }
            catch
            {
                //img is disposed so do nothing
            }
        }

        void loadMeters()
        {
            meters = new List<Meter>();

            StreamReader file = new StreamReader(Application.StartupPath + "\\meters.txt");

            while (!file.EndOfStream)
                meters.Add(new Meter(file.ReadLine(), Application.StartupPath + "\\imgs\\", LoadImg, DisposeImg));

            file.Close();
        }

        void loadOptions()
        {
            StreamReader file = new StreamReader(Application.StartupPath + "\\options.txt");

            this.Left = int.Parse(file.ReadLine());
            this.Top = int.Parse(file.ReadLine());

            //timerRefresh.Interval = int.Parse(file.ReadLine());
            timerUpdateData.Interval = int.Parse(file.ReadLine());

            this.Opacity = double.Parse(file.ReadLine().Replace('.', ','));
            fixedH = int.Parse(file.ReadLine());
            this.BackColor = Color.FromArgb(int.Parse(file.ReadLine()), int.Parse(file.ReadLine()), int.Parse(file.ReadLine()));
            this.TransparencyKey = this.BackColor;
            align = (VertAlign)Enum.Parse(typeof(VertAlign), file.ReadLine());
            this.TopMost = bool.Parse(file.ReadLine());
            this.AllowTransparency = bool.Parse(file.ReadLine());
            font = new Font(file.ReadLine(), int.Parse(file.ReadLine()), Misc.GenFontStyle(bool.Parse(file.ReadLine()), bool.Parse(file.ReadLine()), bool.Parse(file.ReadLine()), bool.Parse(file.ReadLine())));
            updateNotifs = int.Parse(file.ReadLine());
            showChangelog = bool.Parse(file.ReadLine());

            file.Close();

            menuOnTop.Checked = this.TopMost;
        }

        void saveOptions()
        {
            StreamWriter file = new StreamWriter(Application.StartupPath + "\\options.txt");

            file.WriteLine(this.Left);
            file.WriteLine(this.Top);
            file.WriteLine(timerRefresh.Interval);
            file.WriteLine(this.Opacity);
            file.WriteLine(fixedH);
            file.WriteLine(this.BackColor.R);
            file.WriteLine(this.BackColor.G);
            file.WriteLine(this.BackColor.B);
            file.WriteLine(align.ToString());
            file.WriteLine(this.TopMost);
            file.WriteLine(this.AllowTransparency);
            file.WriteLine(font.Name);
            file.WriteLine(font.Size);
            file.WriteLine(font.Bold);
            file.WriteLine(font.Italic);
            file.WriteLine(font.Underline);
            file.WriteLine(font.Strikeout);
            file.WriteLine(updateNotifs);
            file.WriteLine(showChangelog);

            file.Close();
        }

        void initDataSources()
        {
            data = new DataSources(meters);
        }

        Meter GetSelectedMeter(int mouseX)
        {
            for (int i = meters.Count - 1; i >= 0; i--)
                if (meters[i].Left < mouseX)
                    return meters[i];

            return null;
        }

        string getTooltip(Meter meter)
        {
            if (meter == null)
                return "";
            else
            {
                string tip;

                if (meter.Data == "Dota-style clock")
                    tip = DateTime.Now.Date.ToLongDateString();
                else
                {
                    if (meter.OnlyValue)
                        tip = data.GetValue(meter.Data, meter.DataSubsource).ToString();
                    else
                        tip = meter.Prefix + data.GetValue(meter.Data, meter.DataSubsource).ToString() + meter.Postfix;
                }

                return tip;
            }
        }

        void meterClick(int meterInd, Point mouse)
        {
            if (meters[meterInd] != null)
                switch (meters[meterInd].ClickAction)
                {
                    case "Open recycle bin":
                        Process.Start("shell:RecycleBinFolder");
                        break;
                    case "Open control panel":
                        Process.Start("control");
                        break;
                    case "Open task manager":
                        Process.Start("taskmgr");
                        break;
                    case "Open mobility center":
                        WinExec(@"C:\Windows\System32\control.exe /name Microsoft.MobilityCenter", SW_NORMAL);
                        break;
                    case "Open power options":
                        WinExec(@"C:\Windows\System32\control.exe /name Microsoft.PowerOptions", SW_NORMAL);
                        break;
                    case "Open date and time options":
                        WinExec(@"C:\Windows\System32\control.exe /name Microsoft.DateAndTime", SW_NORMAL);
                        break;
                    case "Open volume mixer":
                        WinExec("sndvol.exe", SW_NORMAL);
                        break;
                    case "Popup volume control":
                        WinExec("sndvol.exe -f", SW_NORMAL);
                        break;
                    case "Popup WLAN connections":
                        WinExec("rundll32 van.dll,RunVAN", SW_NORMAL);
                        break;
                    case "Change system volume (Vertical meter)":
                        audioDevice.AudioEndpointVolume.MasterVolumeLevelScalar = Math.Min(Math.Max((float)(meters[meterInd].H - mouse.Y) / meters[meterInd].H, 0), 1);
                        break;
                    case "Change system volume (Horizontal meter)":
                        int nextLeft;
                        if (meterInd < meters.Count - 1)
                            nextLeft = meters[meterInd + 1].Left - meters[meterInd + 1].LeftMargin;
                        else
                            nextLeft = this.Width;

                        audioDevice.AudioEndpointVolume.MasterVolumeLevelScalar = Math.Min(Math.Max((float)(mouse.X - meters[meterInd].Left) / (nextLeft - meters[meterInd].Left), 0), 1);
                        break;
                    default:
                        if (meters[meterInd].ClickAction.Contains("Launch program/file...") || meters[meterInd].ClickAction.Contains("Launch web page..."))
                            Process.Start(meters[meterInd].ClickAction.Substring(meters[meterInd].ClickAction.IndexOf("...") + 3));
                        break;
                }
        }

        void meterScroll(Meter meter, int delta)
        {
            if (meter != null && meter.MouseWheelAction == "Change system volume")
                audioDevice.AudioEndpointVolume.MasterVolumeLevelScalar = Math.Min(Math.Max(audioDevice.AudioEndpointVolume.MasterVolumeLevelScalar + (float)delta / 24 / 100, 0), 1);
        }


        public formSysMana()
        {
            InitializeComponent();
        }

        protected override CreateParams CreateParams
        {
            //hide form from alt tab
            get
            {
                // Turn on WS_EX_TOOLWINDOW style bit
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= 0x80;
                return cp;
            }
        }

        private void formSysMeters_Load(object sender, EventArgs e)
        {
            this.SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);

            MMDeviceEnumerator DevEnum = new MMDeviceEnumerator();
            audioDevice = DevEnum.GetDefaultAudioEndpoint(EDataFlow.eRender, ERole.eMultimedia);

            //check for updates
            bool[] askPermissions = new bool[3] { true, true, true };
            for (int i = 0; i < updateNotifs; i++)
                askPermissions[i] = false;

            Updater.Update(VERSION, "https://raw2.github.com/Winterstark/SysMana/master/update/update.txt", askPermissions, showChangelog);
        }

        private void formSysMana_Activated(object sender, EventArgs e)
        {
            if (!initialized)
            {
                loadOptions();
                loadMeters();
                initDataSources();

                initialized = true;
            }
        }

        private void formSysMana_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                Meter meter = GetSelectedMeter(e.X - this.Left);

                if (meter != null && meter.DragFileAction != "")
                    e.Effect = DragDropEffects.All;
                else
                    e.Effect = DragDropEffects.None;
            }
            else
                e.Effect = DragDropEffects.None;
        }

        private void formSysMana_DragDrop(object sender, DragEventArgs e)
        {
            Meter meter = GetSelectedMeter(e.X - this.Left);

            if (meter != null)
                foreach (string path in (string[])e.Data.GetData(DataFormats.FileDrop))
                    switch (meter.DragFileAction)
                    {
                        case "Run":
                            Process.Start(path);
                            break;
                        case "Send to Recycle Bin":
                            if (Directory.Exists(path))
                                Microsoft.VisualBasic.FileIO.FileSystem.DeleteDirectory(path, Microsoft.VisualBasic.FileIO.UIOption.AllDialogs, Microsoft.VisualBasic.FileIO.RecycleOption.SendToRecycleBin, Microsoft.VisualBasic.FileIO.UICancelOption.DoNothing);
                            else
                                Microsoft.VisualBasic.FileIO.FileSystem.DeleteFile(path, Microsoft.VisualBasic.FileIO.UIOption.AllDialogs, Microsoft.VisualBasic.FileIO.RecycleOption.SendToRecycleBin, Microsoft.VisualBasic.FileIO.UICancelOption.DoNothing);
                            break;
                        default:
                            if (meter.DragFileAction.Contains("Copy to directory..."))
                            {
                                string dest = meter.DragFileAction.Substring(meter.DragFileAction.IndexOf("...") + 3);
                                if (dest[dest.Length - 1] != '\\')
                                    dest += "\\";

                                if (Directory.Exists(path))
                                {
                                    //source is dir
                                    dest += Path.GetFileName(path);
                                    if (!Directory.Exists(dest))
                                        Directory.CreateDirectory(dest);

                                    Microsoft.VisualBasic.FileIO.FileSystem.CopyDirectory(path, dest, Microsoft.VisualBasic.FileIO.UIOption.AllDialogs, Microsoft.VisualBasic.FileIO.UICancelOption.DoNothing);
                                }
                                else
                                    //source is file
                                    Microsoft.VisualBasic.FileIO.FileSystem.CopyFile(path, dest + Path.GetFileName(path), Microsoft.VisualBasic.FileIO.UIOption.AllDialogs, Microsoft.VisualBasic.FileIO.UICancelOption.DoNothing);
                            }
                            else if (meter.DragFileAction.Contains("Move to directory..."))
                            {
                                string dest = meter.DragFileAction.Substring(meter.DragFileAction.IndexOf("...") + 3);
                                if (dest[dest.Length - 1] != '\\')
                                    dest += "\\";

                                if (Directory.Exists(path))
                                {
                                    //source is dir
                                    dest += Path.GetFileName(path);
                                    if (!Directory.Exists(dest))
                                        Directory.CreateDirectory(dest);

                                    Microsoft.VisualBasic.FileIO.FileSystem.MoveDirectory(path, dest, Microsoft.VisualBasic.FileIO.UIOption.AllDialogs, Microsoft.VisualBasic.FileIO.UICancelOption.DoNothing);
                                }
                                else
                                    //source is file
                                    Microsoft.VisualBasic.FileIO.FileSystem.MoveFile(path, dest + Path.GetFileName(path), Microsoft.VisualBasic.FileIO.UIOption.AllDialogs, Microsoft.VisualBasic.FileIO.UICancelOption.DoNothing);
                            }
                            break;
                    }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            ImageAnimator.UpdateFrames();

            int left = 0, h = 0;

            if (meters.Count == 0)
            {
                e.Graphics.DrawString("Right-click here and select Setup to add meters.", font, Brushes.Black, 0, 0);

                SizeF txtSize = e.Graphics.MeasureString("Right-click here and select Setup to add meters.", font);
                left = (int)txtSize.Width;
                h = (int)txtSize.Height;
            }
            else
                foreach (Meter meter in meters)
                    meter.Draw(e.Graphics, font, fixedH, align, ref left, ref h);

            this.Width = Math.Max(10, left);

            if (fixedH > 0)
                this.Height = fixedH;
            else
                this.Height = Math.Max(10, h);
        }

        private void OnFrameChanged(object o, EventArgs e)
        {
            //this.Invalidate(); //Force a call to the Paint event handler. 
        }

        private void formSysMana_MouseEnter(object sender, EventArgs e)
        {
            if (menuOnTop.Checked)
                //simulate middle mouse click so the window gets focus
                mouse_event(MOUSEEVENTF_MIDDLEDOWN | MOUSEEVENTF_MIDDLEUP, (uint)System.Windows.Forms.Cursor.Position.X, (uint)System.Windows.Forms.Cursor.Position.Y, 0, 0);
        }

        private void formSysMeters_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == System.Windows.Forms.MouseButtons.Left)
            {
                prevX = e.X;
                prevY = e.Y;
                prevPos = this.Location;

                mousedown = true;
            }
        }

        private void formSysMeters_MouseMove(object sender, MouseEventArgs e)
        {
            if (mousedown)
            {
                this.Left += e.X - prevX;
                this.Top += e.Y - prevY;
            }
            else
            {
                Meter meter = GetSelectedMeter(e.X);

                string newTip = getTooltip(meter);
                if (tipInfo.GetToolTip(this) != newTip)
                    tipInfo.SetToolTip(this, newTip);

                if (meter != null && meter.Data == "Dota-style clock")
                {
                    meter.ClockMouseover = true;
                    clockMeter = meter;
                }
                else if (clockMeter != null)
                {
                    clockMeter.ClockMouseover = false;
                    clockMeter = null;
                }
            }
        }

        private void formSysMeters_MouseUp(object sender, MouseEventArgs e)
        {
            mousedown = false;

            if (e.Button == System.Windows.Forms.MouseButtons.Left)
            {
                if (this.Location != prevPos)
                    saveOptions();
                else
                {
                    //meter click event
                    Meter selMeter = GetSelectedMeter(e.X);

                    if (selMeter != null)
                        for (int i = 0; i < meters.Count; i++)
                            if (meters[i] == selMeter)
                            {
                                meterClick(i, e.Location);
                                break;
                            }
                }
            }
        }

        private void formSysMeters_MouseWheel(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            meterScroll(GetSelectedMeter(e.X), e.Delta);
        }

        private void formSysMana_MouseLeave(object sender, EventArgs e)
        {
            if (clockMeter != null)
            {
                clockMeter.ClockMouseover = false;
                clockMeter = null;
            }
        }

        private void timerRefresh_Tick(object sender, EventArgs e)
        {
            this.Invalidate();
        }

        private void timerEnsureTopMost_Tick(object sender, EventArgs e)
        {
            if (!(setup == null || setup.IsDisposed))
                return; //options window and this.TopMost can interact negatively

            if (menuOnTop.Checked           //don't bring to front when user deselected that option
                && !fullScreenActive())     //also when user is in a full screen application
                this.TopMost = true;
            else 
            {
                this.TopMost = false;
                SetWindowPos(this.Handle, HWND_BOTTOM, 0, 0, 0, 0, SWP_NOSIZE | SWP_NOMOVE);
            }
        }

        private void timerUpdateData_Tick(object sender, EventArgs e)
        {
            foreach (Meter meter in meters)
                meter.CurrDataValue = data.GetValue(meter.Data, meter.DataSubsource);
        }

        private void menuSetup_Click(object sender, EventArgs e)
        {
            if (setup == null || setup.IsDisposed)
            {
                setup = new formSetup();
                setup.Init(meters, timerUpdateData.Interval, (int)this.Opacity, fixedH, this.BackColor, align, this.TopMost, this.AllowTransparency, font, data, loadMeters, loadOptions, initDataSources, LoadImg, DisposeImg, updateNotifs, showChangelog);
                setup.Show();
            }
        }

        private void menuOnTop_Click(object sender, EventArgs e)
        {
            if (menuOnTop.Checked)
            {
                menuOnTop.Checked = false;
                this.TopMost = false;
            }
            else
            {
                menuOnTop.Checked = true;
                this.TopMost = true;
            }

            saveOptions();
        }

        private void menuExit_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }
    }
}
