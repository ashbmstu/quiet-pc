using System;
using System.Runtime.InteropServices;

namespace QuietPC {
    // Windows internal notification settings interface. Verified on build 26100.
    // Source for ABI: https://learn.microsoft.com/en-us/answers/questions/2154912/how-to-enable-disable-windows-notification-using-c
    [ComImport, Guid("443b1739-3779-4aba-953b-9e4ff84dd4b6"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    interface INotificationSettings {
        void GetSettingsForAumid([MarshalAs(UnmanagedType.LPWStr)] string id, out IntPtr settings);
        void GetGlobalSetting(uint setting, out long value);
        void SetGlobalSetting(uint setting, long value, uint source);
        void IsGlobalSettingRestrictedByAdmin(uint setting, out long restricted);
    }

    public sealed class NotificationControl : INotifications, IDisposable {
        INotificationSettings settings;
        [DllImport("ole32.dll", ExactSpelling=true)]
        static extern int CoCreateInstance(ref Guid clsid, IntPtr outer, uint context, ref Guid iid, [MarshalAs(UnmanagedType.Interface)] out INotificationSettings instance);
        public NotificationControl() {
            Guid clsid = new Guid("D18705BE-FC2F-44C8-AEFF-1CD49AEA8FC1");
            Guid iid = typeof(INotificationSettings).GUID;
            Marshal.ThrowExceptionForHR(CoCreateInstance(ref clsid, IntPtr.Zero, 4, ref iid, out settings));
        }
        public long Get() { long value; settings.GetGlobalSetting(0, out value); return value; }
        public void Set(long value) {
            settings.SetGlobalSetting(0, value, 0);
            if (Get() != value) throw new InvalidOperationException("Windows did not apply the notification setting.");
        }
        public void Dispose() { if (settings != null) { Marshal.FinalReleaseComObject(settings); settings = null; } }
    }
}
