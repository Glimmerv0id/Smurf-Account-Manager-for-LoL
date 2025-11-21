# Smurf Account Manager for LoL

Version 1.0

A lightweight Windows desktop application designed to make switching between multiple League of Legends accounts fast and easy.

## Features

- **One-Click Login**: Automatically kill and restart Riot Client with selected account credentials
- **Secure Storage**: Passwords encrypted using Windows DPAPI (Data Protection API)
- **Auto-Recording**: Automatically records summoner names and queue penalty timers
- **Queue Penalty Detection**: Displays queue lockout and low priority queue timers
- **Account Management**: Add, edit, delete, and reorder accounts
- **Hover Tooltips**: View account details on hover

## Requirements

- Windows 10/11
- .NET 6.0 or newer
- League of Legends installed (default path: C:\Riot Games)

## Building the Application

### Prerequisites
- Visual Studio 2022 or newer, OR
- .NET 6.0 SDK

### Build with Visual Studio
1. Open `SmurfAccountManager.csproj` in Visual Studio
2. Build > Build Solution (or press Ctrl+Shift+B)
3. Run the application (F5)

### Build with .NET CLI
```bash
cd SmurfAccountManager
dotnet build
dotnet run
```

### Create Release Build
```bash
dotnet publish -c Release -r win-x64 --self-contained false
```

The executable will be in: `bin/Release/net6.0-windows/win-x64/publish/`

## Usage

### First Time Setup
1. Launch the application
2. Click "edit accounts" button
3. Click "paths requirement" if your Riot Games folder is not at `C:\Riot Games`
4. Click "new account" to add your first account
5. Enter username and password
6. Click "Save"

### Logging In
1. Click on any account button in the main window
2. The app will:
   - Close all Riot/League processes
   - Start Riot Client
   - Automatically fill in credentials
   - Attempt to log in

### Viewing Account Details
- Hover over any account button to see:
  - Username
  - Password (masked)
  - In-game name (if recorded)
  - Queue lockout timer (if any)
  - Low priority queue timer (if any)

### Managing Accounts
1. Click "edit accounts" button
2. Options:
   - **New account**: Add a new account
   - **Edit**: Modify account credentials
   - **⋯ (Options)**: Delete account
   - **Paths requirement**: Change Riot Games installation path

## File Locations

### Configuration
- Config file: `%AppData%\SmurfAccountManager\config.json`
- Contains encrypted passwords and account data

### Required Riot Paths (Default)
- Riot Client: `C:\Riot Games\Riot Client\RiotClientServices.exe`
- League Logs: `C:\Riot Games\League of Legends\Logs\LeagueClient Logs\`
- Riot Logs: `%LocalAppData%\Riot Games\Riot Client\Logs\`

## Security

- Passwords are encrypted using Windows DPAPI
- Passwords are never stored in plain text
- No network communication outside Riot Client
- No interaction with game files
- Only automates the login process

## Troubleshooting

### Login Fails
- Verify Riot Games path in "paths requirement"
- Check if Riot Client executable exists
- Ensure no other Riot processes are running
- Try logging in manually first

### Summoner Name Not Recording
- Summoner name is recorded after successful login
- Requires access to League Client logs
- May take a few seconds after login

### Queue Penalties Not Showing
- Penalties are read from Riot Client logs
- May not detect all penalty types
- Updates when hovering over account

## Known Limitations

- Windows only
- Requires .NET 6.0 runtime
- Assumes standard Riot Games installation structure
- Does not support Two-Factor Authentication (2FA) prompts
- Log parsing patterns may need updates after Riot Client changes

## Technical Details

### Technology Stack
- C# / WPF (.NET 6.0)
- Windows Forms (for SendKeys)
- DPAPI for encryption
- JSON for data storage

### Architecture
- **Models**: Account, AppConfig
- **Services**: 
  - EncryptionService (DPAPI)
  - StorageService (JSON)
  - ProcessService (Process management)
  - LoginService (UI automation)
  - LogReaderService (Log parsing)
- **Views**: 
  - MainWindow (account grid)
  - EditAccountsWindow (account management)
  - AccountDialog (add/edit account)
  - PathRequirementDialog (set paths)
  - TooltipWindow (hover details)

## License

This is a personal utility tool. Use at your own risk.

## Disclaimer

This application only interacts with the Riot Client login interface and log files. It does not modify game files or interact with gameplay. Use responsibly and in accordance with Riot Games' Terms of Service.
