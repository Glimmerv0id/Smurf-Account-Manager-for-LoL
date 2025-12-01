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
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [DllImport("user32.dll")]
        private static extern bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern IntPtr LoadKeyboardLayout(string pwszKLID, uint Flags);

        [DllImport("user32.dll")]
        private static extern IntPtr ActivateKeyboardLayout(IntPtr hkl, uint Flags);

        [DllImport("user32.dll")]
        private static extern short GetKeyState(int nVirtKey);

        [DllImport("user32.dll")]
        private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

        private const uint WM_INPUTLANGCHANGEREQUEST = 0x0050;
        private const uint KLF_ACTIVATE = 1;
        private const int VK_CAPITAL = 0x14;
        private const uint KEYEVENTF_KEYUP = 0x02;

        private static bool IsCapsLockOn()
        {
            return (GetKeyState(VK_CAPITAL) & 0x0001) != 0;
        }

        private static void ToggleCapsLock()
        {
            keybd_event(VK_CAPITAL, 0x45, 0, UIntPtr.Zero);           // Press
            keybd_event(VK_CAPITAL, 0x45, KEYEVENTF_KEYUP, UIntPtr.Zero); // Release
        }

        private static string EscapeForSendKeys(string text)
        {
            if (string.IsNullOrEmpty(text))
                return text;
            
            // SendKeys special characters that need escaping: + ^ % ~ ( ) { } [ ]
            return text
                .Replace("{", "{{")      // { → {{
                .Replace("}", "}}")      // } → }}
                .Replace("+", "{+}")     // + → {+}
                .Replace("^", "{^}")     // ^ → {^}
                .Replace("%", "{%}")     // % → {%}
                .Replace("~", "{~}")     // ~ → {~}
                .Replace("(", "{(}")     // ( → {(}
                .Replace(")", "{)}")     // ) → {)}
                .Replace("[", "{[}")     // [ → {[}
                .Replace("]", "{]}");    // ] → {]}
        }

        private static IntPtr GetCurrentKeyboardLayout()
        {
            // 0 = current thread (gets system's active keyboard)
            return GetKeyboardLayout(0);
        }

        private static void SwitchKeyboardLayout(IntPtr windowHandle, IntPtr keyboardLayout)
        {
            try
            {
                if (keyboardLayout == IntPtr.Zero)
                    return;
                
                // Activate the layout first
                ActivateKeyboardLayout(keyboardLayout, KLF_ACTIVATE);
                
                // Also post message to the window to ensure it switches
                PostMessage(windowHandle, WM_INPUTLANGCHANGEREQUEST, IntPtr.Zero, keyboardLayout);
            }
            catch
            {
                // If switching fails, continue anyway
            }
        }

        private static void SwitchToEnglishKeyboard(IntPtr windowHandle)
        {
            try
            {
                // US English keyboard layout identifier
                string usEnglishLayout = "00000409";
                
                // Load and activate US English keyboard layout
                IntPtr hkl = LoadKeyboardLayout(usEnglishLayout, KLF_ACTIVATE);
                
                // Switch to it
                SwitchKeyboardLayout(windowHandle, hkl);
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
                        
                        // Store original keyboard layout
                        IntPtr originalKeyboardLayout = GetCurrentKeyboardLayout();
                        
                        // Handle Caps Lock - turn off if on, restore after typing
                        bool capsWasOn = IsCapsLockOn();
                        if (capsWasOn)
                            ToggleCapsLock();
                        
                        // Bring window to foreground
                        SetForegroundWindow(riotWindow);
                        await Task.Delay(500);

                        // Switch to English keyboard to prevent Chinese input
                        SwitchToEnglishKeyboard(riotWindow);
                        await Task.Delay(300);

                        // Send username (escaped for special characters)
                        SendKeys.SendWait(EscapeForSendKeys(username));
                        await Task.Delay(300);

                        // Tab to password field
                        SendKeys.SendWait("{TAB}");
                        await Task.Delay(300);

                        // Send password (escaped for special characters)
                        SendKeys.SendWait(EscapeForSendKeys(password));
                        await Task.Delay(300);

                        // Press Enter to login
                        SendKeys.SendWait("{ENTER}");
                        
                        // Restore keyboard layout to original
                        await Task.Delay(300);
                        SwitchKeyboardLayout(riotWindow, originalKeyboardLayout);
                        
                        // Restore Caps Lock state if it was on
                        if (capsWasOn)
                        {
                            await Task.Delay(500);
                            ToggleCapsLock();
                        }
                        
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
