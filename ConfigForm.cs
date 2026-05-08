using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using TimeControl.Agent.WinForms.Models;
using TimeControl.Agent.WinForms.Services;

namespace TimeControl.Agent.WinForms
{
    internal sealed class ConfigForm : Form
    {
        private readonly ApiClient _apiClient;
        private readonly TextBox _apiUrlTextBox;
        private readonly TextBox _tokenTextBox;
        private readonly CheckBox _startupCheckBox;
        private readonly Label _statusLabel;
        private readonly Button _testButton;
        private System.ComponentModel.IContainer components;
        private readonly Button _saveButton;

        public ConfigForm(AgentSettings settings, ApiClient apiClient)
        {
            _apiClient = apiClient;
            Settings = new AgentSettings
            {
                ApiUrl = settings.ApiUrl,
                Token = settings.Token,
                StartWithWindows = settings.StartWithWindows
            };

            Text = "TimeControl";
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ClientSize = new Size(460, 250);
            Font = new Font("Segoe UI", 9F);

            var title = new Label
            {
                Text = "Configuracao do agente",
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                Location = new Point(18, 16),
                AutoSize = true
            };

            var apiLabel = new Label { Text = "Dominio da API", Location = new Point(20, 58), AutoSize = true };
            _apiUrlTextBox = new TextBox { Location = new Point(20, 78), Width = 420, Text = Settings.ApiUrl };

            var tokenLabel = new Label { Text = "Token", Location = new Point(20, 110), AutoSize = true };
            _tokenTextBox = new TextBox { Location = new Point(20, 130), Width = 420, Text = Settings.Token, UseSystemPasswordChar = true };

            _startupCheckBox = new CheckBox
            {
                Text = "Iniciar com o Windows",
                Location = new Point(20, 162),
                Width = 220,
                Checked = Settings.StartWithWindows
            };

            _statusLabel = new Label
            {
                Text = "Informe apenas o dominio da API e o token do perfil.",
                Location = new Point(20, 190),
                Width = 420,
                Height = 20
            };

            _testButton = new Button { Text = "Testar", Location = new Point(196, 215), Width = 110 };
            _saveButton = new Button { Text = "Salvar", Location = new Point(320, 215), Width = 120, DialogResult = DialogResult.None };

            _testButton.Click += async delegate { await TestAsync(); };
            _saveButton.Click += delegate { SaveAndClose(); };

            Controls.Add(title);
            Controls.Add(apiLabel);
            Controls.Add(_apiUrlTextBox);
            Controls.Add(tokenLabel);
            Controls.Add(_tokenTextBox);
            Controls.Add(_startupCheckBox);
            Controls.Add(_statusLabel);
            Controls.Add(_testButton);
            Controls.Add(_saveButton);
        }

        public AgentSettings Settings { get; private set; }

        private async Task TestAsync()
        {
            if (!ReadSettings())
            {
                return;
            }

            SetStatus("Testando conexao...", Color.DimGray);
            _testButton.Enabled = false;
            try
            {
                _apiClient.Configure(Settings);
                await _apiClient.TestAsync();
                SetStatus("Conexao OK.", Color.ForestGreen);
            }
            catch (Exception ex)
            {
                SetStatus(ex.Message, Color.Firebrick);
            }
            finally
            {
                _testButton.Enabled = true;
            }
        }

        private void SaveAndClose()
        {
            if (!ReadSettings())
            {
                return;
            }

            DialogResult = DialogResult.OK;
            Close();
        }

        private bool ReadSettings()
        {
            var apiUrl = _apiUrlTextBox.Text.Trim();
            var token = _tokenTextBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(apiUrl) || string.IsNullOrWhiteSpace(token))
            {
                SetStatus("Dominio da API e token sao obrigatorios.", Color.Firebrick);
                return false;
            }

            Settings.ApiUrl = ApiClient.NormalizeApiBaseUrl(apiUrl);
            Settings.Token = token;
            Settings.StartWithWindows = _startupCheckBox.Checked;
            return true;
        }

        private void SetStatus(string text, Color color)
        {
            _statusLabel.Text = text;
            _statusLabel.ForeColor = color;
        }
    }
}
