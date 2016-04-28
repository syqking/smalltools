using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Threading;
using System.Management;
using System.Diagnostics;

namespace AutoUpdate
{
    // 功能：到指定网络路径获取版本信息，并自动更新有改变的文件
    //      * 约定网络路径和本地路径中都要有一个Version文件，用于存放版本信息，当本地版本低于网络版本时进行自动更新
    //      * Version文件中的内容为版本号，以'.'分隔，如：1.0.15.2
    //      * 需要配置autoupdate.ini，例子见工程目录下的同名示例ini文件
    //        1.第一行是Version所在位置
    //        2.最后一行是启动exe，更新完后自动运行之
    //        3.其他行是网络路径和本地路径的映射
    //      * 需要使用此工具进行自动更新的程序要自己访问网络路径，获取Version中版本号并进行对比

    public partial class Form1 : Form
    {
        private string m_CurVersion = "0.0.0.0";
        private string m_NewVersion = "0.0.0.0";

        private string m_UpdateLocation = "";
        private string m_StartProcess = "GameEditor.exe";
        // private string m_UserName;
        // private string m_Password;
        private StreamWriter m_log;
        private Thread m_updateThread = null;
        private List<KeyValuePair<string, string>> m_UpdateLocations = new List<KeyValuePair<string, string>>();
        delegate void Delegate0();

        public Form1()
        {
            InitializeComponent();
            buttonClose.Visible = false;
        }

        private void OnLoad(object sender, EventArgs e)
        {
            // 在线程中进行自动更新
            m_updateThread = new Thread(new ThreadStart(StartAutoUpdate));
            m_updateThread.Start();
        }

        private void ShowCloseButton()
        {
            this.BeginInvoke(new Delegate0(delegate() { buttonClose.Show(); }));
        }

        private void StartAutoUpdate()
        {
            m_log = new StreamWriter("autoupdate.log", false);

            try
            {
                // 读取更新源和自动启动的进程
                StreamReader updateSourceReader = new System.IO.StreamReader("./autoupdate.ini");
                string content = updateSourceReader.ReadToEnd();
                string[] lines = content.Split('\n');
                for (int i = 0; i < lines.Length; ++i)
                {
                    string line = lines[i].TrimEnd();
                    if (i == 0)
                    {
                        // 第一行是version所在位置
                        m_UpdateLocation = line;
                    }
                    else if (i == lines.Length - 1)
                    {
                        // 最后一行是启动exe
                        m_StartProcess = line;
                    }
                    else if (i < lines.Length - 1 && line.Contains("|"))
                    {
                        // 其他行是网络路径和本地路径的映射
                        string[] parts = line.Split('|');
                        if (parts.Length == 2)
                            m_UpdateLocations.Add(new KeyValuePair<string, string>(parts[0], parts[1]));
                    }
                }

                updateSourceReader.Close();
            }
            catch (Exception ex)
            {
                ShowMessage("autoupdate.ini文件异常，无法自动更新");
                m_log.WriteLine("autoupdate.ini文件异常:{0}", ex.Message);
                ShowCloseButton();
            }

            if (m_UpdateLocations.Count == 0)
                m_UpdateLocations.Add(new KeyValuePair<string, string>(m_UpdateLocation, ".\\"));

            try
            {
                // 读取version文件
                StreamReader nativeVersionReader = new System.IO.StreamReader("./Version");
                m_CurVersion = nativeVersionReader.ReadLine();
                nativeVersionReader.Close();
            }
            catch
            {
                // 没读到就算了
            }

            if (IsNewAvilable(m_CurVersion))
            {
                // 更新文件
                UpdateFiles();
            }
            else
            {
                Thread.Sleep(500);
                try
                {
                    // 直接打开
                    System.Diagnostics.Process.Start(m_StartProcess, "1");
                    this.BeginInvoke(new Delegate0(Close));
                }
                catch
                {
                    ShowMessage("打开{0}失败", m_StartProcess);
                    ShowCloseButton();
                }
            }

            // 关闭log
            m_log.Close();
        }

        /// <summary>
        /// 显示信息
        /// </summary>
        private void ShowMessage(string fmt, params object[] args)
        {
            string msg = string.Format(fmt, args);
            this.Invoke(new Delegate0(
                delegate
                {
                    m_Label.Text = msg;
                }
            ));
        }

