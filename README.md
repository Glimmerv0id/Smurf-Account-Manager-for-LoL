# Smurf Account Manager for League of Legends

*A Windows application for managing multiple League of Legends accounts with automatic login, account detection, and penalty tracking.*

![Version](https://img.shields.io/badge/version-1.2.0-blue)
![.NET](https://img.shields.io/badge/.NET-10.0-purple)
![Platform](https://img.shields.io/badge/platform-Windows-lightgrey)

## ✨ Features

- 🔐 **Secure Auto-Login** - One-click login for multiple accounts
- 🎮 **Automatic Account Detection** - Detects Riot ID from League Client logs
- 🏷️ **Account Tagging & Organization** (NEW in v1.2)
  - Color-coded tags (⭐ Yellow Star, 🔴 Red Circle, 🟢 Green Circle)
  - Visual colored indicators (not black/white)
  - Drag-and-drop reordering with enhanced visual effects
  - Tags shown in main window and edit accounts screen
  - Export/import support for tags
- ⚠️ **Penalty Tracking** 
  - Low Priority Queue display (permanent tracking)
  - Queue Lockout timer with countdown
  - Visual red card indicator for locked accounts
- 🔒 **Password Encryption** - Secure storage using Windows DPAPI
- 🎨 **Clean UI** - Modern dark theme with drag-to-move window
- 📊 **Multi-Account Support** - Manage unlimited accounts

## 🖼️ Screenshots

*(Add screenshots here after taking some)*

## 📥 Installation

### Option 1: Download Release (Recommended for Users)
1. Go to [Releases](../../releases)
2. Download `SmurfAccountManager-v1.2.zip`
3. Extract and run `SmurfAccountManager.exe`
4. No installation or .NET setup required!

### Option 2: Build from Source (For Developers)
```bash
# Clone the repository
git clone https://github.com/YOUR_USERNAME/SmurfAccountManager-LoL.git
cd SmurfAccountManager-LoL

# Build the project
dotnet build SmurfAccountManager.sln

# Or build release version
dotnet publish SmurfAccountManager/SmurfAccountManager.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

## 🚀 Getting Started

### First Time Setup
1. Launch `SmurfAccountManager.exe`
2. Click **"edit accounts"** button
3. Set up your paths:
   - **Riot Games Path**: `C:\Riot Games` (or your installation path)
   - **League Client Logs**: `C:\Riot Games\League of Legends\Logs\LeagueClient Logs`
   - **Riot Client Logs**: `C:\Users\[YourName]\AppData\Local\Riot Games\Riot Client\Logs`
4. Add your accounts (username + password)

### Usage
1. **Click any account button** to auto-login
2. **Hover over account** to see:
   - Riot ID (GameName#TagLine)
   - Low priority queue status
   - Queue lockout countdown
3. **Red cards** indicate accounts with active queue lockout

## 📋 Requirements

- Windows 10/11 (x64)
- League of Legends installed
- No .NET installation required (self-contained)

## 🔧 Configuration

Config file location: `%AppData%\SmurfAccountManager\config.json`

**Note:** Passwords are encrypted per-user and per-machine. If you copy the app to another computer, you'll need to re-enter passwords.

## 🛡️ Security & Privacy

- **Password Encryption**: Uses Windows DPAPI (Data Protection API)
- **Local Storage**: All data stored locally on your machine
- **No Telemetry**: No data collection or external connections
- **Open Source**: Full source code available for review

### Verifying the Release

If you want to verify the release matches the source code:

```bash
# Clone and build yourself
git clone https://github.com/YOUR_USERNAME/SmurfAccountManager-LoL.git
cd SmurfAccountManager-LoL
dotnet publish SmurfAccountManager/SmurfAccountManager.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true

# Your build should match the released version
```

## 📖 Version History

### v1.2.0 (2025-12-01)
- ✅ **Account Tagging System**
  - Color-coded tags (Yellow Star, Red Circle, Green Circle)
  - Visual colored shape indicators (gold, red, green)
  - Tag selection menu with colored icons
  - Tags persist in config and export/import
- ✅ **Drag-and-Drop Reordering**
  - Reorder accounts by dragging
  - Enhanced visual feedback (blue glow, background change, drop shadow)
  - Automatic display order updates
- ✅ **UI Improvements**
  - Tags displayed in main window (to the right of account names)
  - Tags displayed in edit accounts window (left of account names)
  - Proper centering for accounts without tags
  - Reorganized edit accounts layout (edit/tag/delete buttons)

### v1.1.0 (2025-11-21)
- ✅ Automatic account detection from League Client logs
- ✅ Low priority queue tracking (permanent display)
- ✅ Queue lockout timer with countdown
- ✅ Red card visual indicator for locked accounts
- ✅ 15-second detection delay for improved accuracy
- ✅ Enhanced multi-account support
- ✅ Robust penalty persistence across app restarts

### v1.0.0 (Initial Release)
- Basic auto-login functionality
- Multi-account management
- Secure password storage

## 🐛 Troubleshooting

### Account Detection Fails
- Make sure League Client is fully loaded (wait 15 seconds after login)
- Verify log paths are set correctly in "edit accounts"
- Check if log files exist in the specified directories

### Penalties Don't Update
- Close and restart the app
- Ensure you've logged in at least once with the account
- Check that Riot Client logs path is correct

### App Won't Start
- Run as administrator if needed
- Check Windows Defender hasn't quarantined the file
- Make sure no antivirus is blocking execution

## 🤝 Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

1. Fork the repository
2. Create your feature branch (`git checkout -b feature/AmazingFeature`)
3. Commit your changes (`git commit -m 'Add some AmazingFeature'`)
4. Push to the branch (`git push origin feature/AmazingFeature`)
5. Open a Pull Request

## 📝 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## ⚠️ Disclaimer

This is a third-party application and is not affiliated with, endorsed by, or connected to Riot Games. Use at your own risk. The developer is not responsible for any consequences resulting from the use of this application.

## 🙏 Acknowledgments

- Built with .NET 10 and WPF
- Uses Newtonsoft.Json for configuration management
- Inspired by the need for efficient smurf account management

## 📧 Support

If you encounter any issues or have suggestions:
- Open an [Issue](../../issues)
- Check existing issues for solutions
- Read the troubleshooting section above

---

**Made with ❤️ for the League of Legends community**
