using System;
using System.Threading;
using System.Windows.Forms;
using TimeControl.Agent.WinForms.Services;

namespace TimeControl.Agent.WinForms
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            bool created;
            using (var mutex = new Mutex(true, "TimeControl.Agent.SingleInstance", out created))
            {
                if (!created)
                {
                    MessageBox.Show("TimeControl ja esta em execucao.", "TimeControl", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new AgentContext());
            }
        }
    }
}