        // 拷贝整个目录
        void CopyDirectory(string srcDir, string tgtDir)
        {
            DirectoryInfo source = new DirectoryInfo(srcDir);
            DirectoryInfo target = new DirectoryInfo(tgtDir);

            if (!source.Exists)
            {
                return;
            }

            if (!target.Exists)
            {
                target.Create();
            }

            FileInfo[] files = source.GetFiles();
            FileInfo versionFile = null;
            FileInfo selfFile = null;

            foreach (FileInfo f in files)
            {
                try
                {
                    // 忽略一些文件
                    if (f.Name == "Thumbs.db")
                        continue;

                    // Version最后拷贝
                    if (f.Name == "Version")
                    {
                        versionFile = f;
                        continue;
                    }

                    FileInfo localFileInfo = new FileInfo(target.FullName + @"\" + f.Name);
                    if (! localFileInfo.Exists || localFileInfo.LastWriteTime < f.LastWriteTime)
                    {
                        if (f.Name == "AutoUpdate.exe")
                        {
                            // 自身需要更新，新缓存下来
                            selfFile = f;
                        }
                        else
                        {
                            // 更新文件
                            File.Copy(f.FullName, target.FullName + @"\" + f.Name, true);
                            m_log.WriteLine("Update {0} ...", f.Name);
                            ShowMessage("Copy file " + f.Name + "...");
                        }
                    }
                    else
                    {
                        m_log.WriteLine("No need upate {0} ...", f.Name);
                    }
                }
                catch
                {
                    throw new Exception(string.Format("下载文件{0}失败，请查看是否被占用", f.Name));
                }
            }

            DirectoryInfo[] dirs = source.GetDirectories();

            for (int j = 0; j < dirs.Length; j++)
            {
                CopyDirectory(dirs[j].FullName, target.FullName + @"\" + dirs[j].Name);
            }

            // 最后拷贝version，避免更新中断
            if (versionFile != null)
                File.Copy(versionFile.FullName, target.FullName + @"\" + versionFile.Name, true);

            // 更新自身
            if (selfFile != null)
            {
                m_log.WriteLine("Self need update, start UpdateLauncher.exe");
                File.Copy(selfFile.FullName, target.FullName + @"\" + "AutoUpdateNew.exe", true);

                // 启动UpdateLauncher来更新自身
                ProcessStartInfo startInfo = new ProcessStartInfo();
                startInfo.CreateNoWindow = true;
                startInfo.WindowStyle = ProcessWindowStyle.Hidden;
                startInfo.FileName = "UpdateLauncher.exe";
                Process.Start(startInfo);
            }
        }

        /// <summary>
        /// 判断是否有新版本可以获取
        /// </summary>
        /// <returns>如果有，那么返回true；否则返回false</returns>
        public bool IsNewAvilable(string curVersion)
        {
            try
            {
                {
                    // 读取服务器上的版本号
                    System.IO.StreamReader sr = new System.IO.StreamReader(m_UpdateLocation + "\\version");
                    string version = sr.ReadLine();

                    m_NewVersion = version;

                    // 比较两个版本号
                    string[] newVersionInfo = version.Split('.');
                    string[] curVersionInfo = curVersion.Split('.');

                    m_log.WriteLine("服务端版本号 {0}", version);
                    m_log.WriteLine("本地版本号 {0}", curVersion);

                    // 如果不是 x.y.z.m
                    if (newVersionInfo.Length != 4 || curVersionInfo.Length != 4)
                        return false;

                    // 比较
                    for (int i = 0; i < 4; i++)
                    {
                        int a = Int32.Parse(newVersionInfo[i]);
                        int b = Int32.Parse(curVersionInfo[i]);

                        // 如果相同，那么继续比较下一个
                        if (a == b)
                            continue;

                        // 否则这次比较就能分出新旧了
                        if (a > b)
                            return true;
                        else
                            return false;
                    }
                }

                // 版本完全相同，没有必要更新
                return false;
            }
            catch(Exception e)
            {
                m_log.WriteLine("检查更新失败：" + e.Message);
                ShowMessage("检查更新失败：" + e.Message);

                // 发生任何异常情况，直接返回false
                return false;
            }
        }

        /// <summary>
        /// 拷贝更新文件
        /// </summary>
        public void UpdateFiles()
        {
            try
            {
                // 尝试用户认证？似乎有问题，以后再说
                // System.Net.NetworkCredential credential = new System.Net.NetworkCredential(m_UserName, m_Password);
                // using (new NetworkConnection(m_UpdateLocation, credential))
                foreach (KeyValuePair<string, string> p in m_UpdateLocations)
                {
                    // 拷贝整个目录过来
                    CopyDirectory(p.Key, p.Value);
                    System.Diagnostics.Process.Start(m_StartProcess, "1");
                    this.BeginInvoke(new Delegate0(Close));
                }
            }
            catch (Exception e)
            {
                ShowMessage("更新失败：" + e.Message);
                ShowCloseButton();
            }
        }

        private void OnClose(object sender, FormClosedEventArgs e)
        {
            if (m_updateThread != null)
            {
                m_updateThread.Abort();
                m_updateThread = null;
            }

            m_log.Close();
            m_log = null;
        }

        private void OnTick(object sender, EventArgs e)
        {
            pictureBox1.Invalidate();
        }

        private void OnPaint(object sender, PaintEventArgs e)
        {
            Image img = Properties.Resources.loading;
            float step = 4.0f;
            m_rotate += step * m_rotateDir;

            if (m_rotateDir == 1 && m_rotate > 60.0f)
                m_rotateDir = -1;
            else if (m_rotateDir == -1 && m_rotate < 0.0f)
                m_rotateDir = 1;

            e.Graphics.ResetTransform();
            e.Graphics.TranslateTransform(img.Width / 2, img.Height / 2);
            e.Graphics.RotateTransform(m_rotate);
            e.Graphics.TranslateTransform(-img.Width / 2, -img.Height / 2);
            e.Graphics.DrawImage(Properties.Resources.loading, 0, 0, img.Width, img.Height);
        }

        private float m_rotateDir = 1.0f;
        private float m_rotate = 0.0f;

        private void buttonClose_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}