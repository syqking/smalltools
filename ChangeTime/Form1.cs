using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Net;
using System.Net.Cache;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using Shell32;

// 功能：
// 1.快速修改系统当前时间
// 2.访问http://open.baidu.com/special/time/获取正确时间并设置为当前系统时间

namespace ChangeTime
{
    public partial class Form1 : Form
    {
        [StructLayout(LayoutKind.Sequential)]
        public struct SYSTEMTIME
        {
            public short wYear;
            public short wMonth;
            public short wDayOfWeek;
            public short wDay;
            public short wHour;
            public short wMinute;
            public short wSecond;
            public short wMilliseconds;
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool SetSystemTime(ref SYSTEMTIME st);

        [DllImport("Kernel32.dll")]
        public static extern void SetLocalTime(ref SYSTEMTIME st);

        private DateTime startTime;

        public Form1()
        {
            InitializeComponent();

            this.dateTimePicker1.CustomFormat = "yyyy-MM-dd HH:mm:ss";
            this.dateTimePicker1.Format = System.Windows.Forms.DateTimePickerFormat.Custom;

            startTime = DateTime.Now;

            SetNetworkAdapter(false);
        }

        public static DateTime GetNistTime()
        {
            DateTime dateTime = DateTime.MinValue;

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create("http://nist.time.gov/actualtime.cgi?lzbc=siqm9b");
            // HttpWebRequest request = (HttpWebRequest)WebRequest.Create("http://www.time.ac.cn/stime.asp");
            request.Method = "GET";
            request.Accept = "text/html, application/xhtml+xml, */*";
            request.UserAgent = "Mozilla/5.0 (compatible; MSIE 10.0; Windows NT 6.1; Trident/6.0)";
            request.ContentType = "application/x-www-form-urlencoded";
            request.CachePolicy = new RequestCachePolicy(RequestCacheLevel.NoCacheNoStore); //No caching
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            if (response.StatusCode == HttpStatusCode.OK)
            {
                StreamReader stream = new StreamReader(response.GetResponseStream());
                string html = stream.ReadToEnd();//<timestamp time=\"1395772696469995\" delay=\"1395772696469995\"/>
                string time = Regex.Match(html, @"(?<=\btime="")[^""]*").Value;
                double milliseconds = Convert.ToInt64(time) / 1000.0;
                dateTime = new DateTime(1970, 1, 1).AddMilliseconds(milliseconds).ToLocalTime();
            }

            return dateTime;
        }

        public static DateTime GetBaiduTime()
        {
            DateTime dateTime = DateTime.MinValue;

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create("http://open.baidu.com/special/time/");
            // HttpWebRequest request = (HttpWebRequest)WebRequest.Create("http://www.time.ac.cn/stime.asp");
            request.Method = "GET";
            request.Accept = "text/html, application/xhtml+xml, */*";
            request.UserAgent = "Mozilla/5.0 (compatible; MSIE 10.0; Windows NT 6.1; Trident/6.0)";
            request.ContentType = "application/x-www-form-urlencoded";
            request.CachePolicy = new RequestCachePolicy(RequestCacheLevel.NoCacheNoStore); //No caching
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            if (response.StatusCode == HttpStatusCode.OK)
            {
                StreamReader stream = new StreamReader(response.GetResponseStream());
                string html = stream.ReadToEnd();//<timestamp time=\"1395772696469995\" delay=\"1395772696469995\"/>
                // string time = Regex.Match(html, @"(?<=\btime="")[^""]*").Value;
                // double milliseconds = Convert.ToInt64(time) / 1000.0;
                // dateTime = new DateTime(1970, 1, 1).AddMilliseconds(milliseconds).ToLocalTime();
                int index1 = html.IndexOf("window.baidu_time(");
                int index2 = html.IndexOf(")", index1);
                string timeStr = html.Substring(index1 + 18, index2 - index1 - 18);
                long milliseconds = Convert.ToInt64(timeStr);
                dateTime = new DateTime(1970, 1, 1).AddMilliseconds(milliseconds).ToLocalTime();
            }

            return dateTime;
        }

        private void SetSystemTime(DateTime time)
        {
            SYSTEMTIME st = new SYSTEMTIME();
            st.wYear = (short)time.Year;
            st.wMonth = (short)time.Month;
            st.wDay = (short)time.Day;
            st.wHour = (short)time.Hour;
            st.wMinute = (short)time.Minute;
            st.wSecond = (short)time.Second;
            SetLocalTime(ref st);
        }

        private void NotifyGetTimeResult(bool res, DateTime time)
        {
            if (res)
            {
                ShowLog("时间恢复成功");
                SetSystemTime(time);
                dateTimePicker1.Value = time;
            }
        }
        private delegate void NotifyGetTimeResultDelegate(bool res, DateTime time);

        private void fetchInternetTime(object state)
        {
            try
            {
                DateTime nowTime = GetBaiduTime();
                this.BeginInvoke(new NotifyGetTimeResultDelegate(NotifyGetTimeResult), true, nowTime);
            }
            catch (Exception ex)
            {
                string msg = ex.Message;
                ShowLog("失败：{0}", msg);
                this.BeginInvoke(new NotifyGetTimeResultDelegate(NotifyGetTimeResult), false, null);
            }

        }

        private void button2_Click(object sender, EventArgs e)
        {
            ShowLog("正在联网获取时间...");
            ThreadPool.QueueUserWorkItem(fetchInternetTime);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            try
            {
                SetSystemTime(dateTimePicker1.Value);
                ShowLog("时间设置成功");
            }
            catch (Exception ex)
            {
                ShowLog("时间设置失败：{0}", ex.Message);
            }
        }

        private static bool SetNetworkAdapter(bool status)
        {
            const string discVerb = "停用(&B)"; // "停用(&B)";      
            const string connVerb = "启用(&A)"; // "启用(&A)";      
            const string network = "网络连接"; //"网络连接";      
            const string networkConnection = "以太网适配器 以太网"; // "本地连接"      
            string sVerb = null;
            if (status)
            {
                sVerb = connVerb;
            }
            else
            {
                sVerb = discVerb;
            }
            Shell32.Shell sh = new Shell32.Shell();
            Shell32.Folder folder = sh.NameSpace(Shell32.ShellSpecialFolderConstants.ssfCONTROLS);
            try
            {
                //进入控制面板的所有选项      
                foreach (Shell32.FolderItem myItem in folder.Items())
                {
                    //进入网络连接      
                    if (myItem.Name == network)
                    {
                        Shell32.Folder fd = (Shell32.Folder)myItem.GetFolder;
                        foreach (Shell32.FolderItem fi in fd.Items())
                        {
                            //找到本地连接      
                            if ((fi.Name == networkConnection))
                            {
                                //找本地连接的所有右键功能菜单      
                                foreach (Shell32.FolderItemVerb Fib in fi.Verbs())
                                {
                                    if (Fib.Name == sVerb)
                                    {
                                        Fib.DoIt();
                                        return true;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return false;
            }
            return true;
        }  

        private void ShowLog(string fmt, params object[] args)
        {
            string msg = string.Format(fmt, args);
            labelLog.Text = msg;
#if false
            this.BeginInvoke(new WaitCallback(delegate(object state)
            {
                // labelLog.Text = msg;
            }));
#endif
        }
    }
}