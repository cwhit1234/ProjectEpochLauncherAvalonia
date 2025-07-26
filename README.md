# Project Epoch Avalonia Launcher

A cross-platform launcher for Project Epoch, a World of Warcraft private server that brings back the classic experience with modern improvements.

![ProjectEpochAvaloniaLauncherHome](https://github.com/user-attachments/assets/d889017e-3082-41b5-a97c-8ef025edc5bb)

## Features

- **Cross-Platform Support**: Works on Windows, macOS, and Linux
- **Easy Setup**: First-time setup wizard guides you through installation
- **Multi-CDN Support**: Downloads from multiple CDN providers for reliability
- **Modern UI**: Clean, responsive interface built with Avalonia UI

## Download and Installation

### Windows

1. **Download the Latest Release**
   - Go to the [Releases page](https://github.com/cwhit1234/ProjectEpochLauncherAvalonia/releases)
   - Download `project-epoch-launcher-windows-vX.X.X.zip`

2. **Install**
   ```
   1. Extract the ZIP file to your desired location (e.g., C:\Program Files\Project Epoch Launcher\)
   2. Run ProjectEpochLauncherAvalonia.exe
   3. Follow the setup wizard to complete installation
   ```

3. **Create Desktop Shortcut (Optional)**
   - Right-click on `ProjectEpochLauncherAvalonia.exe`
   - Select "Create shortcut"
   - Move the shortcut to your desktop

### macOS

1. **Download the Latest Release**
   - Go to the [Releases page](https://github.com/Project-Epoch/ProjectEpochLauncherAvalonia/releases)
   - Download `project-epoch-launcher-macos-vX.X.X.zip`

2. **Install**
   ```
   1. Extract the ZIP file to your Applications folder
   2. Launch the app from Applications
   3. If prompted about security, go to System Preferences > Security & Privacy and click "Open Anyway"
   4. Follow the setup wizard to complete installation
   ```

**Note**: Game launching is not supported on macOS, but you can use the launcher to download and manage game files.

### Linux

1. **Download the Latest Release**
   - Go to the [Releases page](https://github.com/Project-Epoch/ProjectEpochLauncherAvalonia/releases)
   - Download `project-epoch-launcher-linux-vX.X.X.zip`

2. **Install**
   ```bash
   1. Extract the archive   
   2. Move to desired location
   3. Make executable
   4. Run the launcher
   ```

**Note**: Game launching is not supported on Linux, but you can use the launcher to download and manage game files.

## First Time Setup

When you run the launcher for the first time, you'll be guided through a setup wizard:

1. **Welcome Screen**: Introduction to Project Epoch
2. **Installation Path**: Choose where to install the game files
3. **Download**: The launcher will download all necessary game files
4. **Complete**: Setup is finished and you're ready to play!

### Default Installation Paths

- **Windows**: `C:\Program Files\Project Epoch\`
- **macOS**: `/Applications/Project Epoch/`
- **Linux**: `~/Games/ProjectEpoch/`

## Usage

### Main Features

- **Play Button**: Launch the game (Windows only)
- **Update Check**: Manually check for game updates
- **Settings**: Configure launcher preferences
- **Discord**: Join the Project Epoch community
- **Donate**: Support the Project Epoch development

### Navigation

- **Home**: Main launcher interface
- **Settings**: Configure installation path and other preferences

## Troubleshooting

### Common Issues

**Launcher won't start**
- Ensure you have the latest .NET runtime installed
- Try running as administrator (Windows) or with sudo (Linux)
- Check antivirus software isn't blocking the launcher

**Download fails or is slow**
- Check your internet connection
- Try restarting the download
- The launcher uses multiple CDNs and will automatically retry failed downloads
- Run the launcher as administrator if it fails to install at `C:\Program Files\Project Epoch\`

**Game won't launch (Windows)**
- Verify game files are downloaded completely
- Check that Project-Epoch.exe exists in your installation directory
- Run the launcher as administrator

**Setup wizard closes unexpectedly**
- Ensure you have write permissions to the installation directory

### Getting Help
- **Issues**: Report bugs on our [GitHub Issues page](https://github.com/cwhit1234/ProjectEpochLauncherAvalonia/issues)

## Configuration Files

The launcher stores configuration in platform-specific locations:

- **Windows**: `%LocalAppData%\ProjectEpochLauncherAvalonia\launcher-config.json`
- **macOS**: `~/Library/Application Support/ProjectEpochLauncherAvalonia/launcher-config.json`
- **Linux**: `~/.config/ProjectEpochLauncherAvalonia/launcher-config.json`

## License

This project is licensed under the MIT License - see the [LICENSE.txt](LICENSE.txt) file for details.
