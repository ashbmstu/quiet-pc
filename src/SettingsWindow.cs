using System;
using System.Drawing;
using System.Windows.Forms;

namespace QuietPC {
    sealed class SettingsWindow : Form {
        readonly CheckBox notifications, audio, visible, startup, resume;
        readonly ComboBox shortcut;
        readonly Label feedback;
        readonly ToolTip tips = new ToolTip();
        readonly Action<Preferences, bool> apply;

        public SettingsWindow(Preferences value, Action<Preferences, bool> apply) {
            this.apply = apply;
            Text = "Quiet PC settings";
            Font = new Font("Segoe UI", 10);
            BackColor = SystemInformation.HighContrast ? SystemColors.Window : Brand.Canvas;
            ForeColor = SystemInformation.HighContrast ? SystemColors.WindowText : Brand.Ink;
            AutoScaleDimensions = new SizeF(96, 96); AutoScaleMode = AutoScaleMode.Dpi;
            ClientSize = new Size(400, 412);
            AutoSize = true; AutoSizeMode = AutoSizeMode.GrowAndShrink;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false; MinimizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;
            Icon = Brand.CreateIcon(1, 32);
            tips.ShowAlways = true;
            var layout = new TableLayoutPanel {
                Dock = DockStyle.Fill, AutoSize = true, ColumnCount = 1,
                Padding = new Padding(24, 20, 24, 18), BackColor = BackColor
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 352));
            Controls.Add(layout);
            AddRow(layout, new Label {
                Text = "Settings", AutoSize = true, Font = new Font("Segoe UI", 18, FontStyle.Regular),
                Margin = new Padding(0, 0, 0, 20)
            });
            notifications = Check("Hide &notifications", value.Notifications,
                "Suppress Windows notifications during quiet mode. Applies next time quiet mode starts.");
            audio = Check("Mute &audio", value.Audio,
                "Mute all playback devices during quiet mode. Volume levels and microphones stay unchanged. Applies next time quiet mode starts.");
            AddRow(layout, notifications); AddRow(layout, audio);
            AddRow(layout, new Label { Height = 10, AutoSize = false, Margin = new Padding(0) });
            visible = Check("Show &tray icon", value.AlwaysVisible,
                "Keep the Quiet PC icon visible outside the Windows tray overflow.");
            startup = Check("Start with &Windows", DesktopIntegration.Startup,
                "Start Quiet PC when you sign in to Windows.");
            resume = Check("&Remember quiet mode", value.ResumeQuiet,
                "Resume quiet mode after a restart. An expired timer restores settings instead.");
            AddRow(layout, visible); AddRow(layout, startup); AddRow(layout, resume);
            var shortcutRow = new TableLayoutPanel {
                AutoSize = true, Dock = DockStyle.Top, ColumnCount = 2, RowCount = 1,
                MinimumSize = new Size(0, 32), Margin = new Padding(0, 12, 0, 0)
            };
            shortcutRow.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
            shortcutRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 102));
            shortcutRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            shortcutRow.Controls.Add(new Label {
                Text = "&Shortcut", AutoSize = true, Anchor = AnchorStyles.Left,
                Margin = new Padding(0, 0, 8, 0)
            }, 0, 0);
            shortcut = new ComboBox {
                DropDownStyle = ComboBoxStyle.DropDownList, Anchor = AnchorStyles.Left | AnchorStyles.Right,
                AccessibleName = "Keyboard shortcut", Margin = new Padding(0)
            };
            shortcut.Items.AddRange(new object[] { "None", "Ctrl + Alt + Q", "Ctrl + Shift + F12", "Ctrl + Alt + F12" });
            shortcut.SelectedIndex = Math.Max(0, Math.Min(3, value.Shortcut));
            Describe(shortcut, "Toggle quiet mode from the keyboard.");
            shortcutRow.Controls.Add(shortcut, 1, 0); AddRow(layout, shortcutRow);
            feedback = new Label {
                AutoSize = true, MinimumSize = new Size(352, 24), MaximumSize = new Size(352, 0),
                ForeColor = SystemInformation.HighContrast ? SystemColors.WindowText : Brand.Muted,
                Margin = new Padding(0, 8, 0, 0), AccessibleName = "Settings status"
            };
            AddRow(layout, feedback);
            var buttons = new FlowLayoutPanel {
                AutoSize = true, Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft,
                WrapContents = false, Margin = new Padding(0)
            };
            var save = new NeoButton { Text = "&Save", Margin = new Padding(8, 0, 0, 0) };
            Brand.StylePrimary(save); save.Click += Save; buttons.Controls.Add(save); AcceptButton = save;
            var close = new NeoButton { Text = "Close", DialogResult = DialogResult.Cancel, Margin = new Padding(0) };
            Brand.StyleSecondary(close); buttons.Controls.Add(close); CancelButton = close;
            AddRow(layout, buttons);
        }
        static void AddRow(TableLayoutPanel table, Control control) {
            int row = table.RowCount++;
            table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            table.Controls.Add(control, 0, row);
        }
        CheckBox Check(string text, bool value, string description) {
            var control = new CheckBox {
                Text = text, AutoSize = true, Checked = value,
                Margin = new Padding(0, 0, 0, 10), UseVisualStyleBackColor = true
            };
            Describe(control, description); return control;
        }
        void Describe(Control control, string description) {
            tips.SetToolTip(control, description); control.AccessibleDescription = description;
        }
        void Save(object sender, EventArgs e) {
            if (!notifications.Checked && !audio.Checked) {
                feedback.ForeColor = SystemInformation.HighContrast ? SystemColors.WindowText : Brand.Warning;
                feedback.Text = "Choose notifications or audio.";
                notifications.Focus(); return;
            }
            try {
                apply(new Preferences {
                    Notifications = notifications.Checked, Audio = audio.Checked,
                    AlwaysVisible = visible.Checked, ResumeQuiet = resume.Checked, Shortcut = shortcut.SelectedIndex
                }, startup.Checked);
                feedback.ForeColor = SystemInformation.HighContrast ? SystemColors.WindowText : Brand.Success;
                feedback.Text = "Saved";
            } catch (Exception error) {
                feedback.ForeColor = SystemInformation.HighContrast ? SystemColors.WindowText : Brand.Warning;
                feedback.Text = error.Message;
            }
        }
        protected override void Dispose(bool disposing) {
            if (disposing) {
                tips.Dispose();
                if (Icon != null) { Icon.Dispose(); Icon = null; }
            }
            base.Dispose(disposing);
        }
    }
}
