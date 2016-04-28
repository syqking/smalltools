using System;
using System.Collections.Generic;
using System.Text;
using System.Management;
using System.IO;
using System.Security.Cryptography;

// 功能： 根据mac地址、C盘序列号并加密后生成一个序列号
//       其他项目可使用同样的代码生成序列号用于校验是否合法

namespace PCKeyGen
{
    class Program
    {
        [System.Runtime.InteropServices.DllImport("kernel32.dll")]
        private static extern long GetVolumeInformation(string PathName, StringBuilder VolumeNameBuffer, UInt32 VolumeNameSize, ref UInt32 VolumeSerialNumber, ref UInt32 MaximumComponentLength, ref UInt32 FileSystemFlags, StringBuilder FileSystemNameBuffer, UInt32 FileSystemNameSize);

        /// <summary>
        /// 获取加密后的机器特征码
        /// </summary>
        public static string GetEncryptMachinId()
        {
            string ret = "";

            // C盘序列号
            uint serNum = 0;
            uint maxCompLen = 0;
            StringBuilder VolLabel = new StringBuilder(256); // Label
            UInt32 VolFlags = new UInt32();
            StringBuilder FSName = new StringBuilder(256); // File System Name
            long Ret = GetVolumeInformation("C:\\", VolLabel, (UInt32)VolLabel.Capacity, ref serNum, ref maxCompLen, ref VolFlags, FSName, (UInt32)FSName.Capacity);
            ret = Convert.ToString(serNum, 16).ToUpper();

            {
                // 物理网卡Mac地址
                ManagementClass mc;
                ManagementObjectCollection moc;
                mc = new ManagementClass("Win32_NetworkAdapterConfiguration");
                moc = mc.GetInstances();
                string str = "";
                foreach (ManagementObject mo in moc)
                {
                    if ((bool)mo["IPEnabled"] == true)
                    {
                        str = mo["MacAddress"].ToString().Replace(':', '-');
                        ret += str;
                        break;
                    }
                }
            }

            // 加密
            System.Security.Cryptography.MD5CryptoServiceProvider md5 = new System.Security.Cryptography.MD5CryptoServiceProvider();
            string input = ret;
            byte[] hash_byte = md5.ComputeHash(Encoding.Default.GetBytes(ret));
            ret = System.BitConverter.ToString(hash_byte);
            ret = ret.Replace("-", "").ToLower();
            hash_byte = md5.ComputeHash(Encoding.Default.GetBytes(ret));
            ret = System.BitConverter.ToString(hash_byte);
            ret = ret.Replace("-", "").ToLower();
            return ret;
        }

        static void Main(string[] args)
        {
            string key = GetEncryptMachinId();
            StreamWriter sw = new StreamWriter("key.txt", true);
            sw.WriteLine(key);
            Console.WriteLine(key);
            sw.Close();
        }
    }
}
