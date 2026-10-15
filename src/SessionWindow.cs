using System;
using System.Drawing;
using System.Windows.Forms;

namespace QuietPC {
    sealed class SessionWindow : Form {
        readonly QuietEngine engine;
        readonly Action<int> start;
        readonly NumericUpDown minutes;
        readonly Label state, countdown, preview, feedback;
        readonly Button cancel, stop;
        readonly Timer clock;
        readonly ToolTip hints = new ToolTip();
        public SessionWindow(QuietEngine engine, Action<int> start, Action cancelTimer, Action restore) {
            this.engine = engine; this.start = start;
            Text = "Quiet PC timer"; Font = new Font("Segoe UI", 10);
            BackColor = SystemInformation.HighContrast ? SystemColors.Window : Brand.Canvas;
            ForeColor = SystemInformation.HighContrast ? SystemColors.WindowText : Brand.Ink;
            AutoScaleDimensions = new SizeF(96, 96); AutoScaleMode = AutoScaleMode.Dpi;
            ClientSize = new Size(340, 314);
            FormBorderStyle = FormBorderStyle.FixedDialog; MaximizeBox = false; MinimizeBox = false;
            StartPosition = FormStartPosition.CenterScreen; Icon = Brand.CreateIcon(1, 32);
            var layout = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(16), ColumnCount = 1, RowCount = 8 };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            foreach (int height in new[] { 26, 64, 24, 50, 50, 20, 0, 48 }) layout.RowStyles.Add(new RowStyle(SizeType.Absolute, height));
            Controls.Add(layout);
            state = Label(12, FontStyle.Regular); layout.Controls.Add(state, 0, 0);
            countdown = Label(34, FontStyle.Regular); countdown.AccessibleName = "Time remaining"; layout.Controls.Add(countdown, 0, 1);
            preview = Label(10, FontStyle.Regular); layout.Controls.Add(preview, 0, 2);
            var presets = Row();
            presets.Controls.Add(Button("30 min", delegate { Start(30); }), 0, 0);
            presets.Controls.Add(Button("1 hour", delegate { Start(60); }), 1, 0); layout.Controls.Add(presets, 0, 3);
            var custom = Row();
            var entry = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, Padding = new Padding(8, 10, 6, 10), Margin = new Padding(0) };
            entry.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            entry.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100)); entry.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 42));
            minutes = new NumericUpDown { Minimum = 1, Maximum = 10080, Value = 45, Dock = DockStyle.Top, AccessibleName = "Duration in minutes", BorderStyle = BorderStyle.FixedSingle };
            hints.SetToolTip(minutes, "1 minute to 7 days");
            entry.Controls.Add(minutes, 0, 0); entry.Controls.Add(new Label { Text = "min", Dock = DockStyle.Fill, AutoSize = false, TextAlign = ContentAlignment.MiddleLeft, Margin = Padding.Empty, Padding = new Padding(4, 0, 0, 0) }, 1, 0);
            custom.Controls.Add(entry, 0, 0);
            var begin = Button("Start", delegate { Start((int)minutes.Value); }); Brand.StylePrimary(begin); begin.AutoSize = false;
            custom.Controls.Add(begin, 1, 0); AcceptButton = begin; layout.Controls.Add(custom, 0, 4);
            feedback = Label(9, FontStyle.Regular); layout.Controls.Add(feedback, 0, 5);
            var actions = Row();
            cancel = Button("No timer", delegate { Try(cancelTimer); }); hints.SetToolTip(cancel, "Cancel the timer. Keep quiet mode on.");
            cancel.AccessibleName = "Cancel timer, keep quiet on";
            cancel.AccessibleDescription = "Remove the deadline without restoring settings.";
            stop = Button("Restore", delegate { Try(restore); }); hints.SetToolTip(stop, "Turn quiet off and restore your previous settings.");
            stop.AccessibleName = "Restore previous settings";
            actions.Controls.Add(cancel, 0, 0); actions.Controls.Add(stop, 1, 0); layout.Controls.Add(actions, 0, 7);
            clock = new Timer { Interval = 1000 }; clock.Tick += delegate { RefreshStatus(); }; clock.Start(); RefreshStatus();
        }
        Label Label(int size, FontStyle style) {
            return new Label { Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter, Font = new Font("Segoe UI", size, style), Margin = new Padding(0) };
        }
        static TableLayoutPanel Row() {
            var row = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, Margin = new Padding(0) };
            row.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50)); row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50)); return row;
        }
        Button Button(string label, Action action) {
            var button = new NeoButton { Text = label, Dock = DockStyle.Fill, Margin = new Padding(2), TabStop = true };
            Brand.StyleSecondary(button); button.AutoSize = false; button.Click += delegate { action(); }; return button;
        }
        void Start(int duration) { Try(delegate { start(duration); }); }
        void Try(Action action) {
            try { action(); feedback.Text = ""; }
            catch (Exception e) { MessageBox.Show(this, e.Message, "Quiet PC", MessageBoxButtons.OK, MessageBoxIcon.Warning); }
            RefreshStatus();
        }
        void RefreshStatus() {
            var deadline = engine.EndsAt;
            state.Text = engine.State.Active ? "Quiet on" : "Quiet off";
            countdown.Text = engine.State.Active ? "\u221e" : "\u2014";
            preview.Text = engine.State.Active ? "No timer" : "";
            if (deadline.HasValue && engine.State.Active) {
                var remaining = deadline.Value - DateTimeOffset.UtcNow;
                if (remaining < TimeSpan.Zero) remaining = TimeSpan.Zero;
                countdown.Text = ((int)remaining.TotalHours).ToString("00") + ":" + remaining.Minutes.ToString("00") + ":" + remaining.Seconds.ToString("00");
                preview.Text = "Until " + deadline.Value.ToLocalTime().ToString("ddd HH:mm");
            }
            cancel.Enabled = deadline.HasValue; stop.Enabled = engine.State.Active;
            countdown.AccessibleName = countdown.Text == "\u221e" ? "No time limit" : "Time remaining: " + countdown.Text;
            feedback.ForeColor = SystemInformation.HighContrast ? SystemColors.WindowText : Brand.Warning;
            feedback.Text = engine.Error.Length > 0 ? "Needs attention \u00b7 Tray \u2192 Details" : !engine.State.Active && engine.State.Devices.Count > 0 ? "Waiting for audio devices" : "";
        }
        protected override bool ProcessCmdKey(ref Message message, Keys keyData) {
            if (keyData == Keys.Escape) { Close(); return true; }
            return base.ProcessCmdKey(ref message, keyData);
        }
        protected override void Dispose(bool disposing) { if (disposing) { clock.Dispose(); hints.Dispose(); if (Icon != null) { Icon.Dispose(); Icon = null; } } base.Dispose(disposing); }
    }
}
