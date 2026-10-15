using System.IO;
using System.Drawing.Imaging;
using QuietPC;
class IconMaker {
    static void Main(string[] args) {
        using (var icon = TrayWindow.MakeIcon(1)) {
            using (var stream = File.Create(args[0])) icon.Save(stream);
            using (var image = icon.ToBitmap()) image.Save(args[1], ImageFormat.Png);
        }
    }
}
