# Safe RAM Cleaner for Windows

[![GitHub license](https://img.shields.io/github/license/Pr-Imran/WindowsMemoryCreaner)](https://github.com/Pr-Imran/WindowsMemoryCreaner/blob/main/LICENSE)
[![GitHub stars](https://img.shields.io/github/stars/Pr-Imran/WindowsMemoryCreaner)](https://github.com/Pr-Imran/WindowsMemoryCreaner/stargazers)
[![GitHub forks](https://img.shields.io/github/forks/Pr-Imran/WindowsMemoryCreaner)](https://github.com/Pr-Imran/WindowsMemoryCreaner/network/members)

A lightweight, reliable, and safe utility designed to help free up system RAM on Windows 10 and Windows 11 (64-bit). This application prioritizes stability and safety, offering both manual and automatic cleaning options to optimize your memory usage without causing system instability.

## ✨ Features

-   **Real-time Memory Monitoring:** Displays Total, Used, Available, Cached (Standby), and Committed RAM in real-time, along with a visual progress bar indicating memory load.
-   **Safe Manual Cleaning:** A "Clean Memory Now" button triggers an operation to free memory by trimming the working sets of non-critical user processes.
-   **Deep Cleaning (System Cache):** Includes an enhanced cleaning mechanism that flushes the System File Cache (Standby List), further increasing available physical RAM. This operation requires Administrator privileges.
-   **Flexible Auto-Cleaning:**
    -   **Percentage-based:** Automatically cleans when RAM usage exceeds a user-defined percentage (0-100%).
    -   **Time-based:** Automatically cleans at a user-specified interval (0-60 minutes).
-   **System Tray Notifications:** Provides discrete balloon tip notifications from the system tray after a cleaning operation, showing how much memory was freed.
-   **Run on Windows Startup:** An optional setting to automatically launch the application when Windows starts, utilizing the Task Scheduler for reliable execution with Administrator privileges.
-   **Internal Event Log:** Maintains a simple in-app log of all cleaning activities, including time, trigger, and amount freed.
-   **Clean and Intuitive UI:** A simple, single-window interface designed for ease of use.

## 🧠 How It Works & Safety Guarantees

This application leverages documented Windows APIs to manage system memory safely and effectively.

1.  **Memory Monitoring:**
    *   Uses `GlobalMemoryStatusEx` for basic physical memory statistics.
    *   Uses `GetPerformanceInfo` to retrieve detailed metrics like System Cache (Standby List) and Commit Charge.
2.  **Memory Cleaning Strategy:**
    *   **Process Working Set Trimming (`EmptyWorkingSet`):** For most running applications, the cleaner requests Windows to `EmptyWorkingSet`. This moves memory pages that are not actively in use by a process from physical RAM to the Standby List (cache) or the page file on disk. This action immediately increases "Available RAM".
    *   **System File Cache Flushing (`SetSystemFileCacheSize`):** To achieve a "deeper" clean and address the often misunderstood "Cached RAM" (Standby List), the application attempts to flush the system's file cache. This is done by temporarily setting a minimal limit on the system cache size, which forces Windows to release cached data, and then resetting it to system-managed. This method ensures that even "cached" memory becomes available for other applications.
3.  **Safety First:**
    *   **Documented APIs Only:** All memory management operations use documented Windows APIs (e.g., `EmptyWorkingSet`, `SetSystemFileCacheSize`). No undocumented "hacks" or internal syscalls are used, ensuring compatibility and stability.
    *   **Critical Process Protection:** The cleaner employs a strict blacklist to **never** attempt to touch or modify critical Windows system processes (like `system`, `svchost`, `lsass`, Windows Defender, etc.). Any attempt to access protected processes is gracefully skipped.
    *   **Error Handling:** All system API calls are wrapped in robust error handling, preventing crashes and ensuring the application remains stable.
    *   **No Process Termination:** The application **never kills any running processes** to free memory, preventing data loss or system instability.
4.  **Run on Windows Startup:**
    *   Due to the deeper cleaning capabilities requiring Administrator privileges, the "Run on Windows Startup" feature is implemented using **Windows Task Scheduler**. This creates a scheduled task that runs the application with "Highest Privileges" when a user logs on, bypassing UAC prompts at startup.

## 🚀 Requirements

*   **Operating System:** Windows 10 (64-bit) or Windows 11 (64-bit).
*   **.NET Runtime:** .NET 9.0 Runtime (or newer).
*   **Administrator Privileges:** Required for the application to function correctly, especially for deeper cleaning operations and "Run on Windows Startup". The application will automatically request these privileges on launch.

## 🛠️ How to Build & Run

### 1. Clone the Repository

First, clone the project to your local machine using Git:

```bash
git clone https://github.com/Pr-Imran/WindowsMemoryCreaner.git
cd WindowsMemoryCreaner
```

### 2. Build and Run

#### Option A: Using Visual Studio 2022 (Recommended)

1.  Open the `WindowsMemoryCreaner` folder in Visual Studio 2022.
    *   You can directly open the `.sln` file if present, or open the folder containing `RamCleaner.csproj`.
2.  Visual Studio will restore any necessary NuGet packages automatically.
3.  Press `F5` to build and run the application in debug mode.
4.  **Accept the User Account Control (UAC) prompt** when it appears, as the application requires Administrator privileges.

#### Option B: Using the .NET CLI

1.  Open a terminal (Command Prompt, PowerShell, or Windows Terminal) and navigate to the `RamCleaner` directory within the cloned repository:
    ```bash
    cd RamCleaner
    ```
2.  Build the application:
    ```bash
    dotnet build
    ```
3.  Run the application. You might need to launch the executable with Administrator privileges:
    ```bash
    # To run directly (might trigger UAC or fail if not run as Admin)
    dotnet run

    # Or navigate to the output directory and run the exe as admin:
    # cd bin\Debug\net9.0-windows
    # .\RamCleaner.exe  (then right-click -> Run as Administrator)
    ```
    **Important:** Always run the application with Administrator privileges to ensure full functionality, especially for deeper cleaning and auto-startup features.

## 💡 Usage

The application provides a straightforward user interface:

*   **Memory Status:** The top section displays real-time statistics for Total, Used, Available, Cached, and Committed RAM.
*   **Clean Memory Now:** Click this button for an immediate manual clean. A system notification will appear showing memory freed.
*   **Auto-Clean Strategies:**
    *   **Usage > [Slider]%:** Enable this and set a threshold. The app will automatically clean when RAM usage exceeds this percentage.
    *   **Every [Slider] min:** Enable this and set an interval. The app will automatically clean after this many minutes.
*   **Run on Windows Startup:** Check this box to enable the application to launch automatically when you log into Windows. This uses the Task Scheduler.
*   **Event Log:** The bottom section provides a log of all cleaning activities.

## 📄 License

This project is licensed under the MIT License. See the [LICENSE](LICENSE) file for details.