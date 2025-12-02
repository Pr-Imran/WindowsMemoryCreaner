# Simple Safe RAM Cleaner

A lightweight, safe, and easy-to-use utility to free up system RAM on Windows.

## Features
- **Real-time Monitoring:** Shows Total, Used, and Free RAM with a visual progress bar.
- **Safe Cleaning:** Trims the working set of non-critical user processes. Does **NOT** kill processes or touch critical system services.
- **Auto-Clean:** Optionally triggers a clean when memory usage exceeds a specific percentage (e.g., 80%).
- **Logging:** Keeps a short history of cleaning actions.

## How It Works
This application uses the Windows API (`EmptyWorkingSet`) to request that applications release unused memory back to the OS. This memory is then moved to the "Standby" or "Free" list. If the application needs that memory again, Windows will page it back in.

**Safety Note:** The cleaner is programmed to explicitly **IGNORE** critical system processes (like `system`, `svchost`, `csrss`, etc.) to prevent system instability or UI lag.

## Requirements
- Windows 10 or Windows 11 (64-bit recommended)
- .NET Runtime (The project targets .NET 8.0)

## How to Build & Run
1. **Open in Visual Studio:**
   - Double-click `RamCleaner.csproj` or open the folder in Visual Studio 2022.
2. **Build:**
   - Press `Ctrl + Shift + B` to build the solution.
3. **Run:**
   - Press `F5` to start debugging.

## CLI Build
You can also build and run from the command line:
```bash
dotnet build
dotnet run
```

## License
Free and Open Source.
