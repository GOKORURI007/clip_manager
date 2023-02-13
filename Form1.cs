using System;
using System.Collections.Generic;
using System.Drawing;
using System.Resources;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace clip_manager
{
    public partial class Form1 : Form
    {
        //private ResourceManager icons = new ResourceManager(typeof(Icons));
        private readonly ResourceManager configFile = new ResourceManager(typeof(Config));
        private readonly Icon workingIcon = Icons.working;
        private readonly Icon stopIcon = Icons.stop;
        private bool isWork = true;
        private readonly Thread clipThread;
        private static readonly ManualResetEvent mre = new ManualResetEvent(false);

        public void ClipboardManager(Object obj)
        {
            Dictionary<string, string[]> target_dict = obj as Dictionary<string, string[]>;
            IDataObject clipManager;
            string recentClip = null;
            string currentClip;
            string tmpClip;
            char[] trimCH = { '\r', '\n', ' ' };
            IntPtr currentWindow;
            int textLength;
            StringBuilder windowName;
            StringBuilder windowClass = new StringBuilder(255);
            string s_windowName;
            string s_windowClass;
            Boolean isOverWrite = false;
            while (true)
            {
                if (isWork)
                {
                    clipManager = Clipboard.GetDataObject();
                    currentClip = (string)clipManager.GetData(typeof(string));
                    if (clipManager.GetDataPresent(typeof(string)) && currentClip != null && (currentClip != recentClip))
                    {
                        currentWindow = GetForegroundWindow();
                        textLength = GetWindowTextLength(currentWindow);
                        windowName = new StringBuilder(textLength + 1);
                        GetWindowText(currentWindow, windowName, textLength);
                        GetClassName(currentWindow, windowClass, 254);
                        s_windowName = windowName.ToString();
                        s_windowClass = windowClass.ToString();
                        //Console.WriteLine("before " + currentClip);
                        if (!isOverWrite)
                        {
                            foreach (var title in target_dict["title"])
                            {
                                if (s_windowName.Contains(title))
                                {
                                    isOverWrite = true;
                                    break;
                                }
                            }
                        }
                        if (!isOverWrite)
                        {
                            foreach (var classname in target_dict["classname"])
                            {
                                if (s_windowClass.Contains(classname))
                                {
                                    isOverWrite = true;
                                    break;
                                }
                            }
                        }
                        if (isOverWrite)
                        {
                            tmpClip = Regex.Replace(currentClip.Trim(trimCH), "( +)|((\r\n)+)", " ");
                            currentClip = tmpClip;
                        }
                        //Console.WriteLine("after " + currentClip);
                        Clipboard.SetDataObject(currentClip, true);
                        recentClip = currentClip;
                        isOverWrite = false;
                    }
                    Thread.Sleep(200);
                }
                else
                {
                    mre.WaitOne();
                    mre.Reset();
                }

            }
        }

        public Form1()
        {
            InitializeComponent();
            // 控件初始化
            notifyIcon1.Icon = workingIcon;
            // 读取config
            string t_classname = configFile.GetString("target_classname").Replace("\r", "");
            string t_title = configFile.GetString("target_title").Replace("\r", "");
            string[] list_classname = t_classname.Split('\n');
            string[] list_title = t_title.Split('\n');
            Dictionary<string, string[]> target_dict = new Dictionary<string, string[]>
            {
                { "classname", list_classname },
                { "title", list_title }
            };
            // 线程初始化
            ParameterizedThreadStart parameterized = new ParameterizedThreadStart(ClipboardManager);
            clipThread = new Thread(parameterized);
            clipThread.SetApartmentState(ApartmentState.STA);
            clipThread.Start(target_dict);
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            Hide();
        }

        private void WorkToolStripMenuItem_Click(object sender, EventArgs e)
        {
            notifyIcon1.Icon = workingIcon;
            isWork = true;
            mre.Set();
        }

        private void StopToolStripMenuItem_Click(object sender, EventArgs e)
        {
            notifyIcon1.Icon = stopIcon;
            isWork = false;
        }

        private void ExitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            clipThread.Abort();
            Close();
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto, ExactSpelling = true)]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

        [DllImport("user32.dll")]
        public static extern int GetWindowTextLength(IntPtr hWnd);

        [DllImport("User32.dll", EntryPoint = "GetWindowText")]
        private static extern int GetWindowText(IntPtr hwnd, StringBuilder lpString, int nMaxCount);
    }
}
