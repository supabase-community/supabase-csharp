using System;
using System.IO;
using io.notification;
using Newtonsoft.Json;
using Supabase.Gotrue;
using Supabase.Gotrue.Interfaces;
using static io.notification.NotificationManager.NotificationType;

namespace io.supabase {
    public class UnitySession : IGotrueSessionPersistence<Session> {
        private string FilePath() {
            const string cacheFileName = ".gotrue.cache";
            string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "Dayboard");

            if (!Directory.Exists(path)) {
                Directory.CreateDirectory(path);
            }

            string filePath = Path.Join(path, cacheFileName);
            return filePath;
        }

        public void SaveSession(Session session) {
            if (session == null) {
                DestroySession();
                return;
            }

            try {
                string filePath = FilePath();
                string str = JsonConvert.SerializeObject(session);
                using StreamWriter file = new StreamWriter(filePath);
                file.Write(str);
                file.Dispose();
            } catch (Exception) {
                Console.WriteLine("Unable to write cache file.");
                throw;
            }
        }

        public void DestroySession() {
            string filePath = FilePath();
            if (File.Exists(filePath)) {
                File.Delete(filePath);
            }
        }

        public Session LoadSession() {
            string filePath = FilePath();

            if (!File.Exists(filePath)) return null;

            using StreamReader file = new StreamReader(filePath);
            string sessionJson = file.ReadToEnd();

            if (string.IsNullOrEmpty(sessionJson)) {
                return null;
            }

            try {
                return JsonConvert.DeserializeObject<Session>(sessionJson);
            } catch (Exception e) {
                NotificationManager.PostMessage(Auth, "Unable to load user", e);
                return null;
            }
        }
    }
}
