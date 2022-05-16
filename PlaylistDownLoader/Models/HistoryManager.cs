using System;
using System.Collections.Generic;
using System.IO;

namespace PlaylistDownLoader.Models
{
    public static class HistoryManager
    {
        /// <summary>
        /// 直接いじるならToUpperするように。
        /// </summary>
        public static HashSet<string> HistoryHashs { get; } = new HashSet<string>();
        public static string HistoryTextPath { get; } = Path.Combine(Environment.CurrentDirectory, "UserData", $"{Plugin.Name}.txt");

        static HistoryManager()
        {
            Read();
        }

        public static void Add(string hash)
        {
            if (string.IsNullOrEmpty(hash)) {
                return;
            }
            HistoryHashs.Add(hash.ToUpper());
        }

        public static void Remove(string hash)
        {
            if (string.IsNullOrEmpty(hash)) {
                return;
            }
            HistoryHashs.Remove(hash.ToUpper());
        }

        public static bool Contains(string hash)
        {
            if (string.IsNullOrEmpty(hash)) {
                return false;
            }
            return HistoryHashs.Contains(hash.ToUpper());
        }

        public static void Read()
        {
            try {
                if (!File.Exists(HistoryTextPath)) {
                    using (var _ = File.Create(HistoryTextPath)) {

                    }
                }
            }
            catch (Exception e) {
                Logger.Error(e);
            }
            foreach (var hash in File.ReadAllLines(HistoryTextPath)) {
                HistoryHashs.Add(hash.ToUpper());
            }
        }

        public static void Save()
        {
            try {
                File.WriteAllLines(HistoryTextPath, HistoryHashs);
            }
            catch (Exception e) {
                Logger.Error(e);
            }
        }
    }
}
