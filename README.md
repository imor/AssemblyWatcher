# AssemblyWatcher

AssemblyWatcher is an ILSpy plugin which reloads assemblies when they change.

## Installation
Copy the AssemblyWatcher.Plugin.dll file into the same folder as ILSpy executable and restart ILSpy.

## Usage
If the plugin is detected by ILSpy, you'll see an eye icon in the toolbar. This is a toggle button. Pressing it will enable watching of loaded assemblies. Pressing again will disable. When watching is enabled, and any of the loaded assemblies change on disk, the plugin refreshes all the assemblies.
