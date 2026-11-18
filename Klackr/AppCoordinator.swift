//
//  AppCoordinator.swift
//  Klackr
//
//  Wires app services together at launch
//

import Foundation
import Combine

final class AppCoordinator {
    private let settings: SettingsManager
    private let soundEngine: SoundEngine
    private let keyboardMonitor: KeyboardMonitor
    private let hotKeyManager: HotKeyManager
    private var cancellables = Set<AnyCancellable>()
    
    init(
        settings: SettingsManager,
        soundEngine: SoundEngine,
        keyboardMonitor: KeyboardMonitor,
        hotKeyManager: HotKeyManager? = nil
    ) {
        self.settings = settings
        self.soundEngine = soundEngine
        self.keyboardMonitor = keyboardMonitor
        self.hotKeyManager = hotKeyManager ?? HotKeyManager(shortcut: settings.hotKeyShortcut)
        
        configure()
    }
    
    private func configure() {
        soundEngine.volume = Float(settings.volume)
        soundEngine.loadSoundPack(settings.selectedSoundPack)
        
        soundEngine.mouseVolume = Float(settings.mouseVolume)
        soundEngine.loadMouseSoundPack(settings.selectedMouseSoundPack)
        
        keyboardMonitor.onKeyEvent = { [weak self] keyType, keyEvent in
            guard let self, self.settings.isEnabled else { return }
            self.soundEngine.playSound(for: keyType, event: keyEvent)
        }
        
        keyboardMonitor.onMouseEvent = { [weak self] button, mouseEvent in
            guard let self, self.settings.isEnabled, self.settings.isMouseSoundEnabled else { return }
            self.soundEngine.playMouseSound(for: button, event: mouseEvent)
        }

        hotKeyManager.onHotKeyPressed = { [weak self] in
            self?.settings.toggleEnabled()
        }

        // Automatically start monitoring if enabled and permission is already granted
        if settings.isEnabled && keyboardMonitor.checkPermission() {
            keyboardMonitor.startMonitoring()
        }
        
        settings.$isEnabled
            .dropFirst()
            .sink { [weak self] isEnabled in
                guard let self else { return }
                if isEnabled {
                    if self.keyboardMonitor.checkPermission() {
                        self.keyboardMonitor.startMonitoring()
                    }
                } else {
                    self.keyboardMonitor.stopMonitoring()
                }
            }
            .store(in: &cancellables)
        
        settings.$volume
            .dropFirst()
            .sink { [weak self] volume in
                self?.soundEngine.volume = Float(volume)
            }
            .store(in: &cancellables)
        
        settings.$mouseVolume
            .dropFirst()
            .sink { [weak self] volume in
                self?.soundEngine.mouseVolume = Float(volume)
            }
            .store(in: &cancellables)
        
        settings.$selectedSoundPackId
            .dropFirst()
            .sink { [weak self] _ in
                guard let self else { return }
                self.soundEngine.loadSoundPack(self.settings.selectedSoundPack)
            }
            .store(in: &cancellables)
        
        settings.$selectedMouseSoundPackId
            .dropFirst()
            .sink { [weak self] _ in
                guard let self else { return }
                self.soundEngine.loadMouseSoundPack(self.settings.selectedMouseSoundPack)
            }
            .store(in: &cancellables)
        
        Publishers.CombineLatest(settings.$hotKeyKeyCode, settings.$hotKeyModifierPresetRawValue)
            .dropFirst()
            .sink { [weak self] _, _ in
                guard let self else { return }
                self.hotKeyManager.updateHotKey(self.settings.hotKeyShortcut)
            }
            .store(in: &cancellables)
    }
}
