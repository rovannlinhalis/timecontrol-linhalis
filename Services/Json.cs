using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Web.Script.Serialization;
using TimeControl.Agent.WinForms.Models;

namespace TimeControl.Agent.WinForms.Services
{
    internal static class Json
    {
        public static string SerializeSettings(AgentSettings settings)
        {
            return "{" +
                   "\"apiUrl\":\"" + Escape(settings.ApiUrl) + "\"," +
                   "\"token\":\"" + Escape(settings.Token) + "\"," +
                   "\"startWithWindows\":" + (settings.StartWithWindows ? "true" : "false") +
                   "}";
        }

        public static AgentSettings DeserializeSettings(string json)
        {
            var settings = new AgentSettings();
            if (string.IsNullOrWhiteSpace(json))
            {
                return settings;
            }

            var serializer = new JavaScriptSerializer();
            var data = serializer.Deserialize<Dictionary<string, object>>(json);
            if (data == null)
            {
                return settings;
            }

            settings.ApiUrl = ReadString(data, "apiUrl", settings.ApiUrl);
            settings.Token = ReadString(data, "token", settings.Token);
            settings.StartWithWindows = ReadBool(data, "startWithWindows", settings.StartWithWindows);
            return settings;
        }

        public static DateTime? ReadLastEventDate(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return null;
            }

            var serializer = new JavaScriptSerializer();
            var data = serializer.Deserialize<Dictionary<string, object>>(json);
            if (data == null || !data.ContainsKey("eventDate") || data["eventDate"] == null)
            {
                return null;
            }

            var value = data["eventDate"].ToString();
            DateTime parsed;
            if (DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out parsed))
            {
                return parsed.ToUniversalTime();
            }

            return null;
        }

        public static string SerializeEvents(IEnumerable<ActivityEventDto> events)
        {
            var builder = new StringBuilder();
            builder.Append("[");
            var first = true;
            foreach (var item in events)
            {
                if (!first)
                {
                    builder.Append(",");
                }

                first = false;
                builder.Append("{");
                AppendProperty(builder, "eventDate", item.EventDateUtc.ToString("o", CultureInfo.InvariantCulture));
                builder.Append(",");
                AppendProperty(builder, "title", Limit(item.Title, 500));
                builder.Append(",");
                AppendProperty(builder, "process", Limit(item.Process, 260));
                builder.Append(",");
                AppendProperty(builder, "station", Limit(item.Station, 260));
                builder.Append(",");
                AppendProperty(builder, "osUsername", Limit(item.OsUsername, 260));
                builder.Append(",\"pointerX\":").Append(item.PointerX.ToString(CultureInfo.InvariantCulture));
                builder.Append(",\"pointerY\":").Append(item.PointerY.ToString(CultureInfo.InvariantCulture));
                builder.Append(",\"tags\":[]");
                builder.Append("}");
            }

            builder.Append("]");
            return builder.ToString();
        }

        private static void AppendProperty(StringBuilder builder, string name, string value)
        {
            builder.Append("\"").Append(name).Append("\":\"").Append(Escape(value)).Append("\"");
        }

        private static string Escape(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            return value
                .Replace("\\", "\\\\")
                .Replace("\"", "\\\"")
                .Replace("\r", "\\r")
                .Replace("\n", "\\n")
                .Replace("\t", "\\t");
        }

        private static string Limit(string value, int maxLength)
        {
            value = string.IsNullOrWhiteSpace(value) ? string.Empty : value;
            return value.Length <= maxLength ? value : value.Substring(0, maxLength);
        }

        private static string ReadString(Dictionary<string, object> data, string key, string fallback)
        {
            return data.ContainsKey(key) && data[key] != null ? data[key].ToString() : fallback;
        }

        private static bool ReadBool(Dictionary<string, object> data, string key, bool fallback)
        {
            if (!data.ContainsKey(key) || data[key] == null)
            {
                return fallback;
            }

            bool value;
            return bool.TryParse(data[key].ToString(), out value) ? value : fallback;
        }
    }
}
