## Custom Item Context Menu Actions

Now WinLaunch can add custom entries to the right-click context menu for launcher
items by reading a JSON file from the user's WinLaunch data directory.

Default installed-mode path:

```text
%APPDATA%\WinLaunch\ContextMenuActions.json
```

Portable-mode path:

```text
Data\ContextMenuActions.json
```

The file is read each time an item context menu is opened, so changing the JSON
does not require rebuilding WinLaunch. In most cases, reopening the right-click
menu is enough.

### File Format

The JSON file must contain an array of action objects:

```json
[
  {
    "title": "Open with VS Code",
    "command": "code",
    "arguments": "\"{targetPath}\""
  },
  {
    "title": "Open with WSL",
    "command": "wt.exe",
    "arguments": "-d \"{targetPath}\" wsl.exe",
    "targetTypes": ["directory"]
  },
  {
    "title": "Open with Windows Terminal",
    "command": "wt.exe",
    "arguments": "-d \"{targetPath}\"",
    "targetTypes": ["directory"]
  },
  {
    "title": "Open with Typora",
    "command": "typora.exe",
    "arguments": "\"{targetPath}\"",
  },
  {
    "title": "Show Target in Explorer",
    "command": "explorer.exe",
    "arguments": "/select,\"{targetPath}\""
  }
]
```



### Fields

`title`

Required. The text shown in the item context menu.

`command`

Required. The executable, shell command, URL, or document to launch. The command
is started through Windows shell execution, so commands available in the Windows
environment, such as `code`, can be used.

`arguments`

Optional. Arguments passed to `command`.

`workingDirectory`

Optional. Working directory used when launching the command.

`extensions`

Optional. A list of file extensions that the action applies to, for example
`[".txt", ".md", ".cs"]`. If omitted or empty, the action is shown for every
non-folder item. Note that many WinLaunch items are cached `.lnk` shortcuts, so
the filter is applied to the resolved shortcut target path when possible.

This field is only for extension names. It is not how Windows folders are
detected, because folders usually have no extension.

`targetTypes`

Optional. A list of target kinds that the action applies to. Supported values:
`"file"`, `"directory"`/`"folder"`, `"shortcut"`, and `"url"`/`"uri"`.

Use this when an action only makes sense for Windows folders. For example:

```json
{
  "title": "Open with WSL",
  "command": "wt.exe",
  "arguments": "-d \"{targetPath}\" wsl.exe",
  "targetTypes": ["directory"]
}
```

`runAsAdmin`

Optional boolean. When `true`, launches the command with the `runas` shell verb.

`includeFolders`

Optional boolean. Folder items are ignored by default. Set this to `true` if the
action should also appear for folders.

### Placeholders

The following placeholders can be used in `command`, `arguments`, and
`workingDirectory`:

`{path}`

The path stored by WinLaunch for the item. For cached shortcuts, this is expanded
to the cached `.lnk` file path.

`{targetPath}`

The resolved shortcut target when the item is a `.lnk`; otherwise the same value
as `{path}`.

`{directory}`

The directory containing `{targetPath}`. If the target itself is a directory,
this is the target directory.

`{name}`

The WinLaunch item name.

`{arguments}`

The arguments configured on the WinLaunch item.

Environment variables such as `%APPDATA%` are also expanded.

## Build and Run

This is to remind myself.

These commands are run from WSL, but they invoke Windows PowerShell with
`powershell.exe`. The application commands themselves are launched by WinLaunch
through Windows shell execution (`ProcessStartInfo.UseShellExecute = true`), not
inside WSL, `cmd.exe`, or PowerShell. If an action uses `code`, `wt.exe`, or
`wsl.exe`, it must be available to the Windows environment.

```bash
powershell.exe -NoProfile -Command '$msbuild = "C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe"; & $msbuild "C:\Projects\WinLaunch\WinLaunch.sln" /t:Restore /p:RestorePackagesConfig=true /p:Configuration=Debug /p:Platform=x86'

powershell.exe -NoProfile -Command '$msbuild = "C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe"; & $msbuild "C:\Projects\WinLaunch\WinLaunch.sln" /t:Build /p:Configuration=Debug /p:Platform=x86 /m'

powershell.exe -NoProfile -Command 'Start-Process "C:\Projects\WinLaunch\WinLaunch\bin\Debug\WinLaunch.exe"'
```
