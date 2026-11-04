// SPDX-License-Identifier: MIT
using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace MdShow
{
    // Application-owned dialogs use the same explicit theme as the document window.
    // Windows file/print dialogs remain under Windows' control.
    internal sealed class ThemedDialog : Form
    {
        private readonly Func<bool> darkTheme;
        internal int TitleBarThemeResult { get; private set; }
        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr window, int attribute, ref int value, int size);

        internal ThemedDialog(Func<bool> darkTheme)
        {
            this.darkTheme = darkTheme;
            AutoScaleDimensions = new SizeF(96, 96);
            AutoScaleMode = AutoScaleMode.Dpi;
            Font = SystemFonts.MessageBoxFont;
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MinimizeBox = false;
            MaximizeBox = false;
            ShowInTaskbar = false;
        }

        internal void ApplyTheme()
        {
            bool highContrast = SystemInformation.HighContrast;
            bool dark = !highContrast && darkTheme != null && darkTheme();
            BackColor = highContrast ? SystemColors.Window : dark ? Color.FromArgb(34, 39, 51) : Color.White;
            ForeColor = highContrast ? SystemColors.WindowText : dark ? Color.WhiteSmoke : Color.FromArgb(32, 39, 53);
            ApplyControls(this, dark, highContrast);
            if (!IsHandleCreated) return;
            int mode = dark ? 1 : 0;
            TitleBarThemeResult = DwmSetWindowAttribute(Handle, 20, ref mode, sizeof(int));
            // If DWM does not support an attribute, retain the native system frame.
            int caption = highContrast ? -1 : ColorTranslator.ToWin32(BackColor);
            int text = highContrast ? -1 : ColorTranslator.ToWin32(ForeColor);
            int border = -1;
            DwmSetWindowAttribute(Handle, 35, ref caption, sizeof(int));
            DwmSetWindowAttribute(Handle, 36, ref text, sizeof(int));
            DwmSetWindowAttribute(Handle, 34, ref border, sizeof(int));
        }

        private void ApplyControls(Control parent, bool dark, bool highContrast)
        {
            foreach (Control control in parent.Controls)
            {
                control.BackColor = BackColor;
                control.ForeColor = ForeColor;
                var button = control as Button;
                if (button != null)
                {
                    button.FlatStyle = highContrast ? FlatStyle.Standard : FlatStyle.Flat;
                    button.UseVisualStyleBackColor = highContrast;
                    button.FlatAppearance.BorderColor = dark ? Color.FromArgb(77, 86, 106) : Color.FromArgb(195, 201, 211);
                    button.FlatAppearance.MouseOverBackColor = dark ? Color.FromArgb(49, 57, 73) : Color.FromArgb(240, 243, 248);
                    button.FlatAppearance.MouseDownBackColor = dark ? Color.FromArgb(62, 73, 95) : Color.FromArgb(224, 231, 242);
                }
                ApplyControls(control, dark, highContrast);
            }
        }

        protected override void OnHandleCreated(EventArgs e)
        { base.OnHandleCreated(e); ApplyTheme(); }

        protected override void WndProc(ref Message message)
        {
            base.WndProc(ref message);
            if (darkTheme != null && (message.Msg == 0x001A || message.Msg == 0x031A)) ApplyTheme();
        }

        internal static string DisplayPath(string path)
        {
            // WinForms/GDI displays FSI/PDI as glyphs. Use its supported LTR
            // embedding for paths while retaining Arabic runs inside the name.
            return "\u202A" + path + "\u202C";
        }

        internal static ThemedDialog Message(Func<bool> theme, string text, string title, MessageBoxButtons buttons, MessageBoxIcon icon)
        {
            if (buttons != MessageBoxButtons.OK && buttons != MessageBoxButtons.YesNo && buttons != MessageBoxButtons.YesNoCancel)
                throw new ArgumentOutOfRangeException("buttons");
            var dialog = new ThemedDialog(theme) { Text = title, ClientSize = new Size(560, 180), Padding = new Padding(16) };
            var actions = new FlowLayoutPanel { Dock = DockStyle.Bottom, AutoSize = true, FlowDirection = FlowDirection.RightToLeft,
                WrapContents = false, Padding = new Padding(0, 14, 0, 0), RightToLeft = L.Language == "ar" ? RightToLeft.Yes : RightToLeft.No };
            var content = new Panel { Dock = DockStyle.Fill, AutoScroll = true };
            var label = new Label { Text = text, AutoSize = true, MaximumSize = new Size(472, 0), UseMnemonic = false,
                Location = new Point(48, 0), RightToLeft = L.Language == "ar" ? RightToLeft.Yes : RightToLeft.No };
            content.Controls.Add(label);
            Icon symbol = icon == MessageBoxIcon.Error ? SystemIcons.Error : icon == MessageBoxIcon.Warning ? SystemIcons.Warning :
                icon == MessageBoxIcon.Question ? SystemIcons.Question : icon == MessageBoxIcon.Information ? SystemIcons.Information : null;
            if (symbol != null)
            {
                var bitmap = symbol.ToBitmap();
                content.Controls.Add(new PictureBox { Image = bitmap, SizeMode = PictureBoxSizeMode.Zoom, Bounds = new Rectangle(0, 0, 32, 32) });
                dialog.Disposed += delegate { bitmap.Dispose(); };
            }
            var results = buttons == MessageBoxButtons.OK ? new[] { DialogResult.OK } : buttons == MessageBoxButtons.YesNo ?
                new[] { DialogResult.No, DialogResult.Yes } : new[] { DialogResult.Cancel, DialogResult.No, DialogResult.Yes };
            foreach (var result in results)
            {
                string caption = result == DialogResult.OK ? L.T("ok") : result == DialogResult.Yes ? L.T("yes") : result == DialogResult.No ? L.T("no") : L.T("cancel");
                var button = new Button { Text = caption, DialogResult = result, AutoSize = true, MinimumSize = new Size(90, 30), Margin = new Padding(6, 0, 0, 0) };
                actions.Controls.Add(button);
                if (result == DialogResult.Yes || result == DialogResult.OK) dialog.AcceptButton = button;
                if (result == DialogResult.Cancel || result == DialogResult.OK) dialog.CancelButton = button;
            }
            dialog.Controls.Add(content);
            dialog.Controls.Add(actions);
            dialog.ClientSize = new Size(560, Math.Max(160, Math.Min(420, label.PreferredHeight) + 82));
            dialog.ControlBox = buttons != MessageBoxButtons.YesNo;
            dialog.FormClosing += delegate(object sender, FormClosingEventArgs e) {
                if (buttons == MessageBoxButtons.YesNo && dialog.DialogResult != DialogResult.Yes && dialog.DialogResult != DialogResult.No) e.Cancel = true;
                if (buttons == MessageBoxButtons.OK && dialog.DialogResult == DialogResult.Cancel) dialog.DialogResult = DialogResult.OK;
            };
            return dialog;
        }
    }
}
