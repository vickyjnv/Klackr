using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using Klackr.Models;
using Klackr.Services;

namespace Klackr.UI
{
    public class TrayApplicationContext : ApplicationContext
    {
        private readonly NotifyIcon _trayIcon;
        private readonly SoundEngine _soundEngine;
        private readonly KeyboardHook _keyboardHook;
        private readonly MouseHook _mouseHook;
        private readonly HotkeyManager _hotkeyManager;

        private ToolStripMenuItem _enabledMenuItem = null!;
        private ToolStripMenuItem _mouseEnabledMenuItem = null!;
        private ToolStripMenuItem _startupMenuItem = null!;

        public TrayApplicationContext()
        {
            _soundEngine = new SoundEngine();
            _keyboardHook = new KeyboardHook();
            _mouseHook = new MouseHook();
            _hotkeyManager = new HotkeyManager();

            _keyboardHook.OnKeyEvent += (keyType, keyEvent) =>
            {
                _soundEngine.PlaySound(keyType, keyEvent);
            };

            _mouseHook.OnMouseEvent += (button, mouseEvent) =>
            {
                _soundEngine.PlayMouseSound(button, mouseEvent);
            };

            _hotkeyManager.OnHotkeyPressed += ToggleEnabled;

            Icon? appIcon = null;
            var icoPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "app.ico");
            if (File.Exists(icoPath))
            {
                try { appIcon = new Icon(icoPath); } catch { }
            }
            appIcon ??= SystemIcons.Application;

            _trayIcon = new NotifyIcon
            {
                Icon = appIcon,
                ContextMenuStrip = CreateContextMenu(),
                Visible = true,
                Text = "Klackr - Mechanical Keyboard Sounds"
            };

            _trayIcon.MouseClick += (s, e) =>
            {
                if (e.Button == MouseButtons.Left)
                {
                    ToggleEnabled();
                }
            };
        }

