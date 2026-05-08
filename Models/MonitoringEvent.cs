using System;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace TimeControl.Agent.WinForms.Models
{
    internal sealed class MonitoringEvent
    {
        private const string HashSalt = "rovannlinhalis";

        public MonitoringEvent()
        {
            IsValid = true;
        }

        public MonitoringEvent(string line)
        {
            IsValid = false;
            if (string.IsNullOrEmpty(line))
            {
                return;
            }

            try
            {
                var fields = line.Split(';');
                if (fields.Length < 9)
                {
                    return;
                }

                Pid = fields[0];
                EventDate = DateTime.Parse(fields[1], CultureInfo.CurrentCulture);
                Process = fields[2];
                Title = fields[3];
                Computer = fields[4];
                UserName = fields.Length > 5 ? fields[5] : string.Empty;
                MouseX = ParseInt(fields, 6);
                MouseY = ParseInt(fields, 7);

                var expectedHash = fields[fields.Length - 1];
                IsValid = string.Equals(expectedHash, Md5(ToPayload() + HashSalt), StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                IsValid = false;
            }
        }

        public string Pid { get; set; }
        public string Process { get; set; }
        public string Title { get; set; }
        public DateTime EventDate { get; set; }
        public string Computer { get; set; }
        public string UserName { get; set; }
        public int MouseX { get; set; }
        public int MouseY { get; set; }
        public bool IsValid { get; private set; }

        public override string ToString()
        {
            var payload = ToPayload();
            return payload + ";" + Md5(payload + HashSalt);
        }

        private static int ParseInt(string[] fields, int index)
        {
            int value;
            return fields.Length > index && int.TryParse(fields[index], out value) ? value : 0;
        }

        private string ToPayload()
        {
            return Clean(Pid) + ";" +
                   EventDate.ToString(CultureInfo.CurrentCulture) + ";" +
                   Clean(Process) + ";" +
                   Clean(Title) + ";" +
                   Clean(Computer) + ";" +
                   Clean(UserName) + ";" +
                   MouseX + ";" +
                   MouseY + ";" +
                   ";" +
                   ";" +
                   ";";
        }

        private static string Clean(string value)
        {
            return string.IsNullOrEmpty(value) ? string.Empty : value.Replace(";", string.Empty);
        }

        private static string Md5(string input)
        {
            using (var md5 = MD5.Create())
            {
                var bytes = Encoding.UTF8.GetBytes(input ?? string.Empty);
                var hash = md5.ComputeHash(bytes);
                var builder = new StringBuilder(hash.Length * 2);
                for (var i = 0; i < hash.Length; i++)
                {
                    builder.Append(hash[i].ToString("x2"));
                }

                return builder.ToString();
            }
        }
    }
}
