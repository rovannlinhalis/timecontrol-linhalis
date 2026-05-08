using System;

namespace TimeControl.Agent.WinForms.Models
{
    internal sealed class ActivityEventDto
    {
        public DateTime EventDateUtc { get; set; }
        public string Title { get; set; }
        public string Process { get; set; }
        public string Station { get; set; }
        public string OsUsername { get; set; }
        public int PointerX { get; set; }
        public int PointerY { get; set; }
    }
}
