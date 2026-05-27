using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace WinLaunch
{
    public class CustomContextMenuAction
    {
        public string Title { get; set; }
        public string Command { get; set; }
        public string Arguments { get; set; }
        public string WorkingDirectory { get; set; }
        public List<string> Extensions { get; set; }
        public List<string> TargetTypes { get; set; }
        public bool RunAsAdmin { get; set; }
        public bool IncludeFolders { get; set; }

        public bool AppliesTo(SBItem item)
        {
            if (item == null || string.IsNullOrWhiteSpace(Title) || string.IsNullOrWhiteSpace(Command))
                return false;

            if (item.IsFolder && !IncludeFolders)
                return false;

            string path = GetLaunchPath(item);
            string targetPath = GetShortcutTargetPath(path);

            if (!MatchesTargetTypes(path, targetPath))
                return false;

            if (Extensions == null || Extensions.Count == 0)
                return true;

            string extension = Path.GetExtension(targetPath);

            foreach (string configuredExtension in Extensions)
            {
                if (string.IsNullOrWhiteSpace(configuredExtension))
                    continue;

                string normalized = configuredExtension.StartsWith(".")
                    ? configuredExtension
                    : "." + configuredExtension;

                if (string.Equals(extension, normalized, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        private bool MatchesTargetTypes(string path, string targetPath)
        {
            if (TargetTypes == null || TargetTypes.Count == 0)
                return true;

            foreach (string targetType in TargetTypes)
            {
                if (string.IsNullOrWhiteSpace(targetType))
                    continue;

                string normalized = targetType.Trim().ToLowerInvariant();

                if ((normalized == "directory" || normalized == "folder") && Directory.Exists(targetPath))
                    return true;

                if (normalized == "file" && File.Exists(targetPath))
                    return true;

                if (normalized == "shortcut" && string.Equals(Path.GetExtension(path), ".lnk", StringComparison.OrdinalIgnoreCase))
                    return true;

                if ((normalized == "url" || normalized == "uri") && Uri.IsWellFormedUriString(targetPath, UriKind.Absolute))
                    return true;
            }

            return false;
        }

        public void Execute(SBItem item)
        {
            string path = GetLaunchPath(item);
            string targetPath = GetShortcutTargetPath(path);

            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.UseShellExecute = true;
            startInfo.FileName = ReplacePlaceholders(Command, item, path, targetPath);
            startInfo.Arguments = ReplacePlaceholders(Arguments, item, path, targetPath);

            string workingDirectory = ReplacePlaceholders(WorkingDirectory, item, path, targetPath);
            if (!string.IsNullOrWhiteSpace(workingDirectory))
            {
                startInfo.WorkingDirectory = workingDirectory;
            }

            if (RunAsAdmin)
            {
                startInfo.Verb = "runas";
            }

            Process.Start(startInfo);
        }

        public static List<CustomContextMenuAction> LoadActions()
        {
            try
            {
                string path = PortabilityManager.ContextMenuActionsPath;
                if (!File.Exists(path))
                    return new List<CustomContextMenuAction>();

                string json = File.ReadAllText(path);
                List<CustomContextMenuAction> actions = JsonConvert.DeserializeObject<List<CustomContextMenuAction>>(json);

                return actions ?? new List<CustomContextMenuAction>();
            }
            catch
            {
                return new List<CustomContextMenuAction>();
            }
        }

        public static string GetLaunchPath(SBItem item)
        {
            string path = item.ApplicationPath;

            if (Path.GetExtension(path).ToLower() == ".lnk" && ItemCollection.IsInCache(item.ApplicationPath))
            {
                path = Path.Combine(PortabilityManager.LinkCachePath, path);
                path = Path.GetFullPath(path);
            }

            return path;
        }

        private static string GetShortcutTargetPath(string path)
        {
            try
            {
                if (Path.GetExtension(path).ToLower() == ".lnk")
                {
                    string shortcutPath = MiscUtils.GetShortcutTargetFile(path);
                    if (!string.IsNullOrEmpty(shortcutPath))
                        return shortcutPath;
                }
            }
            catch { }

            return path;
        }

        private static string ReplacePlaceholders(string value, SBItem item, string path, string targetPath)
        {
            if (string.IsNullOrEmpty(value))
                return "";

            string directory = "";
            try
            {
                directory = Directory.Exists(targetPath) ? targetPath : Path.GetDirectoryName(targetPath);
            }
            catch { }

            return Environment.ExpandEnvironmentVariables(value)
                .Replace("{path}", path ?? "")
                .Replace("{targetPath}", targetPath ?? "")
                .Replace("{directory}", directory ?? "")
                .Replace("{name}", item.Name ?? "")
                .Replace("{arguments}", item.Arguments ?? "");
        }
    }
}
