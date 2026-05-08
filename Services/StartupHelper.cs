using System;
using Microsoft.Win32;

namespace TimeControl.Agent.WinForms.Services
{
    internal static class StartupHelper
    {
        private const string RegistryName = "TimeControl";

        public static void SetEnabled(bool enabled)
        {
            using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true))
            {
                if (key == null)
                {
                    return;
                }

                if (enabled)
                {
                    key.SetValue(RegistryName, "\"" + System.Windows.Forms.Application.ExecutablePath + "\"");
                }
                else
                {
                    key.DeleteValue(RegistryName, false);
                }
            }
        }
    }
}