        private ContextMenuStrip CreateContextMenu()
        {
            var menu = new ContextMenuStrip();

            _enabledMenuItem = new ToolStripMenuItem("Enabled (Alt+Ctrl+K)", null, (s, e) => ToggleEnabled())
            {
                Checked = _soundEngine.IsEnabled,
                Font = new Font(menu.Font, FontStyle.Bold)
            };
            menu.Items.Add(_enabledMenuItem);

            menu.Items.Add(new ToolStripSeparator());

            // Volume Submenu
            var volumeMenu = new ToolStripMenuItem("Volume");
            int[] volumes = { 100, 80, 60, 40, 20 };
            foreach (var vol in volumes)
            {
                var item = new ToolStripMenuItem($"{vol}%", null, (s, e) => SetVolume(vol));
                if (Math.Abs(_soundEngine.Volume - (vol / 100f)) < 0.05f) item.Checked = true;
                volumeMenu.DropDownItems.Add(item);
            }
            menu.Items.Add(volumeMenu);

            // Sound Pack Submenu
            var packMenu = new ToolStripMenuItem("Sound Pack");
            var packs = new (string Name, SoundPackType Type)[]
            {
                ("Cherry MX Brown (Tactile)", SoundPackType.CherryMXBrown),
                ("Cherry MX Blue (Clicky)", SoundPackType.CherryMXBlue),
                ("Cherry MX Red (Smooth Linear)", SoundPackType.CherryMXRed),
                ("Cherry MX Black (Heavy Linear)", SoundPackType.CherryMXBlack),
                ("Topre Purple Hybrid (Thock)", SoundPackType.ToprePurple),
                ("Everglide Oreo (Snappy Tactile)", SoundPackType.EverglideOreo),
                ("Holy Panda (Deep Thock)", SoundPackType.HolyPanda),
                ("Kailh Box White (Click Bar)", SoundPackType.KailhBoxWhite),
                ("NovelKeys Cream (POM Clack)", SoundPackType.NKCream),
                ("Tealios V2 (Smooth Linear)", SoundPackType.TealiosV2),
                ("IBM Buckling Spring (Vintage)", SoundPackType.IBMBucklingSpring),
                ("SteelSeries Apex Pro (OmniPoint)", SoundPackType.SteelSeriesApexPro)
            };

            foreach (var p in packs)
            {
                var item = new ToolStripMenuItem(p.Name, null, (s, e) => SetSoundPack(p.Type))
                {
                    Checked = _soundEngine.CurrentPack == p.Type,
                    Tag = p.Type
                };
                packMenu.DropDownItems.Add(item);
            }

            packMenu.DropDownItems.Add(new ToolStripSeparator());
            packMenu.DropDownItems.Add(new ToolStripMenuItem("Open Sounds Folder...", null, (s, e) =>
            {
                try
                {
                    var soundsDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Sounds");
                    if (!Directory.Exists(soundsDir)) Directory.CreateDirectory(soundsDir);
                    Process.Start("explorer.exe", soundsDir);
                }
                catch { }
            }));
            packMenu.DropDownItems.Add(new ToolStripMenuItem("Download Community Packs (Mechvibes)...", null, (s, e) =>
            {
                try
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "https://mechvibes.com/soundpacks",
                        UseShellExecute = true
                    });
                }
                catch { }
            }));
            menu.Items.Add(packMenu);

            menu.Items.Add(new ToolStripSeparator());

            // Mouse Clicks Toggle
            _mouseEnabledMenuItem = new ToolStripMenuItem("Mouse Clicks", null, (s, e) => ToggleMouseEnabled())
            {
                Checked = _soundEngine.IsMouseEnabled
            };
            menu.Items.Add(_mouseEnabledMenuItem);

            // Mouse Pack Submenu
            var mousePackMenu = new ToolStripMenuItem("Mouse Sound Pack");
            var mousePacks = new (string Name, MouseSoundPackType Type)[]
            {
                ("Classic Omron Microswitch", MouseSoundPackType.ClassicOmron),
                ("Logitech G Pro Superlight", MouseSoundPackType.LogitechSuperlight),
                ("Razer Optical Switch", MouseSoundPackType.RazerOptical),
                ("Glorious Model O", MouseSoundPackType.GloriousModelO),
                ("Silent Office Tactile", MouseSoundPackType.SilentOffice),
                ("Microsoft IntelliMouse", MouseSoundPackType.IntelliMouse),
                ("Logitech G203 Prodigy", MouseSoundPackType.LogitechG203),
                ("A4Tech Bloody LK Optical", MouseSoundPackType.BloodyOptical),
                ("Rapture Venom Mechanical", MouseSoundPackType.RaptureVenom),
                ("Trust GXT Gaming", MouseSoundPackType.TrustGXT),
                ("Apple Magic Mouse", MouseSoundPackType.AppleMagic),
                ("Kailh GM 8.0 Black Mamba", MouseSoundPackType.KailhGM8)
            };

            foreach (var mp in mousePacks)
            {
                var item = new ToolStripMenuItem(mp.Name, null, (s, e) => SetMouseSoundPack(mp.Type))
                {
                    Checked = _soundEngine.CurrentMousePack == mp.Type,
                    Tag = mp.Type
                };
                mousePackMenu.DropDownItems.Add(item);
            }
            menu.Items.Add(mousePackMenu);

            menu.Items.Add(new ToolStripSeparator());

            // Startup Submenu
            _startupMenuItem = new ToolStripMenuItem("Launch at Windows Startup", null, (s, e) => ToggleStartup())
            {
                Checked = StartupManager.IsLaunchAtStartupEnabled()
            };
            menu.Items.Add(_startupMenuItem);

            menu.Items.Add(new ToolStripSeparator());

            // About
            menu.Items.Add(new ToolStripMenuItem("About Klackr...", null, (s, e) =>
            {
                try
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "https://github.com/vickyjnv/Klackr",
                        UseShellExecute = true
                    });
                }
                catch { }
            }));

            // Exit
            menu.Items.Add(new ToolStripMenuItem("Exit", null, (s, e) => ExitApplication()));

            return menu;
        }

        private void ToggleEnabled()
        {
            _soundEngine.IsEnabled = !_soundEngine.IsEnabled;
            _enabledMenuItem.Checked = _soundEngine.IsEnabled;
            _trayIcon.Text = _soundEngine.IsEnabled ? "Klackr (Enabled)" : "Klackr (Muted)";
        }

        private void SetVolume(int percent)
        {
            _soundEngine.Volume = percent / 100f;
            if (_trayIcon.ContextMenuStrip?.Items[2] is ToolStripMenuItem volMenu)
            {
                foreach (ToolStripMenuItem subItem in volMenu.DropDownItems)
                {
                    subItem.Checked = subItem.Text == $"{percent}%";
                }
            }
        }

        private void SetSoundPack(SoundPackType pack)
        {
            _soundEngine.LoadSoundPack(pack);
            if (_trayIcon.ContextMenuStrip?.Items[3] is ToolStripMenuItem packMenu)
            {
                foreach (ToolStripItem item in packMenu.DropDownItems)
                {
                    if (item is ToolStripMenuItem menuItem && menuItem.Tag is SoundPackType itemType)
                    {
                        menuItem.Checked = itemType == pack;
                    }
                }
            }
        }

        private void ToggleMouseEnabled()
        {
            _soundEngine.IsMouseEnabled = !_soundEngine.IsMouseEnabled;
            _mouseEnabledMenuItem.Checked = _soundEngine.IsMouseEnabled;
        }

        private void SetMouseSoundPack(MouseSoundPackType pack)
        {
            _soundEngine.LoadMouseSoundPack(pack);
            if (_trayIcon.ContextMenuStrip?.Items[6] is ToolStripMenuItem mouseMenu)
            {
                foreach (ToolStripItem item in mouseMenu.DropDownItems)
                {
                    if (item is ToolStripMenuItem menuItem && menuItem.Tag is MouseSoundPackType itemType)
                    {
                        menuItem.Checked = itemType == pack;
                    }
                }
            }
        }

        private void ToggleStartup()
        {
            var newState = !StartupManager.IsLaunchAtStartupEnabled();
            StartupManager.SetLaunchAtStartup(newState);
            _startupMenuItem.Checked = newState;
        }

        private void ExitApplication()
        {
            _trayIcon.Visible = false;
            _keyboardHook.Dispose();
            _mouseHook.Dispose();
            _hotkeyManager.Dispose();
            _soundEngine.Dispose();
            _trayIcon.Dispose();
            Application.Exit();
        }
    }
}
