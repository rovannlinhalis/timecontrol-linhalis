using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using TimeControl.Agent.WinForms.Models;

namespace TimeControl.Agent.WinForms.Services
{
    internal sealed class EventStore
    {
        private readonly string _directory;
        private readonly List<MonitoringEvent> _events;

        public EventStore(string directory)
        {
            _directory = directory;
            _events = new List<MonitoringEvent>();
        }

        public int Count
        {
            get { return _events.Count; }
        }

        public void Load()
        {
            Directory.CreateDirectory(_directory);
            if (LoadLegacyCurrentFile())
            {
                return;
            }

            LoadDailyFiles();
        }

        public void AddRange(IEnumerable<MonitoringEvent> events)
        {
            var changedDates = new HashSet<DateTime>();
            foreach (var item in events)
            {
                if (item == null || !item.IsValid)
                {
                    continue;
                }

                _events.Add(item);
                changedDates.Add(item.EventDate.Date);
            }

            foreach (var date in changedDates)
            {
                SaveDate(date);
            }
        }

        public List<MonitoringEvent> NewerThan(DateTime? lastServerDateUtc)
        {
            IEnumerable<MonitoringEvent> query = _events.Where(x => x.IsValid);
            if (lastServerDateUtc.HasValue)
            {
                var localCutoff = lastServerDateUtc.Value.ToLocalTime();
                query = query.Where(x => x.EventDate > localCutoff);
            }

            return query.OrderBy(x => x.EventDate).ToList();
        }

        private bool LoadLegacyCurrentFile()
        {
            var file = Path.Combine(_directory, "appTimeControlData.atc");
            if (!File.Exists(file))
            {
                return false;
            }

            foreach (var item in ReadFile(file))
            {
                _events.Add(item);
            }

            File.Copy(file, Path.Combine(_directory, "appTimeControlData.bkp"), true);
            File.Delete(file);

            foreach (var date in _events.Select(x => x.EventDate.Date).Distinct())
            {
                SaveDate(date);
            }

            return true;
        }

        private void LoadDailyFiles()
        {
            foreach (var file in Directory.GetFiles(_directory, "*.atc", SearchOption.TopDirectoryOnly))
            {
                var info = new FileInfo(file);
                if (DateTime.Now - info.LastWriteTime > TimeSpan.FromDays(90))
                {
                    TryDelete(info.FullName);
                    continue;
                }

                foreach (var item in ReadFile(info.FullName))
                {
                    _events.Add(item);
                }
            }
        }

        private IEnumerable<MonitoringEvent> ReadFile(string file)
        {
            string[] lines;
            try
            {
                lines = File.ReadAllLines(file, Encoding.Default);
            }
            catch
            {
                yield break;
            }

            foreach (var line in lines)
            {
                var item = new MonitoringEvent(line);
                if (item.IsValid)
                {
                    yield return item;
                }
            }
        }

        private void SaveDate(DateTime date)
        {
            var file = Path.Combine(_directory, date.ToString("yyyyMMdd", CultureInfo.InvariantCulture) + ".atc");
            using (var writer = new StreamWriter(file, false, Encoding.Default))
            {
                foreach (var item in _events.Where(x => x.EventDate.Date == date).OrderBy(x => x.EventDate))
                {
                    writer.WriteLine(item.ToString());
                }
            }
        }

        private static void TryDelete(string file)
        {
            try
            {
                File.Delete(file);
            }
            catch
            {
            }
        }
    }
}
