using System;
using System.IO;
using TimeControl.Agent.WinForms.Models;

namespace TimeControl.Agent.WinForms.Services
{
    internal sealed class SettingsStore
    {
        private readonly string _file;

        public SettingsStore()
        {
            var directory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "TimeControl");
            _file = Path.Combine(directory, "settings.json");
        }

        public AgentSettings Load()
        {
            try
            {
                if (File.Exists(_file))
                {
                    return Json.DeserializeSettings(File.ReadAllText(_file));
                }
            }
            catch
            {
            }

            return new AgentSettings();
        }

        public void Save(AgentSettings settings)
        {
            var directory = Path.GetDirectoryName(_file);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(_file, Json.SerializeSettings(settings));
        }
    }
}
