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
            Console.WriteLine("\n功能：开启多个线程，递归遍历某个目录将满足条件的文件拷贝到另一个目录，并且不保留目录结构，变为扁平结构");
            Console.WriteLine("\n用法:");
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
