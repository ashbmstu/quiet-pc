using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace QuietPC {
    internal static class Brand {
        internal static readonly Color Ink = Color.FromArgb(32, 35, 50);
        internal static readonly Color Muted = Color.FromArgb(98, 105, 122);
        internal static readonly Color Canvas = Color.FromArgb(232, 236, 242);
        internal static readonly Color Surface = Color.FromArgb(235, 238, 244);
        internal static readonly Color Accent = Color.FromArgb(91, 74, 209);
        internal static readonly Color AccentLight = Color.FromArgb(238, 235, 252);
        internal static readonly Color Border = Color.FromArgb(223, 226, 235);
        internal static readonly Color Warning = Color.FromArgb(168, 102, 0);
        internal static readonly Color Success = Color.FromArgb(34, 110, 75);
        [DllImport("user32.dll")] static extern bool DestroyIcon(IntPtr icon);

        // Each state has a distinct shape as well as a distinct color.
        internal static Icon CreateIcon(int mode, int size) {
            if (size < 16 || size > 256) throw new ArgumentOutOfRangeException("size");
            using (var bitmap = new Bitmap(size, size)) {
                using (var g = Graphics.FromImage(bitmap)) {
                    g.SmoothingMode = SmoothingMode.AntiAlias;
                    g.ScaleTransform(size / 32f, size / 32f);
                    Color fill = mode == 1 ? Accent : mode == 2 ? Warning : Muted;
                    using (var brush = new SolidBrush(fill)) {
                        g.FillEllipse(brush, 1, 1, 30, 30);
                        if (mode == 2) {
                            using (var pen = new Pen(Color.White, 3.2f)) {
                                pen.StartCap = LineCap.Round; pen.EndCap = LineCap.Round;
                                g.DrawLine(pen, 16, 8, 16, 18);
                            }
                            g.FillEllipse(Brushes.White, 14.2f, 22, 3.6f, 3.6f);
                        } else {
                            g.FillEllipse(Brushes.White, 7, 6, 19, 20);
                            g.FillEllipse(brush, 14, 3, 17, 19);
                            if (mode == 0) {
                                using (var outline = new Pen(fill, 5)) g.DrawLine(outline, 7, 26, 26, 7);
                                using (var slash = new Pen(Color.White, 2.4f)) {
                                    slash.StartCap = LineCap.Round; slash.EndCap = LineCap.Round;
                                    g.DrawLine(slash, 7, 26, 26, 7);
                                }
                            }
                        }
                    }
                }
                IntPtr handle = bitmap.GetHicon();
                try { return (Icon)Icon.FromHandle(handle).Clone(); }
                finally { DestroyIcon(handle); }
            }
        }
        internal static void StylePrimary(Button button) {
            StyleButton(button);
            var neo = button as NeoButton;
            if (neo != null) { neo.Primary = true; return; }
            button.BackColor = SystemInformation.HighContrast ? SystemColors.Highlight : Accent;
            button.ForeColor = SystemInformation.HighContrast ? SystemColors.HighlightText : Color.White;
            button.FlatAppearance.BorderSize = 0;
        }
        internal static void StyleSecondary(Button button) {
            StyleButton(button);
            var neo = button as NeoButton;
            if (neo != null) { neo.Primary = false; return; }
            button.BackColor = SystemInformation.HighContrast ? SystemColors.Control : Surface;
            button.ForeColor = SystemInformation.HighContrast ? SystemColors.ControlText : Ink;
            button.FlatAppearance.BorderColor = SystemInformation.HighContrast ? SystemColors.WindowText : Border;
        }
        static void StyleButton(Button button) {
            button.AutoSize = true; button.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            button.MinimumSize = new Size(92, 44); button.Padding = new Padding(14, 8, 14, 8);
            if (button is NeoButton) return;
            button.FlatStyle = FlatStyle.Flat; button.UseVisualStyleBackColor = false;
        }
    }

    // Inherits Button keyboard activation, accessibility and dialog behavior.
    internal sealed class NeoButton : Button {
        bool primary, hovered, pressed;
        internal bool Primary {
            get { return primary; }
            set { primary = value; Invalidate(); }
        }
        internal NeoButton() {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            FlatStyle = FlatStyle.Standard;
            BackColor = SystemInformation.HighContrast ? SystemColors.Control : Brand.Canvas;
            ForeColor = SystemInformation.HighContrast ? SystemColors.ControlText : Brand.Ink;
            MinimumSize = new Size(72, 44);
            AccessibleRole = AccessibleRole.PushButton;
        }
        protected override void OnMouseEnter(EventArgs e) { hovered = true; base.OnMouseEnter(e); Invalidate(); }
        protected override void OnMouseLeave(EventArgs e) { hovered = false; base.OnMouseLeave(e); Invalidate(); }
        protected override void OnMouseDown(MouseEventArgs e) {
            if (e.Button == MouseButtons.Left) pressed = true;
            base.OnMouseDown(e); Invalidate();
        }
        protected override void OnMouseUp(MouseEventArgs e) { pressed = false; base.OnMouseUp(e); Invalidate(); }
        protected override void OnMouseCaptureChanged(EventArgs e) {
            if (!Capture) pressed = false;
            base.OnMouseCaptureChanged(e); Invalidate();
        }
        protected override void OnKeyDown(KeyEventArgs e) {
            if (e.KeyCode == Keys.Space) pressed = true;
            base.OnKeyDown(e); Invalidate();
        }
        protected override void OnKeyUp(KeyEventArgs e) { pressed = false; base.OnKeyUp(e); Invalidate(); }
        protected override void OnGotFocus(EventArgs e) { base.OnGotFocus(e); Invalidate(); }
        protected override void OnLostFocus(EventArgs e) { pressed = false; base.OnLostFocus(e); Invalidate(); }
        protected override void OnEnabledChanged(EventArgs e) { base.OnEnabledChanged(e); Invalidate(); }
        protected override void OnSystemColorsChanged(EventArgs e) {
            base.OnSystemColorsChanged(e);
            BackColor = SystemInformation.HighContrast ? SystemColors.Control : Brand.Canvas;
            ForeColor = SystemInformation.HighContrast ? SystemColors.ControlText : Brand.Ink;
            Invalidate();
        }
        protected override void OnPaint(PaintEventArgs e) {
            if (SystemInformation.HighContrast) { base.OnPaint(e); return; }
            Graphics g = e.Graphics;
            g.Clear(Parent == null ? Brand.Canvas : Parent.BackColor);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            float unit = g.DpiX / 96f;
            RectangleF body = new RectangleF(6 * unit, 6 * unit, Width - 12 * unit - 1, Height - 12 * unit - 1);
            if (body.Width <= 0 || body.Height <= 0) return;
            bool down = Enabled && pressed;
            // Several translucent silhouettes soften each opposing shadow.
            if (!down) for (int layer = 4; layer >= 1; layer--) {
                float offset = layer * unit;
                RectangleF light = body; light.Offset(-offset, -offset);
                RectangleF shade = body; shade.Offset(offset, offset);
                using (var path = Rounded(light, 9 * unit))
                using (var brush = new SolidBrush(Color.FromArgb(42, Color.White))) g.FillPath(brush, path);
                using (var path = Rounded(shade, 9 * unit))
                using (var brush = new SolidBrush(Color.FromArgb(28, 166, 178, 198))) g.FillPath(brush, path);
            }
            using (var path = Rounded(body, 9 * unit)) {
                Color top = down ? Color.FromArgb(211, 218, 230) : hovered ? Color.FromArgb(245, 247, 250) : Brand.Surface;
                Color bottom = down ? Color.FromArgb(240, 243, 248) : Brand.Canvas;
                using (var brush = new LinearGradientBrush(body, top, bottom, 90f)) g.FillPath(brush, path);
                using (var border = new Pen(down ? Color.FromArgb(182, 193, 211) : Color.FromArgb(242, 245, 249), unit))
                    g.DrawPath(border, path);
                if (Focused && ShowFocusCues) {
                    using (var focus = new Pen(Brand.Accent, 1.5f * unit)) { focus.DashStyle = DashStyle.Dot; g.DrawPath(focus, path); }
                }
            }
            Rectangle textBounds = Rectangle.Round(body);
            if (down) textBounds.Offset(0, (int)Math.Ceiling(unit));
            Color text = !Enabled ? SystemColors.GrayText : primary ? Brand.Accent : Brand.Ink;
            TextFormatFlags flags = TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter |
                TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis;
            if (!ShowKeyboardCues) flags |= TextFormatFlags.HidePrefix;
            TextRenderer.DrawText(g, Text, Font, textBounds, text, flags);
        }
        static GraphicsPath Rounded(RectangleF rect, float radius) {
            float d = Math.Min(radius * 2, Math.Min(rect.Width, rect.Height));
            var path = new GraphicsPath();
            path.AddArc(rect.X, rect.Y, d, d, 180, 90);
            path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
            path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
            path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
            path.CloseFigure(); return path;
        }
    }
}
