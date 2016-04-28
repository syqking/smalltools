using System;
using System.Collections.Generic;
using System.Text;

namespace Pcopy
{
    class Program
    {
        static void ShowHelp()
        {
            Console.WriteLine("Pcopy V1.0");
            Console.WriteLine("Created by shenyq Nov/22/2013");
            Console.WriteLine("\n���ܣ���������̣߳��ݹ����ĳ��Ŀ¼�������������ļ���������һ��Ŀ¼�����Ҳ�����Ŀ¼�ṹ����Ϊ��ƽ�ṹ");
            Console.WriteLine("\n�÷�:");
            Console.WriteLine("pcopy filter from_dir target_dir");
        }

        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                ShowHelp();
                return;
            }

            try
            {
                new pcopy().Do(args[0], args[1], args[2]);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: {0}", ex.Message);
                Console.WriteLine("StackTrace: {0}", ex.StackTrace);
            }
        }
    }
}
