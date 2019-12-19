using System;
using System.Diagnostics;
using System.Reflection;

namespace CrmWebApiProxy
{
    public class Log
    {
        public static void Logo()
        {
            Console.Clear();
            Write($"Dynamics Crm Common Data Service Proxy", ConsoleColor.Yellow);
            Write($"Ensure app registration is created in Azure portal for common data service and appsettings is updated accordingly", ConsoleColor.White);
        }

        public static void Info(string msg)
        {
            Write(msg, ConsoleColor.Magenta);
        }

        public static void Error(string msg)
        {
            Write(msg, ConsoleColor.Red);
        }

        public static void Msg(string msg)
        {
            Write(msg, ConsoleColor.Cyan);
        }

        public static void Write(string msg, ConsoleColor color = ConsoleColor.White)
        {
            Console.ForegroundColor = color;
            Console.WriteLine(msg);
            Console.ForegroundColor = ConsoleColor.Gray;
        }
    }
}