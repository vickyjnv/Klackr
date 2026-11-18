using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using Klackr.Models;
using Klackr.Services;

namespace Klackr.UI
{
    public class SettingsForm : Form
    {
        private readonly SoundEngine _soundEngine;

        // Keyboard settings controls
        private ComboBox _keyboardPackCombo = null!;
        private TrackBar _keyboardVolumeTrackBar = null!;
        private Label _keyboardVolumeLabel = null!;
        private CheckBox _keyboardEnabledCheckBox = null!;
        private Button _testKeyboardSoundButton = null!;

        // Mouse settings controls
        private ComboBox _mousePackCombo = null!;
        private TrackBar _mouseVolumeTrackBar = null!;
        private Label _mouseVolumeLabel = null!;
        private CheckBox _mouseEnabledCheckBox = null!;
        private CheckBox _mouseLeftCheckBox = null!;
        private CheckBox _mouseRightCheckBox = null!;
        private CheckBox _mouseMiddleCheckBox = null!;
        private Button _testMouseSoundButton = null!;

        // General settings
        private CheckBox _startupCheckBox = null!;
        private CheckBox _startMinimizedCheckBox = null!;
        private Button _openSoundsFolderButton = null!;
        private Button _downloadPacksButton = null!;

        // Buttons
        private Button _okButton = null!;
        private Button _cancelButton = null!;
        private Button _applyButton = null!;

        // Track original values for cancel
        private SoundPackType _originalKeyboardPack;
        private float _originalKeyboardVolume;
        private bool _originalKeyboardEnabled;
        private MouseSoundPackType _originalMousePack;
        private float _originalMouseVolume;
        private bool _originalMouseEnabled;
        private bool _originalStartup;

        public SettingsForm(SoundEngine soundEngine)
        {
            _soundEngine = soundEngine;

            InitializeComponent();
            LoadSettings();
        }

        private void InitializeComponent()
        {
            this.Text = "Klackr Settings";
            this.Size = new Size(600, 550);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.ShowInTaskbar = false;
            this.Icon = GetAppIcon();

            var mainPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(15)
            };
            this.Controls.Add(mainPanel);

            // Create tab control for organized settings
            var tabControl = new TabControl
            {
                Dock = DockStyle.Fill
            };
            mainPanel.Controls.Add(tabControl);

            // ===== KEYBOARD TAB =====
            var keyboardTab = new TabPage("Keyboard Sounds");
            tabControl.TabPages.Add(keyboardTab);
            CreateKeyboardTab(keyboardTab);

            // ===== MOUSE TAB =====
            var mouseTab = new TabPage("Mouse Sounds");
            tabControl.TabPages.Add(mouseTab);
            CreateMouseTab(mouseTab);

            // ===== GENERAL TAB =====
            var generalTab = new TabPage("General");
            tabControl.TabPages.Add(generalTab);
            CreateGeneralTab(generalTab);

            // ===== BOTTOM BUTTONS =====
            var buttonPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                Height = 50,
                FlowDirection = FlowDirection.RightToLeft,
                Padding = new Padding(0, 10, 0, 0)
            };
            mainPanel.Controls.Add(buttonPanel);

            _cancelButton = new Button
            {
                Text = "Cancel",
                Size = new Size(90, 32),
                DialogResult = DialogResult.Cancel
            };
            _cancelButton.Click += (s, e) => RestoreOriginalSettings();
            buttonPanel.Controls.Add(_cancelButton);

            _applyButton = new Button
            {
                Text = "Apply",
                Size = new Size(90, 32)
            };
            _applyButton.Click += (s, e) => ApplySettings();
            buttonPanel.Controls.Add(_applyButton);

            _okButton = new Button
            {
                Text = "OK",
                Size = new Size(90, 32),
                DialogResult = DialogResult.OK
            };
            _okButton.Click += (s, e) => { ApplySettings(); };
            buttonPanel.Controls.Add(_okButton);

