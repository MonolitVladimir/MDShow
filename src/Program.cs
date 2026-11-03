// SPDX-License-Identifier: MIT
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Windows.Forms;
using Microsoft.VisualBasic.ApplicationServices;
using Microsoft.Win32;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;

[assembly: AssemblyTitle("MD Show")]
[assembly: AssemblyDescription("Lightweight Markdown viewer and editor for Windows 11")]
[assembly: AssemblyCompany("MONOLIT Agency LLC / ООО «Агентство «МОНОЛИТ»")]
[assembly: AssemblyCopyright("Copyright (c) 2026 ООО «Агентство «МОНОЛИТ»")]
[assembly: AssemblyProduct("MD Show")]
[assembly: AssemblyVersion("1.0.0.0")]
[assembly: AssemblyFileVersion("1.0.0.0")]
[assembly: AssemblyInformationalVersion("1.0")]
// WindowsFormsApplicationBase keys IPC by the assembly's typelib GUID + major/minor.
#if INSTALLED
[assembly: Guid("c5300b73-09bb-44bb-989b-f89bdcfda502")]
#elif ENGLISH_ONLY
[assembly: Guid("a890c177-63f3-42ea-8119-d621015e2bb0")]
#else
[assembly: Guid("3ec5381d-ccaf-46c9-a29d-456138db7132")]
#endif

namespace MdShow
{
    internal static class Program
    {
        [STAThread]
        private static void Main(string[] args)
        {
            StartupTrace.Start(args);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            Application.ThreadException += delegate(object sender, System.Threading.ThreadExceptionEventArgs e)
                { MessageBox.Show(e.Exception.Message, L.T("error"), MessageBoxButtons.OK, MessageBoxIcon.Error); };
#if INSTALLED
            FileStream installation;
            try { installation = new FileStream(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "installation.lock"), FileMode.Open, FileAccess.Read, FileShare.Read); }
            // Do not keep an executable mapped in a modal dialog during uninstall.
            catch (IOException) { Environment.ExitCode = 2; return; }
            catch (UnauthorizedAccessException) { Environment.ExitCode = 2; return; }
            using (installation) Run(args);
#else
            Run(args);
#endif
        }

