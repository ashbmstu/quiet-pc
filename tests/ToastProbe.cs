using System;
using Windows.UI.Notifications;
class ToastProbe {
    [STAThread] static void Main() {
        var notifier = ToastNotificationManager.CreateToastNotifier("Chrome");
        Console.WriteLine(notifier.Setting);
    }
}
