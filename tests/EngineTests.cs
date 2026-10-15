using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web.Script.Serialization;
using QuietPC;

class Tests {
    static int passed;
    static void Assert(bool condition, string message) { if (!condition) throw new Exception(message); }
    sealed class FakeAudio : IAudioControl {
        public List<AudioDevice> Devices = new List<AudioDevice> {
            new AudioDevice { Id = "speaker", Muted = false, Volume = .42f },
            new AudioDevice { Id = "headset", Muted = true, Volume = .19f }
        };
        public bool FailRestore, FailMute;
        public List<AudioDevice> Read() { return Devices.Select(d => new AudioDevice { Id = d.Id, Muted = d.Muted, Volume = d.Volume }).ToList(); }
        public void SetMute(string id, bool mute) {
            if (!mute && FailRestore) throw new IOException("Test restoration error");
            if (mute && FailMute) throw new IOException("Test mute error");
            Devices.Single(d => d.Id == id).Muted = mute;
        }
    }
    sealed class FakeNotifications : INotifications {
        public long Value = 1;
        public bool Fail;
        public long Get() { return Value; }
        public void Set(long value) { if (Fail) throw new IOException("Test notification failure"); Value = value; }
    }
    sealed class MemoryStore : IStateStore {
        string json = new JavaScriptSerializer().Serialize(new SavedState());
        public bool Fail;
        public SavedState Read() { return new JavaScriptSerializer().Deserialize<SavedState>(json); }
        public void Write(SavedState state) { if (Fail) throw new IOException("Test disk error"); json = new JavaScriptSerializer().Serialize(state); }
    }
    static void Case(string name, Action test) { test(); passed++; Console.WriteLine("PASS " + name); }
    [STAThread] static int Main(string[] args) {
        try {
            Case("Mixed initial mute states and volume levels restore exactly", delegate {
                var a = new FakeAudio(); var n = new FakeNotifications(); var e = new QuietEngine(a, n, new MemoryStore());
                e.Enable(); Assert(a.Devices.All(d => d.Muted) && n.Value == 0, "Enable failed");
                e.Disable(); Assert(!a.Devices[0].Muted && a.Devices[1].Muted && n.Value == 1, "Restore failed");
                Assert(a.Devices[0].Volume == .42f && a.Devices[1].Volume == .19f, "Volume changed");
            });
            Case("Notifications already disabled remain disabled", delegate {
                var n = new FakeNotifications { Value = 0 }; var e = new QuietEngine(new FakeAudio(), n, new MemoryStore());
                e.Enable(); e.Disable(); Assert(n.Value == 0, "Notifications unexpectedly enabled");
            });
            Case("New device is muted; disconnected device restores on return", delegate {
                var a = new FakeAudio(); var e = new QuietEngine(a, new FakeNotifications(), new MemoryStore());
                e.Enable(); var usb = new AudioDevice { Id = "usb", Muted = false, Volume = .3f }; a.Devices.Add(usb); e.Refresh();
                Assert(usb.Muted, "New device not muted"); a.Devices.Remove(usb); e.Disable();
                Assert(e.State.Devices.ContainsKey("usb"), "Disconnected restoration lost");
                a.Devices.Add(usb); e.Refresh(); Assert(!usb.Muted && e.State.Devices.Count == 0, "Reconnect restoration failed");
            });
            Case("Restart during quiet mode retains original settings", delegate {
                var a = new FakeAudio(); var n = new FakeNotifications(); var store = new MemoryStore();
                var e = new QuietEngine(a, n, store); e.Enable();
                e = new QuietEngine(a, n, store); e.Refresh(); e.Disable();
                Assert(!a.Devices[0].Muted && a.Devices[1].Muted && n.Value == 1, "Recovery lost original values");
            });
            Case("Failure to save prevents system mutation", delegate {
                var a = new FakeAudio(); var n = new FakeNotifications(); var store = new MemoryStore { Fail = true };
                var e = new QuietEngine(a, n, store); bool threw = false;
                try { e.Enable(); } catch (IOException) { threw = true; }
                Assert(threw && !a.Devices[0].Muted && n.Value == 1 && !e.State.Active, "Unsafe unsaved change");
            });
            Case("Partial restoration is retained and retried", delegate {
                var a = new FakeAudio(); var n = new FakeNotifications(); var store = new MemoryStore(); var e = new QuietEngine(a, n, store);
                e.Enable(); a.FailRestore = true; e.Disable();
                Assert(e.Error.Length > 0 && e.State.Devices.ContainsKey("speaker"), "Failed restore discarded");
                a.FailRestore = false; e = new QuietEngine(a, n, store); e.Refresh();
                Assert(!a.Devices[0].Muted && e.State.Devices.Count == 0, "Restore retry failed");
            });
            Case("Notification failure is reported and recoverable", delegate {
                var a = new FakeAudio(); var n = new FakeNotifications { Fail = true }; var e = new QuietEngine(a, n, new MemoryStore());
                e.Enable(); Assert(e.Error.Length > 0 && a.Devices.All(d => d.Muted), "Partial failure hidden");
                n.Fail = false; e.Refresh(); Assert(e.Error.Length == 0 && n.Value == 0, "Retry failed"); e.Disable();
            });
            Case("Atomic state file survives reload", delegate {
                string dir = Path.Combine(Path.GetTempPath(), "QuietPC-test-" + Guid.NewGuid().ToString("N"));
                var store = new StateStore(Path.Combine(dir, "state.json")); var state = new SavedState { Active = true, Notifications = 1 };
                state.Devices.Add("test", false); store.Write(state); state.Active = false; store.Write(state);
                Assert(!store.Read().Active && !store.Read().Devices["test"], "State persistence failed");
                foreach (var file in Directory.GetFiles(dir)) File.Delete(file); Directory.Delete(dir);
            });
            Case("Audio-only mode leaves notifications untouched", delegate {
                var a = new FakeAudio(); var n = new FakeNotifications(); var e = new QuietEngine(a, n, new MemoryStore());
                e.SuppressNotifications = false; e.Enable();
                Assert(n.Value == 1 && a.Devices.All(d => d.Muted), "Audio-only mode changed notifications");
                e.Disable(); Assert(n.Value == 1 && !a.Devices[0].Muted, "Audio-only restore failed");
            });
            Case("Notifications-only mode leaves audio untouched", delegate {
                var a = new FakeAudio(); var n = new FakeNotifications(); var e = new QuietEngine(a, n, new MemoryStore());
                e.MuteAudio = false; e.Enable(); e.Refresh();
                Assert(n.Value == 0 && !a.Devices[0].Muted && a.Devices[1].Muted, "Notifications-only changed audio");
                e.Disable(); Assert(n.Value == 1 && !a.Devices[0].Muted, "Notifications-only restore failed");
            });
            Case("Settings changed while active apply only to the next session", delegate {
                var a = new FakeAudio(); var n = new FakeNotifications(); var store = new MemoryStore(); var e = new QuietEngine(a, n, store);
                e.Enable(); e.MuteAudio = false; e.Refresh(); Assert(a.Devices.All(d => d.Muted), "Active mode changed unexpectedly");
                e.Disable(); Assert(!a.Devices[0].Muted, "Original audio not restored");
                e.Enable(); Assert(!a.Devices[0].Muted && n.Value == 0, "New settings not applied");
                e = new QuietEngine(a, n, store); e.Refresh(); Assert(!a.Devices[0].Muted, "Restart ignored active session options"); e.Disable();
            });
            Case("Empty suppression choice cannot start a session", delegate {
                var a = new FakeAudio(); var n = new FakeNotifications(); var e = new QuietEngine(a, n, new MemoryStore());
                e.MuteAudio = e.SuppressNotifications = false; bool threw = false;
                try { e.Enable(); } catch (InvalidOperationException) { threw = true; }
                Assert(threw && !e.State.Active && n.Value == 1 && !a.Devices[0].Muted, "Invalid session applied");
            });
            Case("Timed quiet expires at the deadline and restores the snapshot", delegate {
                var now = new DateTimeOffset(2026, 1, 1, 10, 0, 0, TimeSpan.Zero);
                var a = new FakeAudio(); var n = new FakeNotifications(); var e = new QuietEngine(a, n, new MemoryStore(), () => now);
                e.EnableFor(TimeSpan.FromMinutes(30)); now = now.AddMinutes(29); e.Refresh();
                Assert(e.State.Active && n.Value == 0 && a.Devices.All(d => d.Muted), "Expired early");
                now = now.AddMinutes(1); e.Refresh();
                Assert(!e.State.Active && !e.EndsAt.HasValue && n.Value == 1 && !a.Devices[0].Muted && a.Devices[1].Muted, "Deadline did not restore originals");
            });
            Case("Changing a running timer never replaces the original snapshot", delegate {
                var now = DateTimeOffset.UtcNow; var a = new FakeAudio(); var n = new FakeNotifications();
                var e = new QuietEngine(a, n, new MemoryStore(), () => now);
                e.EnableFor(TimeSpan.FromMinutes(30)); now = now.AddMinutes(10); e.EnableFor(TimeSpan.FromHours(1));
                Assert(e.EndsAt == now.AddHours(1), "Timer not replaced from current time");
                now = now.AddHours(1); e.Refresh(); Assert(!a.Devices[0].Muted && n.Value == 1, "Replaced timer captured muted state");
            });
            Case("Timer expiry after restart or sleep restores immediately", delegate {
                var now = DateTimeOffset.UtcNow; var a = new FakeAudio(); var n = new FakeNotifications(); var store = new MemoryStore();
                var e = new QuietEngine(a, n, store, () => now); e.EnableFor(TimeSpan.FromHours(1)); now = now.AddHours(2);
                e = new QuietEngine(a, n, store, () => now); e.Refresh();
                Assert(!e.State.Active && n.Value == 1 && !a.Devices[0].Muted, "Overdue recovered timer still muted");
            });
            Case("Cancel timer keeps quiet on until manually restored", delegate {
                var now = DateTimeOffset.UtcNow; var a = new FakeAudio(); var n = new FakeNotifications();
                var e = new QuietEngine(a, n, new MemoryStore(), () => now); e.EnableFor(TimeSpan.FromMinutes(30)); e.CancelTimer();
                now = now.AddHours(2); e.Refresh(); Assert(e.State.Active && !e.EndsAt.HasValue && n.Value == 0, "Cancel timer restored too early");
                e.Disable(); Assert(n.Value == 1 && !a.Devices[0].Muted, "Cancel timer lost restoration");
            });
            Case("Manual off removes timer without a later surprise toggle", delegate {
                var now = DateTimeOffset.UtcNow; var a = new FakeAudio(); var n = new FakeNotifications(); var e = new QuietEngine(a, n, new MemoryStore(), () => now);
                e.EnableFor(TimeSpan.FromMinutes(1)); e.Disable(); now = now.AddHours(1); e.Refresh();
                Assert(!e.State.Active && !e.State.EndsAtUtcTicks.HasValue && !a.Devices[0].Muted && n.Value == 1, "Old timer remains");
            });
            Case("Expired timer retains restoration for a disconnected device", delegate {
                var now = DateTimeOffset.UtcNow; var a = new FakeAudio(); var n = new FakeNotifications(); var e = new QuietEngine(a, n, new MemoryStore(), () => now);
                var speaker = a.Devices[0]; e.EnableFor(TimeSpan.FromMinutes(1)); a.Devices.Remove(speaker); now = now.AddMinutes(2); e.Refresh();
                Assert(!e.State.Active && e.State.Devices.ContainsKey(speaker.Id), "Pending restoration lost at expiry");
                a.Devices.Add(speaker); e.Refresh(); Assert(!speaker.Muted, "Reconnect after expiry did not restore");
            });
            Case("Timer update write failure retains the old deadline", delegate {
                var now = DateTimeOffset.UtcNow; var store = new MemoryStore(); var e = new QuietEngine(new FakeAudio(), new FakeNotifications(), store, () => now);
                e.EnableFor(TimeSpan.FromMinutes(30)); var old = e.EndsAt; store.Fail = true; bool threw = false;
                try { e.EnableFor(TimeSpan.FromHours(1)); } catch (IOException) { threw = true; }
                Assert(threw && e.EndsAt == old, "Unsaved timer applied");
            });
            Case("Custom duration bounds reject zero and over seven days", delegate {
                var e = new QuietEngine(new FakeAudio(), new FakeNotifications(), new MemoryStore()); int rejected = 0;
                foreach (var duration in new[] { TimeSpan.Zero, TimeSpan.FromDays(8) }) try { e.EnableFor(duration); } catch (ArgumentOutOfRangeException) { rejected++; }
                Assert(rejected == 2 && !e.State.Active, "Invalid duration started quiet");
            });
            Case("Manual unmute is adopted without changing notifications or other outputs", delegate {
                var a = new FakeAudio(); var n = new FakeNotifications(); var e = new QuietEngine(a, n, new MemoryStore());
                e.Enable(); a.Devices[1].Muted = false; e.Refresh(); e.Refresh();
                Assert(!a.Devices[1].Muted && a.Devices[0].Muted && n.Value == 0 && e.ManualAudioDevices == 1, "Manual unmute was overridden");
                e.Disable(); Assert(!a.Devices[1].Muted && n.Value == 1, "Restoration undid manual choice");
            });
            Case("Unmute immediately before expiry is preserved", delegate {
                var now = DateTimeOffset.UtcNow; var a = new FakeAudio(); var n = new FakeNotifications();
                var e = new QuietEngine(a, n, new MemoryStore(), () => now); e.EnableFor(TimeSpan.FromMinutes(1));
                a.Devices[1].Muted = false; now = now.AddMinutes(1); e.Refresh();
                Assert(!a.Devices[1].Muted && !e.State.Active && n.Value == 1, "Expiry overwrote an unobserved unmute");
            });
            Case("Manual audio ownership survives restart and later remuting", delegate {
                var a = new FakeAudio(); var n = new FakeNotifications(); var store = new MemoryStore();
                var e = new QuietEngine(a, n, store); e.Enable(); a.Devices[0].Muted = false; e.Refresh();
                a.Devices[0].Muted = true; e = new QuietEngine(a, n, store); e.Refresh(); e.Disable();
                Assert(a.Devices[0].Muted && n.Value == 1, "User remute was lost after restart");
            });
            Case("Unmute while controller is stopped is adopted on restart", delegate {
                var a = new FakeAudio(); var n = new FakeNotifications(); var store = new MemoryStore();
                var e = new QuietEngine(a, n, store); e.Enable(); a.Devices[1].Muted = false;
                e = new QuietEngine(a, n, store); e.Refresh(); e.Disable();
                Assert(!a.Devices[1].Muted, "Restart re-muted the output");
            });
            Case("Manual override survives reconnect and timer replacement; new session resets ownership", delegate {
                var a = new FakeAudio(); var n = new FakeNotifications(); var e = new QuietEngine(a, n, new MemoryStore());
                e.Enable(); var speaker = a.Devices[0]; speaker.Muted = false; e.Refresh();
                a.Devices.Remove(speaker); e.EnableFor(TimeSpan.FromHours(1)); a.Devices.Add(speaker); e.Refresh();
                Assert(!speaker.Muted, "Reconnect or timer replacement reclaimed output");
                var usb = new AudioDevice { Id = "usb", Muted = false }; a.Devices.Add(usb); e.Refresh();
                Assert(usb.Muted, "New output was not muted");
                e.Disable(); e.Enable(); Assert(speaker.Muted && e.ManualAudioDevices == 0, "New session failed to regain audio control");
            });
            Case("Disconnected manual output does not block restoration", delegate {
                var a = new FakeAudio(); var e = new QuietEngine(a, new FakeNotifications(), new MemoryStore());
                e.Enable(); var headset = a.Devices[1]; headset.Muted = false; e.Refresh(); a.Devices.Remove(headset); e.Disable();
                Assert(e.State.Devices.Count == 0, "Manual output wrongly requires restoration");
            });
            Case("Failed save of manual choice never causes remuting and is retried", delegate {
                var a = new FakeAudio(); var n = new FakeNotifications(); var store = new MemoryStore();
                var e = new QuietEngine(a, n, store); e.Enable(); store.Fail = true; a.Devices[1].Muted = false; e.Refresh();
                Assert(!a.Devices[1].Muted && e.Error.Length > 0, "Failed save remuted audio or hid error");
                store.Fail = false; e.Refresh(); e = new QuietEngine(a, n, store); e.Disable();
                Assert(!a.Devices[1].Muted && e.Error.Length == 0, "Manual choice save was not retried");
            });
            Case("Failed initial mute is retried, not mistaken for manual unmute", delegate {
                var a = new FakeAudio { FailMute = true }; var e = new QuietEngine(a, new FakeNotifications(), new MemoryStore());
                e.Enable(); Assert(e.Error.Length > 0 && e.ManualAudioDevices == 0, "Mute failure treated as manual");
                a.FailMute = false; e.Refresh(); Assert(a.Devices[0].Muted && e.Error.Length == 0, "Initial mute not retried");
            });
            Case("Older recovery files adopt unmute during upgrade", delegate {
                var a = new FakeAudio(); var store = new MemoryStore();
                store.Write(new SavedState { Active = true, Devices = new Dictionary<string, bool> { { "speaker", true }, { "headset", true } } });
                var e = new QuietEngine(a, new FakeNotifications(), store); e.Refresh(); e.Disable();
                Assert(!a.Devices[0].Muted, "Legacy session overwrote external unmute");
            });
            if (args.Contains("--native")) Case("Live Windows notifications and every playback endpoint round trip", delegate {
                using (var a = new AudioControl()) using (var n = new NotificationControl()) {
                    var before = a.Read(); long prior = n.Get();
                    string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "native-test-state.json");
                    var e = new QuietEngine(a, n, new StateStore(path));
                    try {
                        e.Enable(); Assert(e.Error.Length == 0, e.Error);
                        Assert(n.Get() == 0 && a.Read().All(d => d.Muted), "Live mute failed");
                        e = new QuietEngine(a, n, new StateStore(path)); // Simulate a process restart.
                        e.Refresh(); Assert(e.State.Active, "Live recovery failed");
                    } finally { e.Disable(); }
                    Assert(e.Error.Length == 0 && n.Get() == prior, "Live notification restore failed: " + e.Error);
                    var after = a.Read();
                    foreach (var d in before) {
                        var restored = after.Single(x => x.Id == d.Id);
                        Assert(d.Muted == restored.Muted && Math.Abs(d.Volume - restored.Volume) < .00001, "Live device restore mismatch");
                    }
                    File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "native-test-result.json"), new JavaScriptSerializer().Serialize(new { Passed = true, PlaybackDevices = before.Count, NotificationsBefore = prior, NotificationsAfter = n.Get(), Before = before, After = after }));
                    Console.WriteLine("Verified " + before.Count + " playback devices.");
                }
            });
            Console.WriteLine(passed + " tests passed."); return 0;
        } catch (Exception e) { Console.Error.WriteLine(e); return 1; }
    }
}
