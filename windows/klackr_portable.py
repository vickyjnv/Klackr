"""
Klackr Portable for Windows / Cross-Platform
Mechanical keyboard sound app running in system tray.
"""

import os
import sys
import random
import threading
from pathlib import Path

try:
    import pygame
    from PIL import Image
    import pystray
    from pystray import MenuItem as item
    from pynput import keyboard, mouse
except ImportError:
    print("Please install requirements: pip install pygame pystray pynput Pillow")
    sys.exit(1)

BASE_DIR = Path(__file__).resolve().parent
SOUNDS_DIR = BASE_DIR / "Sounds"

class KlackrApp:
    def __init__(self):
        self.is_enabled = True
        self.is_mouse_enabled = True
        self.volume = 0.85
        self.mouse_volume = 0.85
        self.current_pack = "cherry-mx-brown"
        self.current_mouse_pack = "mouse-omron"
        self.sound_buffers = {}
        self.mouse_sound_buffers = {}
        
        # Init pygame mixer with low latency buffer
        pygame.mixer.pre_init(44100, -16, 2, 512)
        pygame.mixer.init()
        pygame.mixer.set_num_channels(24)
        
        self.load_sound_pack(self.current_pack)
        self.load_mouse_sound_pack(self.current_mouse_pack)
        self.setup_tray()
        self.setup_keyboard_hook()
        self.setup_mouse_hook()

    def load_mouse_sound_pack(self, pack_id: str):
        self.current_mouse_pack = pack_id
        self.mouse_sound_buffers.clear()

        if not SOUNDS_DIR.exists():
            return

        buttons = ["left", "right", "middle"]
        events = ["press", "release"]

        for btn in buttons:
            for ev in events:
                key = f"{btn}_{ev}"
                f = SOUNDS_DIR / f"{pack_id}_{btn}_{ev}.mp3"
                if not f.exists():
                    f = SOUNDS_DIR / f"{pack_id}_left_{ev}.mp3"
                if f.exists():
                    try:
                        self.mouse_sound_buffers[key] = [pygame.mixer.Sound(str(f))]
                    except Exception as e:
                        print("Error loading mouse sound:", e)

    def load_sound_pack(self, pack_id: str):
        self.current_pack = pack_id
        self.sound_buffers.clear()
        
        if not SOUNDS_DIR.exists():
            print(f"Sounds directory not found at {SOUNDS_DIR}")
            return

        events = ["press", "release"]
        key_types = ["regular", "space", "enter", "backspace"]

        for kt in key_types:
            for ev in events:
                key = f"{kt}_{ev}"
                files = []
                if kt in ["space", "enter", "backspace"]:
                    f = SOUNDS_DIR / f"{pack_id}_{kt}_{ev}.mp3"
                    if f.exists():
                        files.append(f)
                
                if not files:
                    if ev == "press":
                        for i in range(1, 6):
                            f = SOUNDS_DIR / f"{pack_id}_regular_press_{i}.mp3"
                            if f.exists():
                                files.append(f)
                    else:
                        f = SOUNDS_DIR / f"{pack_id}_regular_release.mp3"
                        if f.exists():
                            files.append(f)
                
                sounds = [pygame.mixer.Sound(str(path)) for path in files]
                if sounds:
                    self.sound_buffers[key] = sounds

    def play_sound(self, key_type: str, event: str):
        if not self.is_enabled:
            return

        key = f"{key_type}_{event}"
        sounds = self.sound_buffers.get(key)
        if not sounds:
            sounds = self.sound_buffers.get(f"regular_{event}")

        if sounds:
            snd = random.choice(sounds)
            var_vol = self.volume * random.uniform(0.92, 1.0)
            snd.set_volume(var_vol)
            snd.play()

    def play_mouse_sound(self, button_name: str, event: str):
        if not self.is_enabled or not self.is_mouse_enabled:
            return

        key = f"{button_name}_{event}"
        sounds = self.mouse_sound_buffers.get(key)
        if not sounds:
            sounds = self.mouse_sound_buffers.get(f"left_{event}")

        if sounds:
            snd = random.choice(sounds)
            var_vol = self.mouse_volume * random.uniform(0.92, 1.0)
            snd.set_volume(var_vol)
            snd.play()

    def classify_key(self, key) -> str:
        if key == keyboard.Key.space:
            return "space"
        elif key in (keyboard.Key.enter, getattr(keyboard.Key, "keypad_enter", None)):
            return "enter"
        elif key in (keyboard.Key.backspace, keyboard.Key.delete):
            return "backspace"
        return "regular"

    def setup_keyboard_hook(self):
        def on_press(key):
            kt = self.classify_key(key)
            self.play_sound(kt, "press")

        def on_release(key):
            kt = self.classify_key(key)
            self.play_sound(kt, "release")

        self.listener = keyboard.Listener(on_press=on_press, on_release=on_release)
        self.listener.daemon = True
        self.listener.start()

    def setup_mouse_hook(self):
        def on_click(x, y, button, pressed):
            btn_str = "left"
            if button == mouse.Button.right:
                btn_str = "right"
            elif button == mouse.Button.middle:
                btn_str = "middle"
            ev_str = "press" if pressed else "release"
            self.play_mouse_sound(btn_str, ev_str)

        self.mouse_listener = mouse.Listener(on_click=on_click)
        self.mouse_listener.daemon = True
        self.mouse_listener.start()

    def toggle_enabled(self):
        self.is_enabled = not self.is_enabled

    def toggle_mouse_enabled(self):
        self.is_mouse_enabled = not self.is_mouse_enabled

    def set_pack(self, pack_id: str):
        self.load_sound_pack(pack_id)

    def set_mouse_pack(self, pack_id: str):
        self.load_mouse_sound_pack(pack_id)

    def set_volume(self, val: float):
        self.volume = val

    def setup_tray(self):
        ico_path = BASE_DIR / "app.ico"
        if ico_path.exists():
            image = Image.open(ico_path)
        else:
            image = Image.new("RGB", (64, 64), color=(30, 30, 30))

        menu = pystray.Menu(
            item("Enabled", lambda icon, item: self.toggle_enabled(), checked=lambda item: self.is_enabled),
            pystray.Menu.SEPARATOR,
            item("Sound Pack", pystray.Menu(
                item("Cherry MX Brown (Tactile)", lambda icon, item: self.set_pack("cherry-mx-brown"), checked=lambda item: self.current_pack == "cherry-mx-brown"),
                item("Cherry MX Blue (Clicky)", lambda icon, item: self.set_pack("cherry-mx-blue"), checked=lambda item: self.current_pack == "cherry-mx-blue"),
                item("Cherry MX Red (Smooth Linear)", lambda icon, item: self.set_pack("cherry-mx-red"), checked=lambda item: self.current_pack == "cherry-mx-red"),
                item("Cherry MX Black (Heavy Linear)", lambda icon, item: self.set_pack("cherry-mx-black"), checked=lambda item: self.current_pack == "cherry-mx-black"),
                item("Topre Purple Hybrid (Thock)", lambda icon, item: self.set_pack("topre-purple"), checked=lambda item: self.current_pack == "topre-purple"),
                item("Everglide Oreo (Snappy Tactile)", lambda icon, item: self.set_pack("everglide-oreo"), checked=lambda item: self.current_pack == "everglide-oreo"),
            )),
            item("Mouse Clicks", lambda icon, item: self.toggle_mouse_enabled(), checked=lambda item: self.is_mouse_enabled),
            item("Mouse Sound Pack", pystray.Menu(
                item("Classic Omron Microswitch", lambda icon, item: self.set_mouse_pack("mouse-omron"), checked=lambda item: self.current_mouse_pack == "mouse-omron"),
                item("Logitech G Pro Superlight", lambda icon, item: self.set_mouse_pack("mouse-superlight"), checked=lambda item: self.current_mouse_pack == "mouse-superlight"),
                item("Razer Optical Switch", lambda icon, item: self.set_mouse_pack("mouse-razer"), checked=lambda item: self.current_mouse_pack == "mouse-razer"),
                item("Glorious Model O", lambda icon, item: self.set_mouse_pack("mouse-glorious"), checked=lambda item: self.current_mouse_pack == "mouse-glorious"),
                item("Silent Office Tactile", lambda icon, item: self.set_mouse_pack("mouse-silent"), checked=lambda item: self.current_mouse_pack == "mouse-silent"),
            )),
            item("Volume", pystray.Menu(
                item("100%", lambda icon, item: self.set_volume(1.0), checked=lambda item: abs(self.volume - 1.0) < 0.05),
                item("80%", lambda icon, item: self.set_volume(0.8), checked=lambda item: abs(self.volume - 0.8) < 0.05),
                item("60%", lambda icon, item: self.set_volume(0.6), checked=lambda item: abs(self.volume - 0.6) < 0.05),
                item("40%", lambda icon, item: self.set_volume(0.4), checked=lambda item: abs(self.volume - 0.4) < 0.05),
            )),
            pystray.Menu.SEPARATOR,
            item("Exit", lambda icon, item: icon.stop())
        )

        self.icon = pystray.Icon("Klackr", image, "Klackr", menu)

    def run(self):
        self.icon.run()

if __name__ == "__main__":
    app = KlackrApp()
    app.run()
