using System;
using System.IO;

namespace MinecraftHotbar
{
    internal static class Log
    {
        private static readonly string LogFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "hotbar.log");

        public static void Write(string msg)
        {
            try
            {
                File.AppendAllText(LogFile, $"[{DateTime.Now:HH:mm:ss.fff}] {msg}{Environment.NewLine}");
            }
            catch { }
        }
    }
}
