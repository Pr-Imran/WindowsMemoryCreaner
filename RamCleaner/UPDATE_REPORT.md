RAM Cleaner - Feature Update Report
===================================

1. Analysis of Existing Features
--------------------------------
- **Start with Windows:** Already implemented via Task Scheduler in `StartupService.cs`.
- **Notifications:** Basic balloon tip infrastructure existed.
- **System Tray:** Basic `NotifyIcon` existed but lacked interaction (context menu, minimize-to-tray).
- **RAM Graph:** Missing.
- **Whitelist:** Missing (only hardcoded critical list existed).

2. Implemented Features
-----------------------

### A. RAM Usage History Graph
- **What:** A real-time line graph showing RAM usage history (last 60 seconds).
- **How:** 
  - Added `HistoryPoints` (PointCollection) to `MainViewModel`.
  - Updated every second in `MonitorTimer_Tick`.
  - Displayed using a `Polyline` within the "System Memory Status" block.

### B. System Tray Mode
- **What:** Application now minimizes to tray instead of taskbar.
- **How:** 
  - Overrode `OnStateChanged` in `MainWindow` to hide the window when minimized.
  - Added a Context Menu to the Tray Icon: "Open / Restore", "Clean Now", "Exit".
  - Updated Tray Icon tooltip with live RAM usage %.

### C. Start with Windows (Minimized)
- **What:** The app can now start silently in the tray.
- **How:** 
  - Updated `StartupService` to add the `-minimized` argument to the scheduled task.
  - Updated `App.xaml.cs` to detect this argument.
  - Updated `MainWindow` constructor to start in a hidden state if the argument is present.

### D. Process Whitelist
- **What:** Users can add specific processes to be ignored during cleaning.
- **How:** 
  - Added `whitelist.txt` handling in `MemoryService`.
  - Added a new "Whitelist" tab in the UI to Add/Remove process names.
  - Updated `CleanMemory` logic to skip processes in this list.

### E. Notification Settings
- **What:** Granular control over when to show notifications.
- **How:** 
  - Added "Options" tab with checkboxes: "Show after Manual Clean" and "Show after Auto Clean".
  - `MainViewModel` now checks these flags before requesting a notification.

3. Safety & Stability
---------------------
- **Zero-Impact Defaults:** Existing Critical Process protection remains untouched. The user whitelist is an *additional* filter.
- **Performance:** The graph uses a simple `Polyline` with ~60 points, ensuring negligible CPU impact.
- **Persistence:** Whitelist is saved to a simple text file `whitelist.txt` in the app directory, ensuring it survives restarts.

4. Build Instructions
---------------------
- No new external dependencies.
- Build normally using Visual Studio or `dotnet build`.
- **Note:** The app requires Administrator privileges to clean system cache efficiently (manifest is already set).
