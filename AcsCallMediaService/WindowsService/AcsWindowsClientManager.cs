using System.Diagnostics;

namespace WindowsService
{
    internal class AcsWindowsClientManager
    {
        public async Task LaunchApp()
        {
            Process.Start(new ProcessStartInfo()
            {
                FileName = "explorer.exe",
                Arguments = "shell:AppsFolder\\AcsWindowsClient_c5y7km29dgsrm!App"
            });

            // todo wait until app has sent message instead of delay (need to do a continuous health monitoring with pings or so)
            await Task.Delay(4000);
        }
    }
}
