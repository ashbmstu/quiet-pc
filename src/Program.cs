using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Threading;
using System.Web.Script.Serialization;
using System.Windows.Forms;

[assembly: System.Reflection.AssemblyTitle("Quiet PC")]
[assembly: System.Reflection.AssemblyDescription("One-click notifications and playback mute")]
[assembly: System.Reflection.AssemblyVersion("1.2.2.0")]

namespace QuietPC {
    static class Program {
        internal static readonly string DataDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "QuietPC");
        internal static readonly string StatePath = Path.Combine(DataDir, "state.json");
        internal static readonly string Prefix = @"Local\QuietPC-" + WindowsIdentity.GetCurrent().User.Value;
        internal static void Log(string text) {
            try {
                Directory.CreateDirectory(DataDir);
                string path = Path.Combine(DataDir, "quiet-pc.log");
                if (File.Exists(path) && new FileInfo(path).Length > 512000) File.Copy(path, path + ".old", true);
                if (File.Exists(path) && new FileInfo(path).Length > 512000) File.WriteAllText(path, "");
                File.AppendAllText(path, DateTimeOffset.Now.ToString("o") + " " + text + Environment.NewLine);
            } catch { }
        }
        [STAThread] static int Main(string[] args) {
            string command = args.Length == 0 ? "--toggle" : args[0].ToLowerInvariant();
            string output = null;
            if (args.Length >= 3 && args[args.Length - 2] == "--output") output = args[args.Length - 1];
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            try {
                int minutes = 0;
                int offset = 1;
                if (command == "--for") {
                    if (args.Length < 2 || !int.TryParse(args[1], out minutes) || minutes < 1 || minutes > 10080)
                        throw new ArgumentException("--for needs whole minutes from 1 to 10080.");
                    offset = 2;
                }
                if (args.Length > offset) {
                    if (args.Length != offset + 2 || args[offset] != "--output") throw new ArgumentException("Use --output followed by a JSON file path.");
                    output = args[offset + 1];
                }
                if (command == "--status") {
                    var state = new StateStore(StatePath).Read();
                    var report = new { Running = IsRunning(), Quiet = state.Active, EndsAtUtc = state.EndsAtUtcTicks.HasValue ? new DateTimeOffset(state.EndsAtUtcTicks.Value, TimeSpan.Zero).ToString("o") : null,
                        ManualAudioDevices = state.Active && state.AudioOwnership != null ? state.AudioOwnership.Count(pair => !pair.Value) : 0,
                        PendingAudioRestores = state.Active ? 0 : state.Devices.Count, PendingNotificationRestore = !state.Active && state.RestoreNotifications, Settings = Preferences.Load(), Startup = DesktopIntegration.Startup };
                    WriteResult(report, output); return 0;
                }
                if (command == "--diagnose") {
                    using (var audio = new AudioControl()) using (var notifications = new NotificationControl()) {
                        var report = new { Notifications = notifications.Get(), Audio = audio.Read(), Recovery = new StateStore(StatePath).Read(), Settings = Preferences.Load(), Startup = DesktopIntegration.Startup };
                        string json = new JavaScriptSerializer().Serialize(report);
                        if (output != null) File.WriteAllText(output, json);
                        else { Directory.CreateDirectory(DataDir); File.WriteAllText(Path.Combine(DataDir, "diagnostics.json"), json); }
                    }
                    return 0;
                }
                if (command != "--toggle" && command != "--on" && command != "--off" && command != "--exit" && command != "--background" && command != "--settings" && command != "--timer" && command != "--for" && command != "--cancel-timer" && command != "--handoff")
                    throw new ArgumentException("Unknown Quiet PC command: " + command);
                if (command != "--background") {
                    if (!IsRunning()) {
                        var launch = new ProcessStartInfo(Application.ExecutablePath, "--background") { UseShellExecute = false, CreateNoWindow = true, WindowStyle = ProcessWindowStyle.Hidden };
                        using (var started = Process.Start(launch)) { }
                    }
                    var reply = CommandChannel.Send(new CommandRequest { Action = command, Minutes = minutes }, 8000);
                    WriteResult(reply, output); return reply.Ok ? 0 : 1;
                }
                bool first;
                using (var mutex = new Mutex(true, Prefix + "-Instance", out first)) {
                    if (!first) {
                        return 0;
                    }
                    try {
                        using (var window = new TrayWindow(command)) Application.Run(window);
                    } finally { mutex.ReleaseMutex(); }
                }
                return 0;
            } catch (Exception e) {
                Log(e.ToString());
                if (command != "--background") try { WriteResult(new CommandReply { Ok = false, Error = e.Message }, output); } catch (Exception writeError) { Log("Could not write command reply: " + writeError.Message); }
                return 1;
            }
        }
        internal static bool IsRunning() {
            Mutex mutex;
            if (!Mutex.TryOpenExisting(Prefix + "-Instance", out mutex)) return false;
            mutex.Dispose(); return true;
        }
        static void WriteResult(object value, string output) {
            string json = new JavaScriptSerializer().Serialize(value);
            if (output != null) { File.WriteAllText(output, json); return; }
            try { using (var writer = new StreamWriter(Console.OpenStandardOutput())) { writer.WriteLine(json); } } catch { }
        }
    }

    sealed class TrayWindow : Form {
        readonly AudioControl audio;
        readonly NotificationControl notifications;
        readonly QuietEngine engine;
        readonly NotifyIcon tray;
        readonly Icon onIcon, offIcon, warningIcon;
        readonly ToolStripMenuItem status, toggle, detail, timerStatus, cancelTimer;
        readonly System.Windows.Forms.Timer refresh;
        CommandChannel channel;
        Preferences preferences;
        SettingsWindow settingsWindow;
        SessionWindow sessionWindow;
        DateTime nextPromotion = DateTime.MinValue;
        string shortcutError = "";
        [DllImport("user32.dll", SetLastError = true)] static extern bool RegisterHotKey(IntPtr window, int id, uint modifiers, uint key);
        [DllImport("user32.dll")] static extern bool UnregisterHotKey(IntPtr window, int id);
        [DllImport("user32.dll")] static extern bool ShowWindow(IntPtr window, int command);
        bool ending, quitting, resourcesDisposed;
        DateTime lastClick = DateTime.MinValue;
        string lastError = "";
        readonly string initial;

        public TrayWindow(string initial) {
            this.initial = initial;
            ShowInTaskbar = false; FormBorderStyle = FormBorderStyle.FixedToolWindow;
            Text = "Quiet PC controller"; Opacity = 0;
            audio = new AudioControl(); notifications = new NotificationControl();
            engine = new QuietEngine(audio, notifications, new StateStore(Program.StatePath));
            preferences = Preferences.Load();
            engine.SuppressNotifications = preferences.Notifications; engine.MuteAudio = preferences.Audio;
            onIcon = MakeIcon(1); offIcon = MakeIcon(0); warningIcon = MakeIcon(2);
            var menu = new ContextMenuStrip();
            status = new ToolStripMenuItem(); status.Enabled = false;
            toggle = new ToolStripMenuItem(); toggle.Click += delegate { Execute("--toggle", true); };
            detail = new ToolStripMenuItem("Details");
            detail.Click += delegate {
                string message = "Left-click the tray icon to toggle Quiet PC.\r\n\r\n" +
                    "Quiet ON suppresses the notifications and/or audio selected in Settings.\r\n" +
                    "Quiet OFF restores the previous notification and mute settings.\r\n" +
                    "Volume levels and microphones are not changed.\r\n\r\n" +
                    "Disconnected devices waiting for restoration: " + (engine.State.Active ? 0 : engine.State.Devices.Count) +
                    "\r\n\r\nRecovery data: " + Program.StatePath;
                if (engine.Error.Length > 0) message += "\r\n\r\n" + engine.Error;
                MessageBox.Show(message, "Quiet PC", MessageBoxButtons.OK, MessageBoxIcon.Information);
            };
            var quit = new ToolStripMenuItem("Restore && exit"); quit.Click += delegate { Execute("--exit", true); };
            var settings = new ToolStripMenuItem("Settings..."); settings.Click += delegate { OpenSettings(); };
            var timed = new ToolStripMenuItem("Quiet for");
            timed.DropDownItems.Add("30 min", null, delegate { StartTimed(30); });
            timed.DropDownItems.Add("1 hour", null, delegate { StartTimed(60); });
            timed.DropDownItems.Add("Custom...", null, delegate { OpenTimer(); });
            timerStatus = new ToolStripMenuItem("Timer..."); timerStatus.Click += delegate { OpenTimer(); };
            cancelTimer = new ToolStripMenuItem("No timer"); cancelTimer.Click += delegate { Execute("--cancel-timer", true); };
            menu.Items.AddRange(new ToolStripItem[] { status, toggle, timed, timerStatus, cancelTimer, new ToolStripSeparator(), settings, detail, quit });
            tray = new NotifyIcon { Icon = offIcon, Text = "Quiet PC", ContextMenuStrip = menu, Visible = true };
            tray.MouseClick += delegate(object sender, MouseEventArgs e) {
                if (e.Button != MouseButtons.Left || (DateTime.UtcNow - lastClick).TotalMilliseconds < 500) return;
                lastClick = DateTime.UtcNow; Execute("--toggle", true);
            };
            refresh = new System.Windows.Forms.Timer { Interval = 1000 };
            refresh.Tick += delegate { RefreshState(); };
            Microsoft.Win32.SystemEvents.PowerModeChanged += PowerChanged;
            IntPtr handle = Handle; // Create the invisible controller before Application.Run.
        }
        protected override void SetVisibleCore(bool value) { base.SetVisibleCore(false); }
        protected override void OnHandleCreated(EventArgs e) {
            base.OnHandleCreated(e);
            BeginInvoke((Action)delegate {
                audio.Observe(delegate {
                    if (!IsDisposed && IsHandleCreated && !quitting) try { BeginInvoke((Action)RefreshState); } catch { }
                });
                if (!preferences.ResumeQuiet && engine.State.Active) engine.Disable();
                try { SetHotkey(preferences.Shortcut); } catch (Exception error) { shortcutError = error.Message; Program.Log(shortcutError); }
                Promote();
                RefreshState();
                if (initial != "--background") Execute(initial, false);
                if (quitting) return;
                channel = new CommandChannel(HandleCommand, delegate { BeginInvoke((Action)delegate { quitting = true; Close(); }); });
                refresh.Start();
                Program.Log("Started; quiet=" + engine.State.Active);
            });
        }
        void PowerChanged(object sender, Microsoft.Win32.PowerModeChangedEventArgs e) {
            if (e.Mode == Microsoft.Win32.PowerModes.Resume && IsHandleCreated && !IsDisposed)
                try { BeginInvoke((Action)RefreshState); } catch { }
        }
        void RefreshState() {
            if (quitting || ending) return;
            try { engine.Refresh(); if (DateTime.UtcNow >= nextPromotion) Promote(); UpdateStatus(); }
            catch (Exception e) { ShowFailure(e, false); }
        }
        void Execute(string command, bool interactive) {
            try {
                if (command == "--settings") { OpenSettings(); return; }
                if (command == "--timer") { OpenTimer(); return; }
                if (command == "--cancel-timer") engine.CancelTimer();
                if (command == "--toggle") engine.Toggle();
                if (command == "--on") engine.Enable();
                if (command == "--off" || command == "--exit") engine.Disable();
                UpdateStatus();
                Program.Log(command + "; quiet=" + engine.State.Active + "; pending=" + engine.State.Devices.Count);
                if (command == "--exit") {
                    if (engine.Error.Length > 0 || engine.State.RestoreNotifications || engine.State.Devices.Count > 0)
                        throw new InvalidOperationException("Some devices still need restoration. Reconnect them before exiting.\r\n" + engine.Error);
                    quitting = true; Close();
                } else if (interactive && engine.Error.Length > 0)
                    MessageBox.Show(engine.Error, "Quiet PC could not apply every setting", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            } catch (Exception e) { ShowFailure(e, interactive); }
        }
        CommandReply HandleCommand(CommandRequest request) {
            return (CommandReply)Invoke((Func<CommandReply>)delegate {
                try {
                    switch (request.Action) {
                        case "--for": engine.EnableFor(TimeSpan.FromMinutes(request.Minutes)); break;
                        case "--on": engine.Enable(); break;
                        case "--off": engine.Disable(); break;
                        case "--toggle": engine.Toggle(); break;
                        case "--cancel-timer": engine.CancelTimer(); break;
                        case "--settings": OpenSettings(); break;
                        case "--timer": OpenTimer(); break;
                        case "--handoff": break; // Installer-only stop; leave recovery state intact.
                        case "--exit":
                            engine.Disable();
                            if (engine.Error.Length > 0 || engine.State.Devices.Count > 0 || engine.State.RestoreNotifications)
                                throw new InvalidOperationException("Restoration is pending. Reconnect audio devices and retry.");
                            break;
                        default: throw new ArgumentException("Unsupported command.");
                    }
                    UpdateStatus();
                    return Reply(request.Action == "--handoff" ? "" : engine.Error);
                } catch (Exception e) { return Reply(e.Message); }
            });
        }
        CommandReply Reply(string error) {
            return new CommandReply { Ok = string.IsNullOrEmpty(error), Error = error, Quiet = engine.State.Active,
                ManualAudioDevices = engine.ManualAudioDevices,
                EndsAtUtc = engine.EndsAt.HasValue ? engine.EndsAt.Value.ToString("o") : null,
                PendingAudioRestores = engine.State.Active ? 0 : engine.State.Devices.Count,
                PendingNotificationRestore = !engine.State.Active && engine.State.RestoreNotifications };
        }
        void StartTimed(int minutes) {
            try { engine.EnableFor(TimeSpan.FromMinutes(minutes)); UpdateStatus(); if (engine.Error.Length > 0) throw new InvalidOperationException(engine.Error); }
            catch (Exception e) { ShowFailure(e, true); }
        }
        void OpenTimer() {
            if (sessionWindow != null && !sessionWindow.IsDisposed) { sessionWindow.Show(); sessionWindow.Activate(); return; }
            sessionWindow = new SessionWindow(engine,
                delegate(int minutes) { engine.EnableFor(TimeSpan.FromMinutes(minutes)); UpdateStatus(); if (engine.Error.Length > 0) throw new InvalidOperationException(engine.Error); },
                delegate { engine.CancelTimer(); UpdateStatus(); }, delegate { engine.Disable(); UpdateStatus(); });
            sessionWindow.Show(); ShowWindow(sessionWindow.Handle, 5); sessionWindow.Activate();
        }
        void Promote() {
            nextPromotion = DateTime.UtcNow.AddSeconds(5);
            if (!preferences.AlwaysVisible) return;
            try {
                if (DesktopIntegration.PromoteIcon()) { tray.Visible = false; tray.Visible = true; }
            } catch (Exception e) { Program.Log("Tray visibility: " + e.Message); }
        }
        void SetHotkey(int choice) {
            if (choice == 0) { UnregisterHotKey(Handle, 7); return; }
            uint modifiers = choice == 2 ? 6u : 3u; // Ctrl+Shift or Ctrl+Alt, plus no-repeat.
            uint key = choice == 1 ? (uint)Keys.Q : (uint)Keys.F12;
            if (!RegisterHotKey(Handle, 8, modifiers | 0x4000, key))
                throw new InvalidOperationException("That shortcut is already in use. Choose another shortcut or None.");
            UnregisterHotKey(Handle, 8); UnregisterHotKey(Handle, 7);
            if (!RegisterHotKey(Handle, 7, modifiers | 0x4000, key)) throw new InvalidOperationException("Could not register the keyboard shortcut.");
        }
        void OpenSettings() {
            if (settingsWindow != null && !settingsWindow.IsDisposed) { settingsWindow.Show(); settingsWindow.Activate(); return; }
            settingsWindow = new SettingsWindow(preferences, ApplySettings);
            settingsWindow.Show(); ShowWindow(settingsWindow.Handle, 5); settingsWindow.Activate();
            if (shortcutError.Length > 0) Program.Log("Shortcut unavailable; change it in Settings.");
        }
        void ApplySettings(Preferences value, bool startup) {
            var old = preferences;
            bool oldStartup = DesktopIntegration.Startup;
            bool changeHotkey = value.Shortcut != old.Shortcut || shortcutError.Length > 0;
            try {
                if (changeHotkey) SetHotkey(value.Shortcut);
                DesktopIntegration.SetStartup(startup);
                value.Save();
            } catch {
                if (changeHotkey) try { SetHotkey(old.Shortcut); } catch { }
                try { DesktopIntegration.SetStartup(oldStartup); } catch { }
                throw;
            }
            preferences = value; shortcutError = "";
            engine.SuppressNotifications = value.Notifications; engine.MuteAudio = value.Audio;
            Promote(); UpdateStatus();
            Program.Log("Settings saved.");
        }
        void ShowFailure(Exception error, bool interactive) {
            Program.Log(error.ToString()); tray.Icon = warningIcon; tray.Text = "Quiet PC - needs attention (right-click for details)";
            status.Text = "Needs attention";
            if (interactive) MessageBox.Show(error.Message, "Quiet PC needs attention", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        void UpdateStatus() {
            bool failed = engine.Error.Length > 0;
            tray.Icon = failed ? warningIcon : engine.State.Active ? onIcon : offIcon;
            string activeLabel = engine.State.MuteAudio && engine.State.RestoreNotifications ? "Quiet ON - notifications and audio muted" : engine.State.MuteAudio ? "Quiet ON - audio muted" : "Quiet ON - notifications suppressed";
            if (engine.ManualAudioDevices > 0) activeLabel = "Quiet ON - manual audio";
            string label = failed ? "Needs attention" : engine.State.Active ? activeLabel : "Quiet OFF - previous settings restored";
            if (!failed && !engine.State.Active && engine.State.Devices.Count > 0) label = "Quiet OFF - waiting to restore disconnected audio";
            tray.Text = "Quiet PC: " + label; status.Text = label;
            toggle.Text = engine.State.Active ? "Restore" : "Quiet on";
            cancelTimer.Visible = engine.EndsAt.HasValue;
            if (engine.EndsAt.HasValue) {
                int remaining = Math.Max(0, (int)Math.Ceiling((engine.EndsAt.Value - DateTimeOffset.UtcNow).TotalMinutes));
                timerStatus.Text = "Until " + engine.EndsAt.Value.ToLocalTime().ToString("HH:mm") + " - " + remaining + " min left";
                tray.Text = "Quiet PC: ON - " + remaining + " min left" + (engine.ManualAudioDevices > 0 ? " - manual audio" : " (until " + engine.EndsAt.Value.ToLocalTime().ToString("HH:mm") + ")");
            } else timerStatus.Text = "Timer...";
            if (lastError != engine.Error) { lastError = engine.Error; Program.Log(failed ? engine.Error : "All settings applied successfully."); }
        }
        protected override void WndProc(ref Message m) {
            if (m.Msg == 0x312 && m.WParam == new IntPtr(7)) { Execute("--toggle", true); return; }
            if (m.Msg == 0x11) { m.Result = new IntPtr(1); return; } // WM_QUERYENDSESSION
            if (m.Msg == 0x16 && m.WParam != IntPtr.Zero) ending = true; // Retain state across sign-out/reboot.
            base.WndProc(ref m);
        }
        protected override void OnFormClosing(FormClosingEventArgs e) {
            if (!quitting && !ending && e.CloseReason != CloseReason.WindowsShutDown && e.CloseReason != CloseReason.TaskManagerClosing) {
                engine.Disable(); UpdateStatus();
                if (engine.Error.Length > 0 || engine.State.Devices.Count > 0) { e.Cancel = true; return; }
            }
            base.OnFormClosing(e);
        }
        protected override void Dispose(bool disposing) {
            if (disposing && !resourcesDisposed) {
                resourcesDisposed = true;
                quitting = true;
                if (IsHandleCreated) { UnregisterHotKey(Handle, 7); UnregisterHotKey(Handle, 8); }
                if (settingsWindow != null) settingsWindow.Dispose();
                if (sessionWindow != null) sessionWindow.Dispose();
                Microsoft.Win32.SystemEvents.PowerModeChanged -= PowerChanged;
                if (channel != null) channel.Dispose();
                refresh.Dispose(); tray.Visible = false; tray.ContextMenuStrip.Dispose(); tray.Dispose();
                audio.Dispose(); notifications.Dispose(); onIcon.Dispose(); offIcon.Dispose(); warningIcon.Dispose();
            }
            base.Dispose(disposing);
        }
        [DllImport("user32.dll")] static extern bool DestroyIcon(IntPtr icon);
        internal static Icon MakeIcon(int mode) {
            return Brand.CreateIcon(mode, 32);
        }
    }
}
