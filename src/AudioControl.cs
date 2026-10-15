using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace QuietPC {
    [ComImport, Guid("BCDE0395-E52F-467C-8E3D-C4579291692E")] class MMDeviceEnumerator { }
    [ComImport, Guid("A95664D2-9614-4F35-A746-DE8DB63617E6"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    interface IMMDeviceEnumerator {
        void EnumAudioEndpoints(int flow, uint state, out IMMDeviceCollection devices);
        void GetDefaultAudioEndpoint(int flow, int role, out IMMDevice device);
        void GetDevice([MarshalAs(UnmanagedType.LPWStr)] string id, out IMMDevice device);
        void RegisterEndpointNotificationCallback(IMMNotificationClient client);
        void UnregisterEndpointNotificationCallback(IMMNotificationClient client);
    }
    [ComImport, Guid("0BD7A1BE-7A1A-44DB-8397-CC5392387B5E"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    interface IMMDeviceCollection { void GetCount(out uint count); void Item(uint index, out IMMDevice device); }
    [ComImport, Guid("D666063F-1587-4E43-81F1-B948E807363F"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    interface IMMDevice {
        void Activate(ref Guid iid, uint context, IntPtr parameters, [MarshalAs(UnmanagedType.IUnknown)] out object obj);
        void OpenPropertyStore(uint mode, out IntPtr store);
        void GetId([MarshalAs(UnmanagedType.LPWStr)] out string id);
        void GetState(out uint state);
    }
    [ComImport, Guid("5CDF2C82-841E-4546-9722-0CF74078229A"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    interface IAudioEndpointVolume {
        void RegisterControlChangeNotify(IntPtr callback);
        void UnregisterControlChangeNotify(IntPtr callback);
        void GetChannelCount(out uint count);
        void SetMasterVolumeLevel(float level, ref Guid context);
        void SetMasterVolumeLevelScalar(float level, ref Guid context);
        void GetMasterVolumeLevel(out float level);
        void GetMasterVolumeLevelScalar(out float level);
        void SetChannelVolumeLevel(uint channel, float level, ref Guid context);
        void SetChannelVolumeLevelScalar(uint channel, float level, ref Guid context);
        void GetChannelVolumeLevel(uint channel, out float level);
        void GetChannelVolumeLevelScalar(uint channel, out float level);
        void SetMute([MarshalAs(UnmanagedType.Bool)] bool mute, ref Guid context);
        void GetMute([MarshalAs(UnmanagedType.Bool)] out bool mute);
    }
    [StructLayout(LayoutKind.Sequential)] public struct PropertyKey { public Guid Format; public uint Id; }
    [ComVisible(true), Guid("7991EEC9-7E89-4D85-8390-6C703CEC60C0"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IMMNotificationClient {
        [PreserveSig] int OnDeviceStateChanged([MarshalAs(UnmanagedType.LPWStr)] string id, uint state);
        [PreserveSig] int OnDeviceAdded([MarshalAs(UnmanagedType.LPWStr)] string id);
        [PreserveSig] int OnDeviceRemoved([MarshalAs(UnmanagedType.LPWStr)] string id);
        [PreserveSig] int OnDefaultDeviceChanged(int flow, int role, [MarshalAs(UnmanagedType.LPWStr)] string id);
        [PreserveSig] int OnPropertyValueChanged([MarshalAs(UnmanagedType.LPWStr)] string id, PropertyKey key);
    }
    [ComVisible(true), ClassInterface(ClassInterfaceType.None)]
    public sealed class DeviceObserver : IMMNotificationClient {
        readonly Action changed;
        public DeviceObserver(Action changed) { this.changed = changed; }
        int Changed() { try { changed(); } catch { } return 0; }
        public int OnDeviceStateChanged(string id, uint state) { return Changed(); }
        public int OnDeviceAdded(string id) { return Changed(); }
        public int OnDeviceRemoved(string id) { return Changed(); }
        public int OnDefaultDeviceChanged(int flow, int role, string id) { return Changed(); }
        public int OnPropertyValueChanged(string id, PropertyKey key) { return Changed(); }
    }
    public sealed class AudioDevice {
        public string Id { get; set; }
        public bool Muted { get; set; }
        public float Volume { get; set; }
    }
    public interface IAudioControl { List<AudioDevice> Read(); void SetMute(string id, bool muted); }
    public sealed class AudioControl : IAudioControl, IDisposable {
        IMMDeviceEnumerator enumerator = (IMMDeviceEnumerator)new MMDeviceEnumerator();
        DeviceObserver observer;
        static readonly Guid Context = new Guid("68F3FA44-37AF-46CC-9276-F58108692F9F");
        public void Observe(Action changed) {
            observer = new DeviceObserver(changed);
            enumerator.RegisterEndpointNotificationCallback(observer);
        }
        static IAudioEndpointVolume Volume(IMMDevice device) {
            Guid iid = typeof(IAudioEndpointVolume).GUID;
            object obj; device.Activate(ref iid, 23, IntPtr.Zero, out obj);
            return (IAudioEndpointVolume)obj;
        }
        public List<AudioDevice> Read() {
            var result = new List<AudioDevice>();
            IMMDeviceCollection collection;
            enumerator.EnumAudioEndpoints(0, 1, out collection); // Playback only; active devices.
            try {
                uint count; collection.GetCount(out count);
                for (uint i = 0; i < count; i++) {
                    IMMDevice device; collection.Item(i, out device);
                    try {
                        string id; device.GetId(out id);
                        var volume = Volume(device);
                        try {
                            bool muted; float level; volume.GetMute(out muted); volume.GetMasterVolumeLevelScalar(out level);
                            result.Add(new AudioDevice { Id = id, Muted = muted, Volume = level });
                        } finally { Marshal.ReleaseComObject(volume); }
                    } finally { Marshal.ReleaseComObject(device); }
                }
            } finally { Marshal.ReleaseComObject(collection); }
            return result;
        }
        public void SetMute(string id, bool muted) {
            IMMDevice device; enumerator.GetDevice(id, out device);
            try {
                var volume = Volume(device);
                try {
                    Guid context = Context; volume.SetMute(muted, ref context);
                    bool actual; volume.GetMute(out actual);
                    if (actual != muted) throw new InvalidOperationException("An audio device did not apply mute.");
                } finally { Marshal.ReleaseComObject(volume); }
            } finally { Marshal.ReleaseComObject(device); }
        }
        public void Dispose() {
            if (enumerator == null) return;
            if (observer != null) { try { enumerator.UnregisterEndpointNotificationCallback(observer); } catch { } }
            Marshal.ReleaseComObject(enumerator); enumerator = null; observer = null;
        }
    }
}
