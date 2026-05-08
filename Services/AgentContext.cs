using System;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using TimeControl.Agent.WinForms.Models;

namespace TimeControl.Agent.WinForms.Services
{
    internal sealed class AgentContext : ApplicationContext
    {
        private readonly SettingsStore _settingsStore;
        private readonly EventStore _eventStore;
        private readonly EventCollector _collector;
        private readonly ApiClient _apiClient;
        private readonly Icon _trayIconAsset;
        private readonly NotifyIcon _trayIcon;
        private readonly Timer _saveTimer;
        private readonly Timer _syncTimer;
        private AgentSettings _settings;
        private bool _syncing;

        public AgentContext()
        {
            _settingsStore = new SettingsStore();
            _settings = _settingsStore.Load();

            _eventStore = new EventStore(Application.StartupPath);
            _eventStore.Load();

            _apiClient = new ApiClient();
            _apiClient.Configure(_settings);

            _collector = new EventCollector(new ActiveWindowReader());
            _collector.EventsCaptured += OnEventsCaptured;

            _saveTimer = new Timer { Interval = 6000 };
            _saveTimer.Tick += OnSaveTimerTick;

            _syncTimer = new Timer { Interval = 60000 };
            _syncTimer.Tick += async delegate { await SyncAsync(); };

            _trayIconAsset = LoadTrayIcon();
            _trayIcon = new NotifyIcon
            {
                Icon = _trayIconAsset,
                Text = "TimeControl",
                Visible = true,
                ContextMenuStrip = BuildMenu()
            };
            _trayIcon.DoubleClick += delegate { ShowSettings(); };

            StartupHelper.SetEnabled(_settings.StartWithWindows);
            _collector.Start();

            if (IsConfigured())
            {
                _syncTimer.Start();
                var ignored = SyncAsync();
            }
            else
            {
                ShowSettings();
            }

            UpdateTrayText("Coletando eventos.");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _collector.Dispose();
                _apiClient.Dispose();
                _saveTimer.Dispose();
                _syncTimer.Dispose();
                _trayIcon.Dispose();
                _trayIconAsset.Dispose();
            }

            base.Dispose(disposing);
        }

        private static Icon LoadTrayIcon()
        {
            var associatedIcon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
            return associatedIcon ?? (Icon)SystemIcons.Application.Clone();
        }

        private ContextMenuStrip BuildMenu()
        {
            var menu = new ContextMenuStrip();
            menu.Items.Add("Configuracoes", null, delegate { ShowSettings(); });
            menu.Items.Add("Sincronizar agora", null, async delegate { await SyncAsync(); });
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add("Sair", null, delegate { Exit(); });
            return menu;
        }

        private void ShowSettings()
        {
            using (var form = new ConfigForm(_settings, _apiClient))
            {
                if (form.ShowDialog() == DialogResult.OK)
                {
                    _settings = form.Settings;
                    _settingsStore.Save(_settings);
                    _apiClient.Configure(_settings);
                    StartupHelper.SetEnabled(_settings.StartWithWindows);

                    if (IsConfigured())
                    {
                        _syncTimer.Start();
                        var ignored = SyncAsync();
                    }
                    else
                    {
                        _syncTimer.Stop();
                    }
                }
            }
        }

        private void OnEventsCaptured(object sender, EventArgs e)
        {
            if (!_saveTimer.Enabled)
            {
                _saveTimer.Start();
            }
        }

        private void OnSaveTimerTick(object sender, EventArgs e)
        {
            _saveTimer.Stop();
            SavePendingEvents();
        }

        private void SavePendingEvents()
        {
            var pending = _collector.Drain();
            if (pending.Count == 0)
            {
                return;
            }

            _eventStore.AddRange(pending);
            UpdateTrayText("Eventos locais: " + _eventStore.Count + ".");
        }

        private async Task SyncAsync()
        {
            if (_syncing || !IsConfigured())
            {
                return;
            }

            _syncing = true;
            try
            {
                SavePendingEvents();
                var lastDate = await _apiClient.GetLastEventDateAsync();
                var pending = _eventStore.NewerThan(lastDate);
                if (!pending.Any())
                {
                    UpdateTrayText("Sincronizado.");
                    return;
                }

                var sent = await _apiClient.SendAsync(pending);
                UpdateTrayText("Enviados " + sent + " evento(s).");
            }
            catch (UnauthorizedAccessException)
            {
                _syncTimer.Stop();
                UpdateTrayText("Token invalido.");
            }
            catch (Exception ex)
            {
                UpdateTrayText("Erro: " + ex.Message);
            }
            finally
            {
                _syncing = false;
            }
        }

        private bool IsConfigured()
        {
            return !string.IsNullOrWhiteSpace(_settings.ApiUrl) && !string.IsNullOrWhiteSpace(_settings.Token);
        }

        private void UpdateTrayText(string status)
        {
            var text = "TimeControl" + Environment.NewLine + status;
            _trayIcon.Text = text.Length > 63 ? text.Substring(0, 63) : text;
        }

        private void Exit()
        {
            SavePendingEvents();
            _collector.Stop();
            _trayIcon.Visible = false;
            Application.Exit();
        }
    }
}
