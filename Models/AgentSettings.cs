namespace TimeControl.Agent.WinForms.Models
{
    internal sealed class AgentSettings
    {
        public AgentSettings()
        {
            ApiUrl = "http://localhost:5173";
            Token = string.Empty;
            StartWithWindows = true;
        }

        public string ApiUrl { get; set; }
        public string Token { get; set; }
        public bool StartWithWindows { get; set; }
    }
}
