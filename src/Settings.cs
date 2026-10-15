using System;
using System.Drawing;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Web.Script.Serialization;
using System.Windows.Forms;
using Microsoft.Win32;

namespace QuietPC {
    public sealed class Preferences {
        public bool Notifications { get; set; }
        public bool Audio { get; set; }
        public bool AlwaysVisible { get; set; }
        public bool ResumeQuiet { get; set; }
        public int Shortcut { get; set; }
        public Preferences() { Notifications = Audio = AlwaysVisible = ResumeQuiet = true; Shortcut = 1; }
        internal static string PathName { get { return Path.Combine(Program.DataDir, "settings.json"); } }
        public static Preferences Load() {
            if (!File.Exists(PathName)) return new Preferences();
            var value = new JavaScriptSerializer().Deserialize<Preferences>(File.ReadAllText(PathName));
            if (value == null || (!value.Notifications && !value.Audio) || value.Shortcut < 0 || value.Shortcut > 3)
                throw new InvalidDataException("Invalid Quiet PC settings. Your file has been preserved.");
            return value;
        }
        public void Save() {
            Directory.CreateDirectory(Program.DataDir);
            string temp = PathName + ".new";
            using (var stream = new FileStream(temp, FileMode.Create, FileAccess.Write, FileShare.None, 4096, FileOptions.WriteThrough)) {
                var bytes = Encoding.UTF8.GetBytes(new JavaScriptSerializer().Serialize(this));
                stream.Write(bytes, 0, bytes.Length); stream.Flush(true);
            }
            if (File.Exists(PathName)) File.Replace(temp, PathName, PathName + ".bak"); else File.Move(temp, PathName);
        }
    }
    static class DesktopIntegration {
        const string RunPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
        public static bool Startup {
            get { using (var key = Registry.CurrentUser.OpenSubKey(RunPath)) return key != null && key.GetValue("QuietPC") != null; }
        }
        public static void SetStartup(bool enabled) {
            if (enabled == Startup) return;
            var integration = StartupIntegration.Load();
            string original = integration == null ? null : File.ReadAllText(integration.RegisterPath);
            if (integration != null) UpdateRegister(integration, original, enabled);
            try {
                using (var key = Registry.CurrentUser.CreateSubKey(RunPath)) {
                    if (enabled) key.SetValue("QuietPC", "\"" + Application.ExecutablePath + "\" --background");
                    else key.DeleteValue("QuietPC", false);
                }
            } catch { if (integration != null) File.WriteAllText(integration.RegisterPath, original, new UTF8Encoding(false)); throw; }
        }
        static void UpdateRegister(StartupIntegration integration, string original, bool enabled) {
            const string marker = "<!-- live-state:begin -->";
            int split = original.IndexOf(marker, StringComparison.Ordinal);
            string manual = split < 0 ? original : original.Substring(0, split);
            string generated = split < 0 ? "" : original.Substring(split);
            manual = Regex.Replace(manual, @"(?m)^\| HKCU Run \| `QuietPC` \|.*\r?\n", "");
            if (enabled) {
                string anchor = integration.InsertBefore;
                int at = manual.IndexOf(anchor, StringComparison.Ordinal);
                if (at < 0) throw new IOException("Startup register section could not be found.");
                string row = "| HKCU Run | `QuietPC` | Quiet PC tray switch at sign-in; startup configurable in Settings | `" + Application.ExecutablePath + " --background`; source `" + integration.SourceDirectory + "`; remove with `scripts\\uninstall.ps1` |\r\n";
                manual = manual.Insert(at, row);
            }
            File.WriteAllText(integration.RegisterPath, manual + generated, new UTF8Encoding(false));
        }
        public static bool PromoteIcon() {
            bool changed = false;
            using (var root = Registry.CurrentUser.OpenSubKey(@"Control Panel\NotifyIconSettings", true)) {
                if (root == null) return false;
                foreach (string name in root.GetSubKeyNames()) using (var key = root.OpenSubKey(name, true)) {
                    if (key == null) continue;
                    string path = Environment.ExpandEnvironmentVariables(Convert.ToString(key.GetValue("ExecutablePath")));
                    if (!string.Equals(path, Application.ExecutablePath, StringComparison.OrdinalIgnoreCase)) continue;
                    if (!object.Equals(key.GetValue("IsPromoted"), 1)) { key.SetValue("IsPromoted", 1, RegistryValueKind.DWord); changed = true; }
                }
            }
            return changed;
        }
    }
    public sealed class StartupIntegration {
        public string RegisterPath { get; set; }
        public string InsertBefore { get; set; }
        public string SourceDirectory { get; set; }
        internal static StartupIntegration Load() {
            string path = Path.Combine(Program.DataDir, "integration.json");
            if (!File.Exists(path)) return null;
            var value = new JavaScriptSerializer().Deserialize<StartupIntegration>(File.ReadAllText(path));
            if (value == null || string.IsNullOrWhiteSpace(value.RegisterPath) || string.IsNullOrWhiteSpace(value.InsertBefore) || string.IsNullOrWhiteSpace(value.SourceDirectory))
                throw new InvalidDataException("Invalid optional startup-register integration.");
            return value;
        }
    }
}
