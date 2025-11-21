using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SmurfAccountManager.Services
{
    public class LoginService
    {
        [DllImport("user32.dll")]
        private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern IntPtr GetKeyboardLayout(uint idThread);

        [DllImport("user32.dll")]
        private static extern bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern IntPtr LoadKeyboardLayout(string pwszKLID, uint Flags);

        private const uint WM_INPUTLANGCHANGEREQUEST = 0x0050;
        private const uint KLF_ACTIVATE = 1;

        private static void SwitchToEnglishKeyboard(IntPtr windowHandle)
        {
            try
            {
                // US English keyboard layout identifier
                string usEnglishLayout = "00000409";
                
                // Load and activate US English keyboard layout
                IntPtr hkl = LoadKeyboardLayout(usEnglishLayout, KLF_ACTIVATE);
                
                // Post message to window to change input language
                PostMessage(windowHandle, WM_INPUTLANGCHANGEREQUEST, IntPtr.Zero, hkl);
            }
            catch
            {
                // If switching fails, continue anyway
            }
        }

        public static async Task<bool> AutoLogin(string username, string password, string riotGamesPath, string leagueClientLogsPath)
        {
            try
            {
                // Kill existing Riot processes
                ProcessService.KillRiotProcesses();
                await Task.Delay(1000);

                // NOW record log state AFTER killing old client but BEFORE starting new one
                // This ensures we only track data from the NEW login
                RiotLogDetectionService.RecordLogStateBeforeLogin(leagueClientLogsPath);

                // Start Riot Client
                var started = await ProcessService.StartRiotClient(riotGamesPath);
                if (!started)
                {
                    return false;
                }

                // Wait for login window to appear (up to 30 seconds)
                for (int i = 0; i < 30; i++)
                {
                    await Task.Delay(1000);
                    
                    // Try to find Riot Client window
                    IntPtr riotWindow = FindWindow(null, "Riot Client");
                    if (riotWindow == IntPtr.Zero)
                        riotWindow = FindWindow(null, "League of Legends");
                    
                    if (riotWindow != IntPtr.Zero)
                    {
                        // Give it extra time for login fields to load
                        await Task.Delay(2000);
                        
                        // Bring window to foreground
                        SetForegroundWindow(riotWindow);
                        await Task.Delay(500);

                        // Switch to English keyboard to prevent Chinese input
                        SwitchToEnglishKeyboard(riotWindow);
                        await Task.Delay(300);

                        // Send username
                        SendKeys.SendWait(username);
                        await Task.Delay(300);

                        // Tab to password field
                        SendKeys.SendWait("{TAB}");
                        await Task.Delay(300);

                        // Send password
                        SendKeys.SendWait(password);
                        await Task.Delay(300);

                        // Press Enter to login
                        SendKeys.SendWait("{ENTER}");
                        
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
    }
}