            this.AcceptButton = _okButton;
            this.CancelButton = _cancelButton;
        }

        private Icon GetAppIcon()
        {
            try
            {
                var icoPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "app.ico");
                if (File.Exists(icoPath))
                    return new Icon(icoPath);
            }
            catch { }
            try
            {
                var assembly = System.Reflection.Assembly.GetExecutingAssembly();
                using var stream = assembly.GetManifestResourceStream("Klackr.app.ico");
                if (stream != null)
                    return new Icon(stream);
            }
            catch { }
            return SystemIcons.Application;
        }

        private void CreateKeyboardTab(TabPage tab)
        {
            var panel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(10) };
            tab.Controls.Add(panel);

            var y = 15;

            // Enabled checkbox
            _keyboardEnabledCheckBox = new CheckBox
            {
                Text = "Enable Keyboard Sounds",
                Location = new Point(10, y),
                AutoSize = true,
                Font = new Font(this.Font, FontStyle.Bold)
            };
            _keyboardEnabledCheckBox.CheckedChanged += (s, e) => UpdateKeyboardControls();
            panel.Controls.Add(_keyboardEnabledCheckBox);
            y += 35;

            // Sound Pack
            var packLabel = new Label
            {
                Text = "Sound Pack:",
                Location = new Point(10, y),
                AutoSize = true
            };
            panel.Controls.Add(packLabel);
            y += 25;

            _keyboardPackCombo = new ComboBox
            {
                Location = new Point(10, y),
                Width = 350,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            foreach (SoundPackType pack in Enum.GetValues(typeof(SoundPackType)))
            {
                _keyboardPackCombo.Items.Add(new PackItem(pack, GetPackDisplayName(pack)));
            }
            _keyboardPackCombo.SelectedIndexChanged += (s, e) => OnKeyboardPackChanged();
            panel.Controls.Add(_keyboardPackCombo);
            y += 40;

            // Test sound button
            _testKeyboardSoundButton = new Button
            {
                Text = "🔊 Test Sound",
                Location = new Point(10, y),
                Size = new Size(120, 32)
            };
            _testKeyboardSoundButton.Click += (s, e) => TestKeyboardSound();
            panel.Controls.Add(_testKeyboardSoundButton);
            y += 45;

            // Volume
            var volumeLabel = new Label
            {
                Text = "Volume:",
                Location = new Point(10, y),
                AutoSize = true
            };
            panel.Controls.Add(volumeLabel);
            y += 25;

            var volumePanel = new FlowLayoutPanel
            {
                Location = new Point(10, y),
                Width = 400,
                Height = 40,
                FlowDirection = FlowDirection.LeftToRight
            };
            panel.Controls.Add(volumePanel);

            _keyboardVolumeTrackBar = new TrackBar
            {
                Minimum = 0,
                Maximum = 100,
                TickFrequency = 10,
                Width = 250,
                Height = 40
            };
            _keyboardVolumeTrackBar.Scroll += (s, e) => UpdateKeyboardVolumeLabel();
            volumePanel.Controls.Add(_keyboardVolumeTrackBar);

            _keyboardVolumeLabel = new Label
            {
                Text = "100%",
                AutoSize = true,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(10, 8, 0, 0)
            };
            volumePanel.Controls.Add(_keyboardVolumeLabel);
            y += 50;

            // Info label
            var infoLabel = new Label
            {
                Text = "Press Alt+Ctrl+K to toggle sounds globally. Hotkey works even when this window is closed.",
                Location = new Point(10, y),
                Width = 540,
                Height = 40,
                ForeColor = SystemColors.GrayText
            };
            panel.Controls.Add(infoLabel);
        }

        private void CreateMouseTab(TabPage tab)
        {
            var panel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(10) };
            tab.Controls.Add(panel);

            var y = 15;

            // Enabled checkbox
            _mouseEnabledCheckBox = new CheckBox
            {
                Text = "Enable Mouse Click Sounds",
                Location = new Point(10, y),
                AutoSize = true,
                Font = new Font(this.Font, FontStyle.Bold)
            };
            _mouseEnabledCheckBox.CheckedChanged += (s, e) => UpdateMouseControls();
            panel.Controls.Add(_mouseEnabledCheckBox);
            y += 35;

            // Mouse button toggles
            var buttonsLabel = new Label
            {
                Text = "Mouse Buttons:",
                Location = new Point(10, y),
                AutoSize = true
            };
            panel.Controls.Add(buttonsLabel);
            y += 25;

            _mouseLeftCheckBox = new CheckBox
            {
                Text = "Left Click",
                Location = new Point(30, y),
                AutoSize = true
            };
            panel.Controls.Add(_mouseLeftCheckBox);

            _mouseRightCheckBox = new CheckBox
            {
                Text = "Right Click",
                Location = new Point(150, y),
                AutoSize = true
            };
            panel.Controls.Add(_mouseRightCheckBox);

            _mouseMiddleCheckBox = new CheckBox
            {
                Text = "Middle Click",
                Location = new Point(280, y),
                AutoSize = true
            };
            panel.Controls.Add(_mouseMiddleCheckBox);
            y += 35;

            // Sound Pack
            var packLabel = new Label
            {
                Text = "Sound Pack:",
                Location = new Point(10, y),
                AutoSize = true
            };
            panel.Controls.Add(packLabel);
            y += 25;

            _mousePackCombo = new ComboBox
            {
                Location = new Point(10, y),
                Width = 350,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            foreach (MouseSoundPackType pack in Enum.GetValues(typeof(MouseSoundPackType)))
            {
                _mousePackCombo.Items.Add(new PackItem(pack, GetMousePackDisplayName(pack)));
            }
            _mousePackCombo.SelectedIndexChanged += (s, e) => OnMousePackChanged();
            panel.Controls.Add(_mousePackCombo);
            y += 40;

            // Test sound button
            _testMouseSoundButton = new Button
            {
                Text = "🔊 Test Sound",
                Location = new Point(10, y),
                Size = new Size(120, 32)
            };
            _testMouseSoundButton.Click += (s, e) => TestMouseSound();
            panel.Controls.Add(_testMouseSoundButton);
            y += 45;

            // Volume
            var volumeLabel = new Label
            {
                Text = "Volume:",
                Location = new Point(10, y),
                AutoSize = true
            };
            panel.Controls.Add(volumeLabel);
            y += 25;

            var volumePanel = new FlowLayoutPanel
            {
                Location = new Point(10, y),
                Width = 400,
                Height = 40,
                FlowDirection = FlowDirection.LeftToRight
            };
            panel.Controls.Add(volumePanel);

            _mouseVolumeTrackBar = new TrackBar
            {
                Minimum = 0,
                Maximum = 100,
                TickFrequency = 10,
                Width = 250,
                Height = 40
            };
            _mouseVolumeTrackBar.Scroll += (s, e) => UpdateMouseVolumeLabel();
            volumePanel.Controls.Add(_mouseVolumeTrackBar);

            _mouseVolumeLabel = new Label
            {
                Text = "100%",
                AutoSize = true,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(10, 8, 0, 0)
            };
            volumePanel.Controls.Add(_mouseVolumeLabel);
        }

        private void CreateGeneralTab(TabPage tab)
        {
            var panel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(10) };
            tab.Controls.Add(panel);

            var y = 15;

            // Startup
            _startupCheckBox = new CheckBox
            {
                Text = "Launch Klackr at Windows startup",
                Location = new Point(10, y),
                AutoSize = true
            };
            panel.Controls.Add(_startupCheckBox);
            y += 35;

            // Start minimized
            _startMinimizedCheckBox = new CheckBox
            {
                Text = "Start minimized to system tray",
                Location = new Point(10, y),
                AutoSize = true
            };
            panel.Controls.Add(_startMinimizedCheckBox);
            y += 45;

            // Sound folder button
            _openSoundsFolderButton = new Button
            {
                Text = "📁 Open Sounds Folder",
                Location = new Point(10, y),
                Size = new Size(180, 36)
            };
            _openSoundsFolderButton.Click += (s, e) => OpenSoundsFolder();
            panel.Controls.Add(_openSoundsFolderButton);
            y += 45;

            // Download packs button
            _downloadPacksButton = new Button
            {
                Text = "🌐 Download Community Packs (Mechvibes)",
                Location = new Point(10, y),
                Size = new Size(300, 36)
            };
            _downloadPacksButton.Click += (s, e) => DownloadPacks();
            panel.Controls.Add(_downloadPacksButton);
            y += 50;

            // Version info
            var versionLabel = new Label
            {
                Text = $"Klackr v1.0.0\nMechanical keyboard & mouse sound simulator\n\nBuilt with .NET 8 & NAudio\nhttps://github.com/vickyjnv/Klackr",
                Location = new Point(10, y),
                Width = 540,
                Height = 100,
                ForeColor = SystemColors.GrayText
            };
            panel.Controls.Add(versionLabel);
        }

        private void LoadSettings()
        {
            // Store originals for cancel
            _originalKeyboardPack = _soundEngine.CurrentPack;
            _originalKeyboardVolume = _soundEngine.Volume;
            _originalKeyboardEnabled = _soundEngine.IsEnabled;
            _originalMousePack = _soundEngine.CurrentMousePack;
            _originalMouseVolume = _soundEngine.MouseVolume;
            _originalMouseEnabled = _soundEngine.IsMouseEnabled;
            _originalStartup = StartupManager.IsLaunchAtStartupEnabled();

            // Load keyboard settings
            _keyboardEnabledCheckBox.Checked = _soundEngine.IsEnabled;
            _keyboardVolumeTrackBar.Value = (int)(_soundEngine.Volume * 100);
            UpdateKeyboardVolumeLabel();
            SelectComboItem(_keyboardPackCombo, _soundEngine.CurrentPack);

            // Load mouse settings
            _mouseEnabledCheckBox.Checked = _soundEngine.IsMouseEnabled;
            _mouseVolumeTrackBar.Value = (int)(_soundEngine.MouseVolume * 100);
            UpdateMouseVolumeLabel();
            SelectComboItem(_mousePackCombo, _soundEngine.CurrentMousePack);

            // Mouse buttons (default all enabled)
            _mouseLeftCheckBox.Checked = true;
            _mouseRightCheckBox.Checked = true;
            _mouseMiddleCheckBox.Checked = true;

            // General
            _startupCheckBox.Checked = _originalStartup;
            _startMinimizedCheckBox.Checked = true;

            UpdateKeyboardControls();
            UpdateMouseControls();
        }

        private void SelectComboItem(ComboBox combo, Enum value)
        {
            for (int i = 0; i < combo.Items.Count; i++)
            {
                if (combo.Items[i] is PackItem item && item.Value.Equals(value))
                {
                    combo.SelectedIndex = i;
                    break;
                }
            }
        }

        private string GetPackDisplayName(SoundPackType pack)
        {
            return pack switch
            {
                SoundPackType.CherryMXBrown => "Cherry MX Brown (Tactile, Warm) ★",
                SoundPackType.CherryMXBlue => "Cherry MX Blue (Clicky, Sharp)",
                SoundPackType.CherryMXRed => "Cherry MX Red (Smooth Linear)",
                SoundPackType.CherryMXBlack => "Cherry MX Black (Heavy Linear)",
                SoundPackType.ToprePurple => "Topre Purple Hybrid (Deep Thock)",
                SoundPackType.EverglideOreo => "Everglide Oreo (Snappy Tactile)",
                SoundPackType.HolyPanda => "Holy Panda (Deep Round Thock) ★",
                SoundPackType.KailhBoxWhite => "Kailh Box White (Click Bar)",
                SoundPackType.NKCream => "NovelKeys Cream (POM Marbly Clack)",
                SoundPackType.TealiosV2 => "Tealios V2 (Ultra Smooth Linear)",
                SoundPackType.IBMBucklingSpring => "IBM Buckling Spring (Vintage Model M) ★",
                SoundPackType.SteelSeriesApexPro => "SteelSeries Apex Pro (OmniPoint Magnetic)",
                _ => pack.ToString()
            };
        }

        private string GetMousePackDisplayName(MouseSoundPackType pack)
        {
            return pack switch
            {
                MouseSoundPackType.ClassicOmron => "Classic Omron Microswitch (Balanced) ★",
                MouseSoundPackType.LogitechSuperlight => "Logitech G Pro Superlight (Esports)",
                MouseSoundPackType.RazerOptical => "Razer Optical Switch (Fast Optical)",
                MouseSoundPackType.GloriousModelO => "Glorious Model O (Kailh Switch)",
                MouseSoundPackType.SilentOffice => "Silent Office Tactile (Muted)",
                MouseSoundPackType.IntelliMouse => "Microsoft IntelliMouse (Legendary)",
                MouseSoundPackType.LogitechG203 => "Logitech G203 Prodigy (Budget Gaming)",
                MouseSoundPackType.BloodyOptical => "A4Tech Bloody LK Optical (Infrared)",
                MouseSoundPackType.RaptureVenom => "Rapture Venom (Heavy Mechanical)",
                MouseSoundPackType.TrustGXT => "Trust GXT Gaming (Solid Punchy)",
                MouseSoundPackType.AppleMagic => "Apple Magic Mouse (Glass/Aluminum)",
                MouseSoundPackType.KailhGM8 => "Kailh GM 8.0 Black Mamba (80M Esports) ★",
                _ => pack.ToString()
            };
        }

        private void UpdateKeyboardControls()
        {
            bool enabled = _keyboardEnabledCheckBox.Checked;
            _keyboardPackCombo.Enabled = enabled;
            _keyboardVolumeTrackBar.Enabled = enabled;
            _testKeyboardSoundButton.Enabled = enabled;
        }

        private void UpdateMouseControls()
        {
            bool enabled = _mouseEnabledCheckBox.Checked;
            _mousePackCombo.Enabled = enabled;
            _mouseVolumeTrackBar.Enabled = enabled;
            _mouseLeftCheckBox.Enabled = enabled;
            _mouseRightCheckBox.Enabled = enabled;
            _mouseMiddleCheckBox.Enabled = enabled;
            _testMouseSoundButton.Enabled = enabled;
        }

        private void UpdateKeyboardVolumeLabel()
        {
            _keyboardVolumeLabel.Text = $"{_keyboardVolumeTrackBar.Value}%";
        }

        private void UpdateMouseVolumeLabel()
        {
            _mouseVolumeLabel.Text = $"{_mouseVolumeTrackBar.Value}%";
        }

        private void OnKeyboardPackChanged()
        {
            if (_keyboardPackCombo.SelectedItem is PackItem item)
            {
                _soundEngine.LoadSoundPack((SoundPackType)item.Value);
            }
        }

        private void OnMousePackChanged()
        {
            if (_mousePackCombo.SelectedItem is PackItem item)
            {
                _soundEngine.LoadMouseSoundPack((MouseSoundPackType)item.Value);
            }
        }

        private void TestKeyboardSound()
        {
            _soundEngine.PlaySound(KeyType.Regular, KeyEvent.Press);
            System.Threading.Tasks.Task.Delay(50).ContinueWith(_ =>
            {
                this.Invoke(() => _soundEngine.PlaySound(KeyType.Regular, KeyEvent.Release));
            });
        }

        private void TestMouseSound()
        {
            _soundEngine.PlayMouseSound(MouseButton.Left, MouseEventType.Press);
            System.Threading.Tasks.Task.Delay(50).ContinueWith(_ =>
            {
                this.Invoke(() => _soundEngine.PlayMouseSound(MouseButton.Left, MouseEventType.Release));
            });
        }

        private void OpenSoundsFolder()
        {
            try
            {
                var soundsDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Sounds");
                if (!Directory.Exists(soundsDir)) Directory.CreateDirectory(soundsDir);
                System.Diagnostics.Process.Start("explorer.exe", soundsDir);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to open sounds folder: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void DownloadPacks()
        {
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "https://mechvibes.com/soundpacks",
                    UseShellExecute = true
                });
            }
            catch { }
        }

        private void ApplySettings()
        {
            // Apply keyboard settings
            _soundEngine.IsEnabled = _keyboardEnabledCheckBox.Checked;
            _soundEngine.Volume = _keyboardVolumeTrackBar.Value / 100f;

            // Apply mouse settings
            _soundEngine.IsMouseEnabled = _mouseEnabledCheckBox.Checked;
            _soundEngine.MouseVolume = _mouseVolumeTrackBar.Value / 100f;

            // Apply startup
            var newStartup = _startupCheckBox.Checked;
            if (newStartup != _originalStartup)
            {
                StartupManager.SetLaunchAtStartup(newStartup);
                _originalStartup = newStartup;
            }

            // Update originals so cancel doesn't revert
            _originalKeyboardPack = _soundEngine.CurrentPack;
            _originalKeyboardVolume = _soundEngine.Volume;
            _originalKeyboardEnabled = _soundEngine.IsEnabled;
            _originalMousePack = _soundEngine.CurrentMousePack;
            _originalMouseVolume = _soundEngine.MouseVolume;
            _originalMouseEnabled = _soundEngine.IsMouseEnabled;
        }

        private void RestoreOriginalSettings()
        {
            _soundEngine.LoadSoundPack(_originalKeyboardPack);
            _soundEngine.Volume = _originalKeyboardVolume;
            _soundEngine.IsEnabled = _originalKeyboardEnabled;
            _soundEngine.LoadMouseSoundPack(_originalMousePack);
            _soundEngine.MouseVolume = _originalMouseVolume;
            _soundEngine.IsMouseEnabled = _originalMouseEnabled;
        }

        private class PackItem
        {
            public Enum Value { get; }
            public string DisplayName { get; }

            public PackItem(Enum value, string displayName)
            {
                Value = value;
                DisplayName = displayName;
            }

            public override string ToString() => DisplayName;
        }
    }
}