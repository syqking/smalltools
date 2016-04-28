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

// ���ܣ�
// 1.�����޸�ϵͳ��ǰʱ��
// 2.����http://open.baidu.com/special/time/��ȡ��ȷʱ�䲢����Ϊ��ǰϵͳʱ��

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
                ShowLog("ʱ��ָ��ɹ�");
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
                ShowLog("ʧ�ܣ�{0}", msg);
                this.BeginInvoke(new NotifyGetTimeResultDelegate(NotifyGetTimeResult), false, null);
            }

        }

        private void button2_Click(object sender, EventArgs e)
        {
            ShowLog("����������ȡʱ��...");
            ThreadPool.QueueUserWorkItem(fetchInternetTime);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            try
            {
                SetSystemTime(dateTimePicker1.Value);
                ShowLog("ʱ�����óɹ�");
            }
            catch (Exception ex)
            {
                ShowLog("ʱ������ʧ�ܣ�{0}", ex.Message);
            }
        }

        private static bool SetNetworkAdapter(bool status)
        {
            const string discVerb = "ͣ��(&B)"; // "ͣ��(&B)";      
            const string connVerb = "����(&A)"; // "����(&A)";      
            const string network = "��������"; //"��������";      
            const string networkConnection = "��̫�������� ��̫��"; // "��������"      
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
                //���������������ѡ��      
                foreach (Shell32.FolderItem myItem in folder.Items())
                {
                    //������������      
                    if (myItem.Name == network)
                    {
                        Shell32.Folder fd = (Shell32.Folder)myItem.GetFolder;
                        foreach (Shell32.FolderItem fi in fd.Items())
                        {
                            //�ҵ���������      
                            if ((fi.Name == networkConnection))
                            {
                                //�ұ������ӵ������Ҽ����ܲ˵�      
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