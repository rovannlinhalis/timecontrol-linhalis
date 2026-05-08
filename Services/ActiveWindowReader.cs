using System;
using System.Diagnostics;
using System.Text;
using System.Windows.Forms;
using TimeControl.Agent.WinForms.Models;
using TimeControl.Agent.WinForms.Native;

namespace TimeControl.Agent.WinForms.Services
{
    internal sealed class ActiveWindowReader
    {
        public MonitoringEvent Read()
        {
            uint processId;
            var handle = NativeMethods.GetForegroundWindow();
            NativeMethods.GetWindowThreadProcessId(handle, out processId);

            if (processId == 0)
            {
                return null;
            }

            var process = Process.GetProcessById((int)processId);
            return new MonitoringEvent
            {
                Pid = processId.ToString(),
                EventDate = DateTime.Now,
                Process = process.ProcessName,
                Title = ReadTitle(handle),
                Computer = Environment.MachineName,
                UserName = Environment.UserName,
                MouseX = Cursor.Position.X,
                MouseY = Cursor.Position.Y
            };
        }

        private static string ReadTitle(IntPtr handle)
        {
            const int maxChars = 256;
            var buffer = new StringBuilder(maxChars);
            return NativeMethods.GetWindowText(handle, buffer, maxChars) > 0 ? buffer.ToString() : string.Empty;
        }
    }
}
