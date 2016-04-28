using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Threading;

namespace Pcopy
{
    public class pcopy
    {
        class CopyTask
        {
            public string[] files;
            public string targetDir;
        }

        StreamWriter mLogWriter = new StreamWriter("pcopy.log");

        private void Log(string fmt, params object[] args)
        {
            string msg = string.Format(fmt, args);
            lock (this)
            {
                Console.WriteLine(msg);
            }
        }

        private void LogFile(string fmt, params object[] args)
        {
            string msg = string.Format(fmt, args);
            lock (this)
            {
                mLogWriter.WriteLine(msg);
            }
        }

        private void DoCopyTask(object state)
        {
            CopyTask copyTask = state as CopyTask;
            foreach (string f in copyTask.files)
            {
                string targetPath = copyTask.targetDir + "\\" + Path.GetFileName(f);
                FileInfo sourceInfo = new FileInfo(f);
                FileInfo targetInfo = null;

                if (File.Exists(targetPath))
                    targetInfo = new FileInfo(targetPath);

                try
                {
                    if (targetInfo == null || targetInfo.LastWriteTime < sourceInfo.LastWriteTime)
                    {
                        File.Copy(f , targetPath, true);
                        Log(string.Format("{0} Copyed.", f));
                    }
                    else
                    {
                        // Skip..
                    }
                }
                catch (Exception ex)
                {
                    LogFile("Copy from {0} to {1} fail : {2}\r\n {3}", f, targetPath, ex.Message, ex.StackTrace);
                }
            }
        }

        private void CopyFiles(string filter, string fromdir, string todir)
        {
            string[] files = Directory.GetFiles(fromdir, filter);
            CopyTask task = new CopyTask();
            task.files = files;
            task.targetDir = todir;

            // 等待有空余线程
            while (GetAvilableThreadNum() == 0)
                Thread.Sleep(10);

            ThreadPool.QueueUserWorkItem(new WaitCallback(DoCopyTask), task);

            foreach (string dir in Directory.GetDirectories(fromdir))
                CopyFiles(filter, dir, todir);
        }

        private int GetAvilableThreadNum()
        {
            try
            {
                int workerThreads = 0;
                int compleThreads = 0;
                ThreadPool.GetAvailableThreads(out workerThreads, out compleThreads);
                return workerThreads;
            }
            catch (Exception ex)
            {
                LogFile("GetAvilableThreadNum fail : {0}\r\n{1}", ex.Message, ex.StackTrace);
                return 0;
            }
        }

        public void Do(string filter, string fromdir, string todir)
        {
            if (!Directory.Exists(todir))
                Directory.CreateDirectory(todir);

            CopyFiles(filter, fromdir, todir);
            Thread.Sleep(1000);

            while (true)
            {
                int workerThreads = 0;
                int maxWordThreads = 0;

                //int 
                int compleThreads = 0;
                ThreadPool.GetAvailableThreads(out workerThreads, out compleThreads);
                ThreadPool.GetMaxThreads(out maxWordThreads, out compleThreads);

                if (workerThreads == maxWordThreads)
                {
                    Log("**** All Copyed. ****");
                    break;
                }
            }

            mLogWriter.Close();
        }
    }
}