        private static void Run(string[] args)
        {
            try
            {
                new SingleInstanceApp(!args.Contains("--smoke-test") && !args.Contains("--qa")).Run(args);
            }
            catch (IOException) { MessageBox.Show(L.T("profileBusy"), L.T("error"), MessageBoxButtons.OK, MessageBoxIcon.Warning); }
            catch (UnauthorizedAccessException) { MessageBox.Show(L.T("profileBusy"), L.T("error"), MessageBoxButtons.OK, MessageBoxIcon.Warning); }
        }
    }

    internal sealed class SingleInstanceApp : WindowsFormsApplicationBase
    {
        public SingleInstanceApp(bool singleInstance) { IsSingleInstance = singleInstance; EnableVisualStyles = true; ShutdownStyle = ShutdownMode.AfterMainFormCloses; }
        protected override void OnCreateMainForm() { MainForm = new MainWindow(CommandLineArgs.ToArray()); }
        protected override void OnStartupNextInstance(StartupNextInstanceEventArgs e)
        {
            base.OnStartupNextInstance(e);
            ((MainWindow)MainForm).OpenArguments(e.CommandLine.ToArray());
            e.BringToForeground = true;
        }
    }

    public sealed class Preferences
    {
        public List<string> Recent = new List<string>();
        public string Theme = "system";
        public string Mode = "split";
        public string Language = "auto";
        public int EditorWidth = 48;
        public int FontSize = 15;
        public int ZoomPercent;
        public bool Wrap = true;
        public int Width = 1200;
        public int Height = 800;
        public bool Maximized;
    }

    internal sealed class MainWindow : Form
    {
        private const string Origin = "https://mdshow.invalid";
        private readonly WebView2 web = new WebView2();
        private readonly JavaScriptSerializer json = new JavaScriptSerializer { MaxJsonLength = 100 * 1024 * 1024 };
        private readonly List<Document> documents = new List<Document>();
        private readonly List<string> pending = new List<string>();
        private readonly string dataDir;
        private readonly FileStream profileLease;
        private readonly Timer recoveryTimer = new Timer { Interval = 10000 };
        private Preferences preferences = new Preferences();
        private string activeId;
        private bool ready, closing, allowClose, busy, rendererFailed;
        private bool recoveryWritable = true;
        private bool recoveryLoaded;
        private readonly bool smokeTest;
        private readonly bool keepSmokeOpen;
        private readonly bool exitAfterStartup;
        private readonly string smokeScript;
        private readonly string diagnosticsDir;
        private bool? appliedDarkMode;
        private bool unifiedTitleBar;
        private bool frameWasMaximized;
        private bool restoringFrame;
        private Rectangle normalFrameBounds;
        private const int ToolbarHeight = 44;
        [StructLayout(LayoutKind.Sequential)]
        private struct NativeRect { public int Left, Top, Right, Bottom; }
        [StructLayout(LayoutKind.Sequential)]
        private struct FrameMargins { public int Left, Right, Top, Bottom; }
        [DllImport("dwmapi.dll")]
        private static extern int DwmExtendFrameIntoClientArea(IntPtr hwnd, ref FrameMargins margins);
        [DllImport("dwmapi.dll")]
        private static extern bool DwmDefWindowProc(IntPtr hwnd, int message, IntPtr wParam, IntPtr lParam, out IntPtr result);
        [DllImport("dwmapi.dll", EntryPoint = "DwmGetWindowAttribute")]
        private static extern int DwmGetCaptionBounds(IntPtr hwnd, int attribute, out NativeRect value, int size);
        [DllImport("user32.dll")]
        private static extern bool SetWindowPos(IntPtr hwnd, IntPtr after, int x, int y, int width, int height, uint flags);
        [DllImport("user32.dll")]
        private static extern int GetSystemMetricsForDpi(int index, uint dpi);
        [DllImport("user32.dll")]
        private static extern bool IsZoomed(IntPtr hwnd);
        private const int UseImmersiveDarkMode = 20;
        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);
        [DllImport("dwmapi.dll")]
        private static extern int DwmGetWindowAttribute(IntPtr hwnd, int attribute, out int value, int size);
        private string RecoveryFile { get { return Path.Combine(dataDir, "recovery.json"); } }

        public MainWindow(string[] args)
        {
            StartupTrace.Mark("constructorBegin");
            smokeTest = args.Contains("--smoke-test");
            keepSmokeOpen = smokeTest && args.Contains("--keep-open");
            exitAfterStartup = args.Contains("--qa") && StartupTrace.Enabled && args.Contains("--qa-startup-exit");
            smokeScript = args.FirstOrDefault(arg => arg.StartsWith("--smoke-script=", StringComparison.Ordinal));
            string diagnosticsArgument = args.FirstOrDefault(arg => arg.StartsWith("--diagnostics-dir=", StringComparison.Ordinal));
            diagnosticsDir = diagnosticsArgument == null ? AppDomain.CurrentDomain.BaseDirectory : Path.GetFullPath(diagnosticsArgument.Substring("--diagnostics-dir=".Length).Trim('"'));
            dataDir = smokeTest || args.Contains("--qa") ? Path.Combine(diagnosticsDir, smokeTest ? "test-profile" : "qa-profile") : Profile.DirectoryPath;
            profileLease = Profile.Acquire(dataDir);
            StartupTrace.Mark("profileAcquired");
            FormClosed += delegate { profileLease.Dispose(); };
            try { preferences = json.Deserialize<Preferences>(File.ReadAllText(Path.Combine(dataDir, "settings.json"))) ?? preferences; }
            catch (IOException) { }
            catch (ArgumentException) { }
            catch (InvalidOperationException) { }
            // Preserve text size from settings written by version 1.0.0.
            if (preferences.ZoomPercent == 0) preferences.ZoomPercent = (int)Math.Round(preferences.FontSize * 100.0 / 15);
            preferences.ZoomPercent = Math.Max(50, Math.Min(300, preferences.ZoomPercent));
            // A valid JSON null must not break opening files or updating recents.
            if (preferences.Recent == null) preferences.Recent = new List<string>();
            L.Language = L.Resolve(preferences.Language, System.Globalization.CultureInfo.CurrentUICulture.Name);
            StartupTrace.Mark("preferencesAndLocaleReady");
            Text = "MD Show";
            MinimumSize = new Size(760, 500);
            Size = new Size(Math.Max(760, Math.Min(2400, preferences.Width)), Math.Max(500, Math.Min(1600, preferences.Height)));
            StartPosition = FormStartPosition.CenterScreen;
            if (preferences.Maximized) WindowState = FormWindowState.Maximized;
            Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
            web.Dock = DockStyle.Fill;
            web.DefaultBackgroundColor = Color.FromArgb(247, 248, 250);
            Controls.Add(web);
            pending.AddRange(args.Where(x => !x.StartsWith("--")));
            Shown += async delegate { await Initialize(); };
            FormClosing += OnClosing;
            recoveryTimer.Tick += delegate { WriteRecovery(); };
            StartupTrace.Mark("constructorEnd");
        }

        private bool startupPainted;
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            if (!startupPainted && Visible) { startupPainted = true; StartupTrace.Mark("windowPainted"); }
        }

        private bool IsDarkWindowTheme()
        {
            if (SystemInformation.HighContrast || preferences.Theme == "light") return false;
            if (preferences.Theme == "dark") return true;
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize"))
                    return key != null && Object.Equals(key.GetValue("AppsUseLightTheme"), 0);
            }
            catch (System.Security.SecurityException) { return false; }
            catch (UnauthorizedAccessException) { return false; }
        }

        private void ApplyWindowTheme()
        {
            if (!IsHandleCreated) return;
            bool dark = IsDarkWindowTheme();
            if (appliedDarkMode == dark) return;
            appliedDarkMode = dark;
            int value = dark ? 1 : 0;
            DwmSetWindowAttribute(Handle, UseImmersiveDarkMode, ref value, sizeof(int));
            BackColor = dark ? Color.FromArgb(34, 39, 51) : Color.White;
            web.DefaultBackgroundColor = BackColor;
            int captionColor = SystemInformation.HighContrast ? -1 : ColorTranslator.ToWin32(BackColor);
            if (unifiedTitleBar) DwmSetWindowAttribute(Handle, 35, ref captionColor, sizeof(int));
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            appliedDarkMode = null;
            ApplyWindowTheme();
        }

        protected override void WndProc(ref Message m)
        {
            if (unifiedTitleBar && m.Msg == 0x0005) // WM_SIZE
            {
                bool restore = m.WParam == IntPtr.Zero && frameWasMaximized;
                var restoreBounds = normalFrameBounds;
                if (m.WParam == new IntPtr(2)) frameWasMaximized = true;
                else if (m.WParam == IntPtr.Zero) frameWasMaximized = false;
                base.WndProc(ref m);
                // WinForms adds the removed top frame when restoring its saved
                // client size. Correct that exact size change after it finishes;
                // keep Windows' position and any different size chosen by Snap.
                if (restore && !restoreBounds.IsEmpty)
                {
                    restoringFrame = true;
                    BeginInvoke((Action)delegate {
                        int removedTop = GetSystemMetricsForDpi(4, (uint)DeviceDpi)
                            + GetSystemMetricsForDpi(33, (uint)DeviceDpi) + GetSystemMetricsForDpi(92, (uint)DeviceDpi);
                        if (WindowState == FormWindowState.Normal && Width == restoreBounds.Width && Height == restoreBounds.Height + removedTop)
                            Size = restoreBounds.Size;
                        restoringFrame = false;
                        if (WindowState == FormWindowState.Normal) normalFrameBounds = Bounds;
                    });
                }
                if (m.WParam == IntPtr.Zero && !restoringFrame) normalFrameBounds = Bounds;
                UpdateWindowFrame();
                return;
            }
            if (unifiedTitleBar && m.Msg == 0x0083 && m.WParam != IntPtr.Zero) // WM_NCCALCSIZE
            {
                var bounds = (NativeRect)Marshal.PtrToStructure(m.LParam, typeof(NativeRect));
                int border = GetSystemMetricsForDpi(33, (uint)DeviceDpi) + GetSystemMetricsForDpi(92, (uint)DeviceDpi);
                var client = bounds;
                client.Left += border;
                client.Right -= border;
                client.Bottom -= border;
                // The top inset must be exactly zero for DWM caption hit testing.
                // The off-screen maximized border is handled by client padding.
                client.Top = bounds.Top;
                Marshal.StructureToPtr(client, m.LParam, false);
                m.Result = IntPtr.Zero;
                return;
            }
            IntPtr result;
            if (unifiedTitleBar && DwmDefWindowProc(Handle, m.Msg, m.WParam, m.LParam, out result))
            {
                m.Result = result;
                return;
            }
            base.WndProc(ref m);
            if (unifiedTitleBar && m.Msg == 0x0084 && WindowState != FormWindowState.Maximized) // WM_NCHITTEST
            {
                var point = PointToClient(new Point(unchecked((short)m.LParam.ToInt64()), unchecked((short)(m.LParam.ToInt64() >> 16))));
                if (point.X >= 0 && point.X < ClientSize.Width && point.Y >= 0 && point.Y < Math.Max(2, DeviceDpi * 3 / 96))
                    m.Result = new IntPtr(12); // HTTOP; keep the native corner resize targets.
            }
            if (unifiedTitleBar && m.Msg == 0x0003 && !restoringFrame && !frameWasMaximized && !IsZoomed(Handle) && WindowState == FormWindowState.Normal) normalFrameBounds = Bounds;
            if (unifiedTitleBar && (m.Msg == 0x0006 || m.Msg == 0x02E0)) UpdateWindowFrame();
            // Windows broadcasts these messages when personalization settings change.
            if (m.Msg == 0x001A || m.Msg == 0x031A) ApplyWindowTheme();
        }

        private void UpdateWindowFrame()
        {
            if (!unifiedTitleBar || !web.IsHandleCreated || WindowState == FormWindowState.Minimized) return;
            int topInset = IsZoomed(Handle)
                ? GetSystemMetricsForDpi(33, (uint)DeviceDpi) + GetSystemMetricsForDpi(92, (uint)DeviceDpi) : 0;
            if (Padding.Top != topInset) Padding = new Padding(0, topInset, 0, 0);
            int height = ToolbarHeight * DeviceDpi / 96;
            var margins = new FrameMargins { Top = height + topInset };
            DwmExtendFrameIntoClientArea(Handle, ref margins);
            NativeRect caption;
            int width = 146 * DeviceDpi / 96;
            if (DwmGetCaptionBounds(Handle, 5, out caption, Marshal.SizeOf(typeof(NativeRect))) == 0 && caption.Right > caption.Left)
                width = caption.Right - caption.Left + 8 * DeviceDpi / 96;
            // Leave the native caption buttons exposed: Windows owns their painting,
            // accessibility, hit testing and Windows 11 Snap Layouts.
            var region = new Region(web.ClientRectangle);
            region.Exclude(new Rectangle(Math.Max(0, web.Width - width), 0, width, height));
            if (WindowState != FormWindowState.Maximized)
                region.Exclude(new Rectangle(0, 0, web.Width, Math.Max(2, DeviceDpi * 3 / 96)));
            var previous = web.Region;
            web.Region = region;
            if (previous != null) previous.Dispose();
            if (ready) Send(new { type = "frame", captionWidth = (int)Math.Ceiling(width * 96.0 / DeviceDpi) });
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            // Zero-alpha black exposes DWM's native caption inside the extended frame.
            if (unifiedTitleBar) e.Graphics.Clear(Color.Black);
            else base.OnPaintBackground(e);
        }

        private int NativeMaximizeHitTest()
        {
            NativeRect caption;
            if (DwmGetCaptionBounds(Handle, 5, out caption, Marshal.SizeOf(typeof(NativeRect))) != 0) return -1;
            int x = Left + (caption.Left + caption.Right) / 2;
            int y = Top + (caption.Top + caption.Bottom) / 2;
            IntPtr result;
            DwmDefWindowProc(Handle, 0x0084, IntPtr.Zero, new IntPtr((y << 16) | (x & 0xffff)), out result);
            return result.ToInt32(); // HTMAXBUTTON (9) is also used by Windows 11 Snap Layouts.
        }

        private async Task Initialize()
        {
            try
            {
                StartupTrace.Mark("environmentBegin");
                var environment = await CoreWebView2Environment.CreateAsync(null, Path.Combine(dataDir, "WebView2"));
                if (IsDisposed || Disposing) return;
                StartupTrace.Mark("environmentReady");
                if (StartupTrace.Enabled) StartupTrace.Runtime = environment.BrowserVersionString;
                await web.EnsureCoreWebView2Async(environment);
                if (IsDisposed || Disposing) return;
                StartupTrace.Mark("controllerReady");
                var core = web.CoreWebView2;
                core.Settings.AreDefaultContextMenusEnabled = true;
                core.Settings.AreDevToolsEnabled = false;
                core.Settings.AreBrowserAcceleratorKeysEnabled = false;
                core.Settings.IsZoomControlEnabled = false;
                core.Settings.IsStatusBarEnabled = false;
                core.Settings.IsGeneralAutofillEnabled = false;
                core.Settings.IsPasswordAutosaveEnabled = false;
                core.Settings.IsBuiltInErrorPageEnabled = false;
                // Old system runtimes keep the ordinary Windows title bar.
                try { core.Settings.IsNonClientRegionSupportEnabled = true; unifiedTitleBar = true; }
                catch (NotImplementedException) { }
                if (unifiedTitleBar)
                {
                    frameWasMaximized = IsZoomed(Handle);
                    normalFrameBounds = frameWasMaximized ? RestoreBounds : Bounds;
                    appliedDarkMode = null;
                    ApplyWindowTheme();
                    SetWindowPos(Handle, IntPtr.Zero, 0, 0, 0, 0, 0x0037); // FRAMECHANGED, no move/size/z-order/activation
                    UpdateWindowFrame();
                }
                core.PermissionRequested += delegate(object s, CoreWebView2PermissionRequestedEventArgs e) { e.State = CoreWebView2PermissionState.Deny; };
                core.DownloadStarting += delegate(object s, CoreWebView2DownloadStartingEventArgs e) { e.Cancel = true; };
                core.NewWindowRequested += delegate(object s, CoreWebView2NewWindowRequestedEventArgs e) { e.Handled = true; };
                core.NavigationStarting += delegate(object s, CoreWebView2NavigationStartingEventArgs e)
                    { if (e.Uri != Origin + "/index.html") e.Cancel = true; };
                core.AddWebResourceRequestedFilter("*", CoreWebView2WebResourceContext.All);
                core.WebResourceRequested += ResourceRequested;
                core.DOMContentLoaded += delegate { StartupTrace.Mark("domContentLoaded"); };
                core.NavigationCompleted += delegate { StartupTrace.Mark("navigationCompleted"); };
                core.WebMessageReceived += MessageReceived;
                core.ContextMenuRequested += ContextMenuRequested;
                core.ProcessFailed += delegate(object s, CoreWebView2ProcessFailedEventArgs e) {
                    StartupTrace.Mark("processFailed:" + e.ProcessFailedKind + ":" + e.Reason + ":" + e.ExitCode); StartupTrace.Flush();
                    // A large editor can be temporarily unresponsive without
                    // exiting. Keep waiting; do not mark its live state as lost.
                    if (e.ProcessFailedKind != CoreWebView2ProcessFailedKind.BrowserProcessExited &&
                        e.ProcessFailedKind != CoreWebView2ProcessFailedKind.RenderProcessExited) return;
                    rendererFailed = true; WriteRecovery(); ShowDialogMessage(L.T("rendererFailed"), "MD Show", MessageBoxButtons.OK, MessageBoxIcon.Error);
                };
                StartupTrace.Mark("navigationBegin");
                core.Navigate(Origin + "/index.html");
            }
            catch (Exception e)
            {
                if (IsDisposed || Disposing) return;
                ShowDialogMessage(L.T("webviewFailed") + "\n\n" + e.Message, "MD Show", MessageBoxButtons.OK, MessageBoxIcon.Error);
                allowClose = true; Close();
            }
        }

        private void ResourceRequested(object sender, CoreWebView2WebResourceRequestedEventArgs e)
        {
            if (StartupTrace.Enabled) StartupTrace.ResourceRequests++;
            try
            {
                var uri = new Uri(e.Request.Uri);
                string path = uri.AbsolutePath.TrimStart('/');
                if (StartupTrace.Enabled && new[] { "index.html", "style.css", "vendor/purify.js", "core.js", "i18n.js", "app.js", "logo.png", "markdown-worker.js", "vendor/marked.js" }.Contains(path)) StartupTrace.Mark("resource:" + path);
                Stream stream = null;
                string type = "application/octet-stream";
                if (uri.GetLeftPart(UriPartial.Authority) == Origin)
                {
                    if (path.StartsWith("media/"))
                    {
                        var parts = path.Split(new[] { '/' }, 3);
                        var doc = parts.Length == 3 ? documents.Find(d => d.Id == parts[1]) : null;
                        string local = doc == null ? null : Document.ResolveLocal(doc.Path, parts[2]);
                        var types = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) {
                            { ".png", "image/png" }, { ".jpg", "image/jpeg" }, { ".jpeg", "image/jpeg" }, { ".gif", "image/gif" },
                            { ".webp", "image/webp" }, { ".svg", "image/svg+xml" }, { ".bmp", "image/bmp" }, { ".ico", "image/x-icon" } };
                        if (local != null && types.TryGetValue(Path.GetExtension(local), out type) && File.Exists(local) && new FileInfo(local).Length < Document.MaxBytes)
                            stream = new MemoryStream(Document.ReadBytes(local));
                    }
                    else
                    {
                        stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("web." + path.Replace('/', '.'));
                        // Match the native frame before the first HTML paint, not only
                        // after the asynchronous settings message reaches the page.
                        if (path == "index.html" && stream != null)
                        {
                            string html;
                            using (var reader = new StreamReader(stream, Encoding.UTF8)) html = reader.ReadToEnd();
                            if (IsDarkWindowTheme()) html = html.Replace("<body>", "<body class=\"dark\">");
                            stream = new MemoryStream(Encoding.UTF8.GetBytes(html));
                        }
                        type = path.EndsWith(".html") ? "text/html; charset=utf-8" : path.EndsWith(".css") ? "text/css; charset=utf-8" : path.EndsWith(".png") ? "image/png" : "text/javascript; charset=utf-8";
                    }
                }
                string headers = "Content-Type: " + type + "\r\nCache-Control: no-store\r\nX-Content-Type-Options: nosniff\r\n";
                e.Response = web.CoreWebView2.Environment.CreateWebResourceResponse(stream ?? new MemoryStream(), stream == null ? 404 : 200, stream == null ? "Not Found" : "OK", headers);
            }
            catch { e.Response = web.CoreWebView2.Environment.CreateWebResourceResponse(new MemoryStream(), 404, "Not Found", ""); }
        }

        private void Send(object message) { if (ready && !IsDisposed) web.CoreWebView2.PostWebMessageAsJson(json.Serialize(message)); }
        private void ContextMenuRequested(object sender, CoreWebView2ContextMenuRequestedEventArgs e)
        {
            // Keep Chromium's native editing commands and undo stack; translate only their labels.
            // Other browser commands (navigation, view source, web search) are not app actions.
            e.Handled = true;
            var menu = new ContextMenuStrip { RightToLeft = L.Language == "ar" ? RightToLeft.Yes : RightToLeft.No };
            var commands = new[] { "undo", "redo", "cut", "copy", "paste", "pasteAndMatchStyle", "selectAll" };
            foreach (var item in e.MenuItems)
            {
                if (!commands.Contains(item.Name)) continue;
                var command = item;
                var entry = new ToolStripMenuItem(L.T(command.Name)) { Enabled = command.IsEnabled };
                entry.Click += delegate { e.SelectedCommandId = command.CommandId; };
                menu.Items.Add(entry);
            }
            if (menu.Items.Count == 0) { menu.Dispose(); return; }
            var deferral = e.GetDeferral();
            // ToolStrip finishes dispatching the clicked command after Closed.
            // Release the menu and WebView deferral on the next UI message.
            menu.Closed += delegate { BeginInvoke((Action)delegate { deferral.Complete(); menu.Dispose(); }); };
            menu.Show(web, e.Location);
        }
        private static string Isolate(string value) { return "\u2068" + value + "\u2069"; }
        private DialogResult ShowDialogMessage(string message, string title, MessageBoxButtons buttons = MessageBoxButtons.OK, MessageBoxIcon icon = MessageBoxIcon.None)
        {
            using (var dialog = ThemedDialog.Message(IsDarkWindowTheme, message, title, buttons, icon))
            { dialog.Icon = Icon; return dialog.ShowDialog(this); }
        }
        private void Notify(string message) { Send(new { type = "notice", message = message }); }
        private object View(Document d) { return new { id = d.Id, path = d.Path, name = d.Name, text = d.Text, original = d.Text == d.OriginalText ? null : d.OriginalText, encoding = d.EncodingName + (d.Bom ? " BOM" : ""), newline = d.Newline == "\r\n" ? "CRLF" : d.Newline == "\r" ? "CR" : "LF", dirty = d.Dirty }; }
        private void SendDocument(Document d) { Send(new { type = "document", document = View(d) }); }
        private void UpdateTitle() { var d = documents.Find(x => x.Id == activeId); Text = d == null ? "MD Show" : (d.Dirty ? "● " : "") + d.Name + " — MD Show"; }
        private static string Str(Dictionary<string, object> m, string key) { return m.ContainsKey(key) ? Convert.ToString(m[key]) : ""; }

        private async void MessageReceived(object sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
            if (e.Source != Origin + "/index.html") return;
            bool acquired = false;
            try
            {
                var m = json.Deserialize<Dictionary<string, object>>(e.WebMessageAsJson);
                string action = Str(m, "action");
                if (action == "startupStage") {
                    string stage = Str(m, "stage");
                    if (StartupTrace.Enabled && new[] { "uiReady", "documentReady", "documentReceived", "editorValueBegin", "editorValueEnd", "editorSelectionSet", "editorScrollSet", "activationSettings", "activationTabs", "activationStatus", "activationRender", "activationFocus" }.Contains(stage)) {
                        StartupTrace.Mark(stage); StartupTrace.Flush();
                        if (stage == "documentReady" && exitAfterStartup) BeginInvoke((Action)Close);
                    }
                    return;
                }
                var doc = documents.Find(d => d.Id == Str(m, "id"));
                if (action == "change" && doc != null) { doc.Text = Document.Normalize(Str(m, "text")); UpdateTitle(); return; }
                if (action == "active") { activeId = Str(m, "id"); UpdateTitle(); return; }
                if (busy) return;
                busy = true; acquired = true;
                switch (action)
                {
                    case "ready":
                        StartupTrace.Mark("bridgeReady");
                        ready = true;
                        UpdateWindowFrame();
                        Send(new { type = "settings", settings = preferences, catalogs = L.Catalogs, systemLanguage = System.Globalization.CultureInfo.CurrentUICulture.Name, startupTrace = StartupTrace.Enabled });
                        if (!smokeTest) Recover();
                        recoveryLoaded = true;
                        foreach (var path in pending.ToArray()) OpenPath(path);
                        pending.Clear();
                        if (documents.Count == 0) NewDocument();
                        web.Focus();
                        recoveryTimer.Start();
                        // Release the ready-message gate before diagnostics send host commands.
                        if (smokeTest) BeginInvoke((Action)(async () => await RunSmokeTest()));
                        break;
                    case "new": NewDocument(); break;
                    case "open":
                        using (var dialog = new OpenFileDialog { Filter = "Markdown (*.md;*.markdown;*.mdown)|*.md;*.markdown;*.mdown|" + L.T("textFiles") + " (*.txt)|*.txt|" + L.T("allFiles") + " (*.*)|*.*", Multiselect = true })
                            if (dialog.ShowDialog(this) == DialogResult.OK) foreach (string path in dialog.FileNames) OpenPath(path);
                        break;
                    case "recent": if (preferences.Recent.Contains(Str(m, "path"))) OpenPath(Str(m, "path")); break;
                    case "drop": foreach (object item in e.AdditionalObjects) { var file = item as CoreWebView2File; if (file != null) OpenPath(file.Path); } break;
                    case "save": if (doc != null) SaveDocument(doc, false); break;
                    case "saveAs": if (doc != null) SaveDocument(doc, true); break;
                    case "close":
                        if (doc != null && CanClose(doc)) { documents.Remove(doc); Send(new { type = "closed", id = doc.Id }); WriteRecovery(); }
                        break;
                    case "reload":
                        if (doc != null && doc.Path.Length > 0 && (!doc.Dirty || ShowDialogMessage(L.T("reloadPrompt"), L.T("reload"), MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes))
                        { var fresh = Document.Read(doc.Path); fresh.Id = doc.Id; documents[documents.IndexOf(doc)] = fresh; SendDocument(fresh); WriteRecovery(); }
                        break;
                    case "settings":
                        preferences.Theme = new[] { "light", "dark", "system" }.Contains(Str(m, "theme")) ? Str(m, "theme") : "system";
                        preferences.Mode = new[] { "read", "edit", "split" }.Contains(Str(m, "mode")) ? Str(m, "mode") : "split";
                        int zoom; if (Int32.TryParse(Str(m, "zoomPercent"), System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out zoom)) preferences.ZoomPercent = Math.Max(50, Math.Min(300, zoom));
                        preferences.Wrap = Str(m, "wrap").ToLowerInvariant() == "true";
                        // EN-only resolves the UI to English without destroying the full edition's preference.
                        if (!L.EnglishOnly)
                            preferences.Language = Str(m, "language") == "auto" || L.Languages.Contains(Str(m, "language")) ? Str(m, "language") : "auto";
                        int width; if (Int32.TryParse(Str(m, "editorWidth"), System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out width)) preferences.EditorWidth = Math.Max(20, Math.Min(80, width));
                        L.Language = L.Resolve(preferences.Language, System.Globalization.CultureInfo.CurrentUICulture.Name);
                        // Web chrome and native dialogs set their own direction. Changing
                        // Form.RightToLeft here recreates the HWND and loses input focus.
                        ApplyWindowTheme(); UpdateTitle(); SavePreferences(); break;
                    case "external":
                        Uri external;
                        if (Uri.TryCreate(Str(m, "url"), UriKind.Absolute, out external) && (external.Scheme == "https" || external.Scheme == "http" || external.Scheme == "mailto"))
                            Process.Start(new ProcessStartInfo(external.AbsoluteUri) { UseShellExecute = true });
                        break;
                    case "localLink":
                        if (doc != null) { string path = Document.ResolveLocal(doc.Path, Str(m, "url"));
                            if (path != null && new[] { ".md", ".markdown", ".mdown", ".txt" }.Contains(Path.GetExtension(path).ToLowerInvariant())) OpenPath(path);
                            else Notify(L.T("localLinkDenied")); }
                        break;
                    case "exportHtml": if (doc != null) ExportHtml(doc, Str(m, "html")); break;
                    case "exportPdf":
                        if (doc != null) using (var dialog = new SaveFileDialog { Filter = "PDF (*.pdf)|*.pdf", FileName = Path.GetFileNameWithoutExtension(doc.Name) + ".pdf" })
                            if (dialog.ShowDialog(this) == DialogResult.OK)
                            { if (!await web.CoreWebView2.PrintToPdfAsync(dialog.FileName)) throw new IOException(L.T("pdfFailed")); Notify(L.T("pdfSaved")); }
                        break;
                    case "print": web.CoreWebView2.ShowPrintUI(); break;
                    case "defaultApps": Process.Start(new ProcessStartInfo("ms-settings:defaultapps") { UseShellExecute = true }); break;
                    case "about": ShowAbout(); break;
                    // Let the message handler release busy before the ordinary unsaved-document close flow.
                    case "exit": BeginInvoke((Action)Close); break;
                }
                UpdateTitle();
            }
            catch (Exception error) { ShowDialogMessage(error.Message, L.T("error"), MessageBoxButtons.OK, MessageBoxIcon.Error); }
            finally { if (acquired) busy = false; }
        }

        public void OpenArguments(string[] args)
        {
            StartupTrace.Mark("argumentsReceived");
            if (WindowState == FormWindowState.Minimized) WindowState = FormWindowState.Normal;
            Activate();
            foreach (string path in args.Where(x => !x.StartsWith("--"))) { if (ready) OpenPath(path); else pending.Add(path); }
        }

        private void ShowAbout()
        {
            using (var dialog = new ThemedDialog(IsDarkWindowTheme) { Text = L.T("about"), Icon = Icon, FormBorderStyle = FormBorderStyle.FixedDialog,
                StartPosition = FormStartPosition.CenterParent, MinimizeBox = false, MaximizeBox = false,
                ClientSize = new Size(540, 270), BackColor = BackColor,
                ForeColor = IsDarkWindowTheme() ? Color.WhiteSmoke : Color.FromArgb(32, 39, 53) })
            using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("web.logo.png"))
            using (var logo = Image.FromStream(stream))
            {
                dialog.Controls.Add(new PictureBox { Image = logo, SizeMode = PictureBoxSizeMode.Zoom, Bounds = new Rectangle(16, 18, 64, 64) });
                dialog.Controls.Add(new Label { Text = L.T("aboutText"), Bounds = new Rectangle(96, 18, 428, 210),
                    RightToLeft = L.Language == "ar" ? RightToLeft.Yes : RightToLeft.No });
                var ok = new Button { Text = L.T("ok"), DialogResult = DialogResult.OK, Bounds = new Rectangle(434, 232, 90, 28) };
                dialog.Controls.Add(ok); dialog.AcceptButton = ok; dialog.CancelButton = ok;
                dialog.ShowDialog(this);
            }
        }

        private void OpenPath(string path)
        {
            try
            {
                path = Path.GetFullPath(path);
                var existing = documents.Find(d => String.Equals(d.Path, path, StringComparison.OrdinalIgnoreCase));
                if (existing != null) { Send(new { type = "activate", id = existing.Id }); return; }
                StartupTrace.Mark("fileReadBegin");
                var doc = Document.Read(path);
                StartupTrace.Mark("fileReadEnd");
                documents.Add(doc); activeId = doc.Id; SendDocument(doc); AddRecent(path);
            }
            catch (Exception e) { ShowDialogMessage(L.T("openFailed", Isolate(path)) + "\n\n" + e.Message, "MD Show", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        }

        private void NewDocument() { var doc = new Document(); documents.Add(doc); activeId = doc.Id; SendDocument(doc); }
        private void AddRecent(string path)
        {
            preferences.Recent.RemoveAll(x => String.Equals(x, path, StringComparison.OrdinalIgnoreCase));
            preferences.Recent.Insert(0, path); preferences.Recent = preferences.Recent.Take(12).ToList();
            SavePreferences(); Send(new { type = "recent", paths = preferences.Recent });
        }

        private bool SaveDocument(Document doc, bool saveAs)
        {
            string target = doc.Path;
            if (saveAs || target.Length == 0)
                using (var dialog = new SaveFileDialog { Filter = "Markdown (*.md)|*.md|Markdown (*.markdown)|*.markdown|" + L.T("allFiles") + " (*.*)|*.*", DefaultExt = "md", FileName = doc.Name, InitialDirectory = target.Length == 0 ? "" : Path.GetDirectoryName(target) })
                { if (dialog.ShowDialog(this) != DialogResult.OK) return false; target = dialog.FileName; }
            if (documents.Any(d => d != doc && String.Equals(d.Path, target, StringComparison.OrdinalIgnoreCase)))
                throw new IOException(L.T("alreadyOpen"));
            if (String.Equals(target, doc.Path, StringComparison.OrdinalIgnoreCase) && doc.ChangedOnDisk())
            {
                var choice = ShowDialogMessage(L.T("saveConflictPrompt"), L.T("saveConflict"), MessageBoxButtons.YesNoCancel, MessageBoxIcon.Warning);
                if (choice == DialogResult.Cancel) return false;
                if (choice == DialogResult.No) return SaveDocument(doc, true);
            }
            try { doc.Save(target, false); }
            catch (EncoderFallbackException)
            {
                if (ShowDialogMessage(L.T("encodingPrompt"), L.T("encoding"), MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return false;
                doc.Save(target, true);
            }
            Send(new { type = "saved", document = View(doc) }); AddRecent(doc.Path); WriteRecovery(); Notify(L.T("saved"));
            return true;
        }

        private bool CanClose(Document doc)
        {
            if (!doc.Dirty) return true;
            var choice = ShowDialogMessage(L.T("savePrompt", Isolate(doc.Name)), L.T("unsaved"), MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
            return choice == DialogResult.No || (choice == DialogResult.Yes && SaveDocument(doc, false));
        }

        private async void OnClosing(object sender, FormClosingEventArgs e)
        {
            StartupTrace.Mark("closeRequested"); StartupTrace.Flush();
            if (allowClose) return;
            e.Cancel = true;
            if (closing || busy) return;
            closing = true;
            try
            {
                if (ready && !rendererFailed)
                {
                    var snapshot = json.Deserialize<List<Dictionary<string, object>>>(await web.CoreWebView2.ExecuteScriptAsync("window.mdShow.snapshot()"));
                    foreach (var item in snapshot) { var doc = documents.Find(d => d.Id == Str(item, "id")); if (doc != null) doc.Text = Str(item, "text"); }
                }
                foreach (var doc in documents) if (!CanClose(doc)) return;
                SavePreferences();
                if (recoveryLoaded && recoveryWritable && File.Exists(RecoveryFile)) File.Delete(RecoveryFile);
                allowClose = true; Close();
            }
            catch (Exception error) { WriteRecovery(); ShowDialogMessage(error.Message, "MD Show", MessageBoxButtons.OK, MessageBoxIcon.Error); }
            finally { closing = false; }
        }

        private void SavePreferences()
        {
            var bounds = WindowState == FormWindowState.Normal ? Bounds : RestoreBounds;
            preferences.Width = bounds.Width; preferences.Height = bounds.Height; preferences.Maximized = WindowState == FormWindowState.Maximized;
            try { Document.AtomicWrite(Path.Combine(dataDir, "settings.json"), Encoding.UTF8.GetBytes(json.Serialize(preferences))); }
            catch (IOException) { Notify(L.T("settingsFailed")); }
            catch (UnauthorizedAccessException) { Notify(L.T("settingsDenied")); }
        }

        private void WriteRecovery()
        {
            if (!recoveryLoaded || !recoveryWritable) return;
            try
            {
                var dirty = documents.Where(d => d.Dirty).ToArray();
                if (dirty.Length > 0) Document.AtomicWrite(RecoveryFile, Encoding.UTF8.GetBytes(json.Serialize(dirty)));
                else if (File.Exists(RecoveryFile)) File.Delete(RecoveryFile);
            }
            catch (IOException) { Notify(L.T("recoveryFailed")); }
            catch (UnauthorizedAccessException) { Notify(L.T("recoveryFailed")); }
        }

        private void Recover()
        {
            if (!File.Exists(RecoveryFile)) return;
            try
            {
                var recovered = json.Deserialize<List<Document>>(File.ReadAllText(RecoveryFile));
                if (recovered == null || recovered.Count == 0) return;
                if (ShowDialogMessage(L.T("recoveryPrompt"), L.T("recovery"), MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                    foreach (var doc in recovered) { documents.Add(doc); activeId = doc.Id; SendDocument(doc); }
                else File.Delete(RecoveryFile);
            }
            catch (Exception e)
            {
                string preserved = RecoveryFile + ".unreadable-" + Guid.NewGuid().ToString("N") + ".json";
                try { File.Copy(RecoveryFile, preserved); }
                catch (Exception) { recoveryWritable = false; preserved = RecoveryFile; }
                ShowDialogMessage(L.T("recoveryUnreadable", Isolate(preserved)) +
                    (recoveryWritable ? "" : "\n" + L.T("recoveryDisabled")) + "\n\n" + e.Message, "MD Show");
            }
        }

        private void ExportHtml(Document doc, string html)
        {
            using (var dialog = new SaveFileDialog { Filter = "HTML (*.html)|*.html", FileName = Path.GetFileNameWithoutExtension(doc.Name) + ".html" })
                if (dialog.ShowDialog(this) == DialogResult.OK) { Document.AtomicWrite(dialog.FileName, Encoding.UTF8.GetBytes(html)); Notify(L.T("htmlSaved")); }
        }

        private async Task RunSmokeTest()
        {
            // Let previously posted settings and document messages reach the page first.
            await Task.Delay(250);
            // Keep test source out of release packages. An explicit local CLI input
            // runs the complete source-tree suite against this exact executable.
            string script = "window.browserTestResult={passed:document.querySelector('#preview')&&window.mdShow?1:0,total:1,results:[]};";
            if (smokeScript != null)
                script = Encoding.UTF8.GetString(Document.ReadBytes(smokeScript.Substring("--smoke-script=".Length)));
            await web.CoreWebView2.ExecuteScriptAsync(script);
            string result = "null";
            for (int attempt = 0; attempt < 600 && result == "null"; attempt++)
            {
                await Task.Delay(50);
                result = await web.CoreWebView2.ExecuteScriptAsync("window.browserTestResult || null");
            }
            if (result == "null") result = "{\"error\":\"Browser tests timed out\",\"passed\":0,\"total\":1}";
            File.WriteAllText(Path.Combine(diagnosticsDir, "smoke-result.json"), result);
            var integration = new List<object>();
            if (unifiedTitleBar)
            {
                var previousState = WindowState;
                WindowState = FormWindowState.Normal;
                await Task.Delay(100);
                int restoredTop = web.PointToScreen(Point.Empty).Y, restoredWindowTop = Top, restoredHit = NativeMaximizeHitTest();
                var restoredBounds = Bounds;
                WindowState = FormWindowState.Maximized;
                await Task.Delay(100);
                var workArea = Screen.FromControl(this).WorkingArea;
                int maximizedTop = web.PointToScreen(Point.Empty).Y, maximizedHit = NativeMaximizeHitTest();
                WindowState = previousState;
                await Task.Delay(100);
                integration.Add(new { name = "Unified title bar keeps native maximize hit target and visible client top when restored/maximized",
                    pass = restoredTop == restoredWindowTop && maximizedTop == workArea.Top && restoredHit == 9 && maximizedHit == 9,
                    restoredTop, restoredWindowTop, restoredHit, maximizedTop, workAreaTop = workArea.Top, maximizedHit });
                integration.Add(new { name = "Maximize/restore preserves window bounds", pass = previousState != FormWindowState.Normal || Bounds == restoredBounds,
                    before = restoredBounds.ToString(), after = Bounds.ToString() });
            }
            int titleBarMode;
            bool pageDark = json.Deserialize<bool>(await web.CoreWebView2.ExecuteScriptAsync("document.body.classList.contains('dark')"));
            integration.Add(new { name = "Page and native frame retain the same theme after browser checks", pass = pageDark == IsDarkWindowTheme() });
            string previousTheme = preferences.Theme;
            preferences.Theme = "light"; ApplyWindowTheme();
            bool lightTitleBar = DwmGetWindowAttribute(Handle, UseImmersiveDarkMode, out titleBarMode, sizeof(int)) == 0 && titleBarMode == 0;
            preferences.Theme = "dark"; ApplyWindowTheme();
            bool darkTitleBar = DwmGetWindowAttribute(Handle, UseImmersiveDarkMode, out titleBarMode, sizeof(int)) == 0 && titleBarMode == (SystemInformation.HighContrast ? 0 : 1);
            preferences.Theme = "system"; ApplyWindowTheme();
            bool systemTitleBar = DwmGetWindowAttribute(Handle, UseImmersiveDarkMode, out titleBarMode, sizeof(int)) == 0 && titleBarMode == (IsDarkWindowTheme() ? 1 : 0);
            integration.Add(new { name = "Windows title bar supports light, dark and system themes", pass = lightTitleBar && darkTitleBar && systemTitleBar });
            preferences.Theme = previousTheme; ApplyWindowTheme();
            string sample = Path.Combine(dataDir, "Проверка с пробелами.md");
            File.WriteAllText(sample, "# Проверка\r\n\r\nТекст для PDF и сохранения.\r\n", new UTF8Encoding(true));
            OpenPath(sample);
            await Task.Delay(200);
            string source = await web.CoreWebView2.ExecuteScriptAsync("active().editor.value");
            integration.Add(new { name = "Native file to editor", pass = json.Deserialize<string>(source).StartsWith("# Проверка\n") });
            await web.CoreWebView2.ExecuteScriptAsync("active().editor.value += '\\nИзменение из редактора'; active().editor.dispatchEvent(new Event('input')); render();");
            await Task.Delay(200);
            var document = documents.Find(d => d.Path == sample);
            integration.Add(new { name = "Editor to native bridge", pass = document.Text.EndsWith("Изменение из редактора") });
            WriteRecovery();
            var recovery = json.Deserialize<List<Document>>(File.ReadAllText(RecoveryFile));
            var recoveredDocument = recovery.Find(d => d.Id == document.Id);
            integration.Add(new { name = "Recovery preserves unsaved content and disk hash", pass = recoveredDocument != null && recoveredDocument.Text == document.Text && recoveredDocument.DiskHash == document.DiskHash && recoveredDocument.Dirty });
            SaveDocument(document, false);
            integration.Add(new { name = "Native save preserves BOM and CRLF", pass = File.ReadAllBytes(sample).Take(3).SequenceEqual(new byte[] { 239, 187, 191 }) && File.ReadAllText(sample).Contains("\r\nИзменение") && !document.Dirty });
            string pdf = Path.Combine(dataDir, "preview.pdf");
            bool printed = await web.CoreWebView2.PrintToPdfAsync(pdf);
            integration.Add(new { name = "PDF export", pass = printed && Encoding.ASCII.GetString(File.ReadAllBytes(pdf), 0, 4) == "%PDF" });
            var exportSamples = new Dictionary<string, string> {
                { "en-US", "# Export check\n\nReadable text and **bold words**." },
                { "ru-RU", "# Проверка экспорта\n\nРусский текст и **жирный шрифт**." },
                { "de-DE", "# Exportprüfung\n\nGrüße, Straße und **lesbarer Text**." },
                { "ar", "# اختبار التصدير\n\nنص عربي واضح مع **كلمات بارزة**.\n\nEnglish 123 — العربية." },
                { "zh-CN", "# 导出检查\n\n中文段落与**粗体文字**。\n\n简体中文、标点与 English 123。" }
            };
            foreach (var exportSample in exportSamples)
            {
                string text = exportSample.Value + "\n\n| A | B |\n| --- | --- |\n| 123 | 456 |\n\n```text\nC:\\Documents\\sample.md\nhttps://example.com\n```";
                string exportScript = "window.qaExportReady=false; settings.Language=" + json.Serialize(exportSample.Key) + "; applyLanguage(); setMode('read'); receiveDocument(" +
                    json.Serialize(new { id = "export-check", path = "sample.md", name = "sample.md", text = text, original = text, encoding = "utf-8", newline = "LF" }) +
                    "); render().then(ok => { window.qaExportReady=ok; });";
                await web.CoreWebView2.ExecuteScriptAsync(exportScript);
                bool rendered = false;
                for (int attempt = 0; attempt < 80 && !rendered; attempt++)
                {
                    await Task.Delay(50);
                    rendered = await web.CoreWebView2.ExecuteScriptAsync("window.qaExportReady === true") == "true";
                }
                string localePdf = Path.Combine(dataDir, "export-" + exportSample.Key + ".pdf");
                bool exported = rendered && await web.CoreWebView2.PrintToPdfAsync(localePdf);
                integration.Add(new { name = "PDF export " + exportSample.Key, pass = exported && Encoding.ASCII.GetString(File.ReadAllBytes(localePdf), 0, 4) == "%PDF" });
            }
            File.WriteAllText(Path.Combine(diagnosticsDir, "integration-result.json"), json.Serialize(integration));
            var browserResults = json.Deserialize<Dictionary<string, object>>(result);
            var nativeResults = json.Deserialize<List<Dictionary<string, object>>>(json.Serialize(integration));
            Environment.ExitCode = browserResults.ContainsKey("total") &&
                Convert.ToInt32(browserResults["passed"]) == Convert.ToInt32(browserResults["total"]) &&
                nativeResults.All(check => Object.Equals(check["pass"], true)) ? 0 : 1;
            if (File.Exists(RecoveryFile)) File.Delete(RecoveryFile);
            recoveryTimer.Stop();
            allowClose = true;
            if (!keepSmokeOpen) Close();
        }
    }
}
