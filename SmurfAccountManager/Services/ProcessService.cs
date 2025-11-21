using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace SmurfAccountManager.Services
{
    public class ProcessService
    {
        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll")]
        private static extern bool IsWindowVisible(IntPtr hWnd);
        private static readonly string[] RiotProcessNames = new[]
        {
            "RiotClientServices",
            "RiotClientCrashHandler",
            "RiotClientUx",
            "RiotClientUxRender",
            "LeagueClient",
            "LeagueClientUx",
            "LeagueClientUxRender",
            "LeagueCrashHandler"
        };

        public static void KillRiotProcesses()
        {
            foreach (var processName in RiotProcessNames)
            {
                try
                {
                    var processes = Process.GetProcessesByName(processName);
                    foreach (var process in processes)
                    {
                        try
                        {
                            process.Kill();
                            process.WaitForExit(5000);
                        }
                        catch
                        {
                            // Continue if we can't kill a specific process
                        }
                    }
                }
                catch
                {
                    // Continue even if we can't get processes
                }
            }
        }

        /// <summary>
        /// Checks if League Client main window is visible
        /// </summary>
        public static bool IsLeagueClientWindowVisible()
        {
            try
            {
                // Try multiple possible window titles
                string[] possibleTitles = new[]
                {
                    "League of Legends",
                    "League Client",
                    "Riot Client"
                };

                foreach (var title in possibleTitles)
                {
                    IntPtr hwnd = FindWindow(null, title);
                    if (hwnd != IntPtr.Zero && IsWindowVisible(hwnd))
                    {
                        return true;
                    }
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Waits for League Client main window to appear (with timeout)
        /// </summary>
        public static async Task<bool> WaitForLeagueClientWindow(int timeoutSeconds = 30)
        {
            var startTime = DateTime.Now;
            
            while ((DateTime.Now - startTime).TotalSeconds < timeoutSeconds)
            {
                if (IsLeagueClientWindowVisible())
                {
                    return true;
                }
                
                await Task.Delay(1000); // Check every second
            }
            
            return false;
        }

        public static async Task<bool> StartRiotClient(string riotGamesPath)
        {
            try
            {
                var riotClientPath = Path.Combine(riotGamesPath, "Riot Client", "RiotClientServices.exe");
                
                if (!File.Exists(riotClientPath))
                {
                    return false;
                }

                var startInfo = new ProcessStartInfo
                {
                    FileName = riotClientPath,
                    Arguments = "--launch-product=league_of_legends --launch-patchline=live",
                    UseShellExecute = true
                };

                Process.Start(startInfo);
                await Task.Delay(2000); // Give it time to start
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static bool IsRiotClientRunning()
        {
            return Process.GetProcessesByName("RiotClientServices").Any();
        }
    }
}
