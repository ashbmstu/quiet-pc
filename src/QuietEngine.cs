using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Web.Script.Serialization;

namespace QuietPC {
    public interface INotifications { long Get(); void Set(long value); }
    public sealed class SavedState {
        public int Version { get; set; }
        public bool Active { get; set; }
        public bool MuteAudio { get; set; }
        public bool RestoreNotifications { get; set; }
        public long Notifications { get; set; }
        public long? EndsAtUtcTicks { get; set; }
        public Dictionary<string, bool> Devices { get; set; }
        public SavedState() { Version = 1; MuteAudio = true; Devices = new Dictionary<string, bool>(); }
    }
    public interface IStateStore { SavedState Read(); void Write(SavedState state); }
    public sealed class StateStore : IStateStore {
        readonly string path;
        public StateStore(string path) { this.path = path; }
        public SavedState Read() {
            if (!File.Exists(path)) return new SavedState();
            var state = new JavaScriptSerializer().Deserialize<SavedState>(File.ReadAllText(path));
            if (state == null || state.Version != 1 || state.Devices == null)
                throw new InvalidDataException("Quiet PC recovery state is invalid. The file has been preserved: " + path);
            if (state.EndsAtUtcTicks.HasValue && (state.EndsAtUtcTicks.Value < DateTime.MinValue.Ticks || state.EndsAtUtcTicks.Value > DateTime.MaxValue.Ticks))
                throw new InvalidDataException("Quiet PC timer deadline is invalid. The recovery file has been preserved.");
            return state;
        }
        public void Write(SavedState state) {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            string temp = path + ".new";
            byte[] bytes = Encoding.UTF8.GetBytes(new JavaScriptSerializer().Serialize(state));
            using (var file = new FileStream(temp, FileMode.Create, FileAccess.Write, FileShare.None, 4096, FileOptions.WriteThrough)) {
                file.Write(bytes, 0, bytes.Length); file.Flush(true);
            }
            if (File.Exists(path)) File.Replace(temp, path, path + ".bak");
            else File.Move(temp, path);
        }
    }
    public sealed class QuietEngine {
        readonly IAudioControl audio;
        readonly INotifications notifications;
        readonly IStateStore store;
        readonly Func<DateTimeOffset> clock;
        public SavedState State { get; private set; }
        public string Error { get; private set; }
        public bool SuppressNotifications = true;
        public bool MuteAudio = true;
        public QuietEngine(IAudioControl audio, INotifications notifications, IStateStore store)
            : this(audio, notifications, store, delegate { return DateTimeOffset.UtcNow; }) { }
        public QuietEngine(IAudioControl audio, INotifications notifications, IStateStore store, Func<DateTimeOffset> clock) {
            this.audio = audio; this.notifications = notifications; this.store = store;
            this.clock = clock;
            State = store.Read(); Error = "";
        }
        public void Enable() {
            EnableFor(null);
        }
        public void EnableFor(TimeSpan? duration) {
            if (duration.HasValue && (duration.Value < TimeSpan.FromMinutes(1) || duration.Value > TimeSpan.FromDays(7)))
                throw new ArgumentOutOfRangeException("duration", "Choose a duration from 1 minute to 7 days.");
            long? deadline = duration.HasValue ? (long?)clock().Add(duration.Value).UtcTicks : null;
            if (State.Active) { SaveDeadline(deadline); Refresh(); return; }
            // Clear recoverable leftovers first. Disconnected devices retain their original states.
            Restore();
            if (State.RestoreNotifications) throw new InvalidOperationException(Error);
            if (!SuppressNotifications && !MuteAudio) throw new InvalidOperationException("Select notifications or audio in Settings.");
            var devices = MuteAudio ? audio.Read() : new List<AudioDevice>();
            long prior = SuppressNotifications ? notifications.Get() : 0;
            var snapshot = new SavedState {
                Active = true, Notifications = prior, RestoreNotifications = SuppressNotifications, MuteAudio = MuteAudio, EndsAtUtcTicks = deadline,
                Devices = new Dictionary<string, bool>(State.Devices)
            };
            foreach (var device in devices) if (!snapshot.Devices.ContainsKey(device.Id)) snapshot.Devices.Add(device.Id, device.Muted);
            store.Write(snapshot); // Durable snapshot BEFORE touching any Windows setting.
            State = snapshot;
            Refresh();
        }
        void SaveDeadline(long? deadline) {
            long? before = State.EndsAtUtcTicks;
            State.EndsAtUtcTicks = deadline;
            try { store.Write(State); } catch { State.EndsAtUtcTicks = before; throw; }
        }
        public void CancelTimer() { if (State.Active && State.EndsAtUtcTicks.HasValue) SaveDeadline(null); }
        public DateTimeOffset? EndsAt { get { return State.Active && State.EndsAtUtcTicks.HasValue ? (DateTimeOffset?)new DateTimeOffset(State.EndsAtUtcTicks.Value, TimeSpan.Zero) : null; } }
        public void Disable() {
            if (State.Active) {
                long? deadline = State.EndsAtUtcTicks;
                State.Active = false;
                State.EndsAtUtcTicks = null;
                try { store.Write(State); } catch { State.Active = true; State.EndsAtUtcTicks = deadline; throw; }
            }
            Restore();
        }
        public void Toggle() { if (State.Active) Disable(); else Enable(); }
        public void Refresh() {
            if (!State.Active) { Restore(); return; }
            if (State.EndsAtUtcTicks.HasValue && clock().UtcTicks >= State.EndsAtUtcTicks.Value) { Disable(); return; }
            var errors = new List<string>();
            try { if (State.RestoreNotifications && notifications.Get() != 0) notifications.Set(0); } catch (Exception e) { errors.Add("Notifications: " + e.Message); }
            if (State.MuteAudio) try {
                foreach (var device in audio.Read()) {
                    try {
                        if (!State.Devices.ContainsKey(device.Id)) {
                            State.Devices.Add(device.Id, device.Muted);
                            try { store.Write(State); } catch { State.Devices.Remove(device.Id); throw; }
                        }
                        if (!device.Muted) audio.SetMute(device.Id, true);
                    } catch (Exception e) { errors.Add("Audio: " + e.Message); }
                }
            } catch (Exception e) { errors.Add("Audio devices: " + e.Message); }
            Error = string.Join("\r\n", errors);
        }
        void Restore() {
            var errors = new List<string>();
            if (State.RestoreNotifications) {
                try {
                    notifications.Set(State.Notifications);
                    State.RestoreNotifications = false;
                    try { store.Write(State); } catch { State.RestoreNotifications = true; throw; }
                } catch (Exception e) { errors.Add("Restore notifications: " + e.Message); }
            }
            if (State.Devices.Count > 0) {
                try {
                    var active = new HashSet<string>(audio.Read().Select(d => d.Id));
                    foreach (var pair in State.Devices.ToArray()) {
                        if (!active.Contains(pair.Key)) continue; // Restore when reconnected, even after restart.
                        try {
                            audio.SetMute(pair.Key, pair.Value);
                            State.Devices.Remove(pair.Key);
                            try { store.Write(State); } catch { State.Devices[pair.Key] = pair.Value; throw; }
                        } catch (Exception e) { errors.Add("Restore audio: " + e.Message); }
                    }
                } catch (Exception e) { errors.Add("Restore devices: " + e.Message); }
            }
            Error = string.Join("\r\n", errors);
        }
    }
}
