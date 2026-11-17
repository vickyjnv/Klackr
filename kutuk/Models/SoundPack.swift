//
//  SoundPack.swift
//  Klackr
//
//  Models for sound pack data and dynamic pack loading
//

import Foundation

/// Represents the type of key being pressed
enum KeyType: String, CaseIterable {
    case regular    // Standard alphanumeric keys
    case space      // Spacebar
    case enter      // Enter/Return
    case backspace  // Backspace/Delete
    case modifier   // Shift, Cmd, Option, Control
}

/// Represents the keyboard event type
enum KeyEvent {
    case press
    case release
}

/// Represents a complete sound pack with all key sounds
struct SoundPack: Identifiable, Hashable {
    let id: String
    let name: String
    let description: String
    var isCustom: Bool = false
    var customDirectoryURL: URL? = nil
    
    /// Directory where user-downloaded / imported sound packs are saved
    static var customPacksDirectory: URL {
        let appSupport = FileManager.default.urls(for: .applicationSupportDirectory, in: .userDomainMask).first!
        let dir = appSupport.appendingPathComponent("Klackr/SoundPacks", isDirectory: true)
        if !FileManager.default.fileExists(atPath: dir.path) {
            try? FileManager.default.createDirectory(at: dir, withIntermediateDirectories: true)
        }
        return dir
    }
    
    /// Get sound file URLs for a specific key type and event
    func soundURLs(for keyType: KeyType, event: KeyEvent) -> [URL] {
        let eventSuffix = event == .press ? "press" : "release"
        let prefix = "\(id)_\(keyType.rawValue)_\(eventSuffix)"
        let extensions = ["mp3", "wav", "caf", "ogg"]
        
        var urls: [URL] = []
        
        // Check custom directory if this is an imported or downloaded pack
        let searchDirectories: [URL] = [
            customDirectoryURL,
            Self.customPacksDirectory.appendingPathComponent(id),
            Self.customPacksDirectory
        ].compactMap { $0 }
        
        for dir in searchDirectories {
            guard FileManager.default.fileExists(atPath: dir.path) else { continue }
            
            // Try numbered variations (e.g. cherry-mx-red_regular_press_1.mp3)
            for i in 1...5 {
                for ext in extensions {
                    let fileURL = dir.appendingPathComponent("\(prefix)_\(i).\(ext)")
                    if FileManager.default.fileExists(atPath: fileURL.path) {
                        urls.append(fileURL)
                        break
                    }
                }
            }
            
            // Fall back to unnumbered file
            if urls.isEmpty {
                for ext in extensions {
                    let fileURL = dir.appendingPathComponent("\(prefix).\(ext)")
                    if FileManager.default.fileExists(atPath: fileURL.path) {
                        urls.append(fileURL)
                        break
                    }
                }
            }
            
            if !urls.isEmpty { break }
        }
        
        // If not found in custom directories, look in bundled resources
        if urls.isEmpty {
            for i in 1...5 {
                for ext in extensions {
                    if let url = Bundle.main.url(forResource: "\(prefix)_\(i)", withExtension: ext) {
                        urls.append(url)
                        break
                    }
                }
            }
            
            if urls.isEmpty {
                for ext in extensions {
                    if let url = Bundle.main.url(forResource: prefix, withExtension: ext) {
                        urls.append(url)
                        break
                    }
                }
            }
        }
        
        // If no specific sound found for space/enter/backspace, fall back to regular key sound
        if urls.isEmpty && keyType != .regular {
            return soundURLs(for: .regular, event: event)
        }
        
        return urls
    }
    
    static func == (lhs: SoundPack, rhs: SoundPack) -> Bool {
        lhs.id == rhs.id
    }
    
    func hash(into hasher: inout Hasher) {
        hasher.combine(id)
    }
}

// MARK: - Pre-bundled & Discovered Sound Packs

extension SoundPack {
    static let cherryMXBrown = SoundPack(
        id: "cherry-mx-brown",
        name: "Cherry MX Brown",
        description: "Tactile, warm, full travel"
    )
    
    static let cherryMXBlue = SoundPack(
        id: "cherry-mx-blue",
        name: "Cherry MX Blue",
        description: "Clicky, bright, crisp click"
    )
    
    static let cherryMXRed = SoundPack(
        id: "cherry-mx-red",
        name: "Cherry MX Red",
        description: "Smooth, light, quiet linear"
    )
    
    static let cherryMXBlack = SoundPack(
        id: "cherry-mx-black",
        name: "Cherry MX Black",
        description: "Vintage, heavy, deep linear"
    )
    
    static let toprePurple = SoundPack(
        id: "topre-purple",
        name: "Topre Purple Hybrid",
        description: "Electro-capacitive, deep muted thock"
    )
    
    static let everglideOreo = SoundPack(
        id: "everglide-oreo",
        name: "Everglide Oreo",
        description: "Crisp, snappy modern tactile"
    )
    
    static let defaultPack = cherryMXBrown
    
    /// All bundled sound packs included with the app
    static let bundledPacks: [SoundPack] = [
        .cherryMXBrown,
        .cherryMXBlue,
        .cherryMXRed,
        .cherryMXBlack,
        .toprePurple,
        .everglideOreo
    ]
    
    /// Dynamically discovered user packs from Application Support
    static var customPacks: [SoundPack] {
        let dir = customPacksDirectory
        guard let items = try? FileManager.default.contentsOfDirectory(at: dir, includingPropertiesForKeys: [.isDirectoryKey]) else {
            return []
        }
        
        var packs: [SoundPack] = []
        for item in items {
            var isDir: ObjCBool = false
            if FileManager.default.fileExists(atPath: item.path, isDirectory: &isDir), isDir.boolValue {
                let packId = item.lastPathComponent
                let packName = packId.split(separator: "-").map { $0.capitalized }.joined(separator: " ")
                packs.append(SoundPack(
                    id: packId,
                    name: packName,
                    description: "Custom user sound pack",
                    isCustom: true,
                    customDirectoryURL: item
                ))
            }
        }
        return packs
    }
    
    /// All available sound packs (bundled + custom)
    static var allPacks: [SoundPack] {
        bundledPacks + customPacks
    }
    
    static var visiblePacks: [SoundPack] {
        allPacks
    }
}

// MARK: - Mouse Input & Sound Models

enum MouseButton: String, CaseIterable, Identifiable {
    case left
    case right
    case middle
    
    var id: String { rawValue }
}

enum MouseEvent: String, CaseIterable {
    case press
    case release
}

struct MouseSoundPack: Identifiable, Hashable {
    let id: String
    let name: String
    let description: String
    var isCustom: Bool = false
    var customDirectoryURL: URL? = nil
    
    static var customPacksDirectory: URL {
        let appSupport = FileManager.default.urls(for: .applicationSupportDirectory, in: .userDomainMask).first!
        let dir = appSupport.appendingPathComponent("Klackr/MouseSoundPacks", isDirectory: true)
        if !FileManager.default.fileExists(atPath: dir.path) {
            try? FileManager.default.createDirectory(at: dir, withIntermediateDirectories: true)
        }
        return dir
    }
    
    func soundURLs(for button: MouseButton, event: MouseEvent) -> [URL] {
        let eventSuffix = event == .press ? "press" : "release"
        let prefix = "\(id)_\(button.rawValue)_\(eventSuffix)"
        let extensions = ["mp3", "wav", "caf", "ogg", "flac"]
        
        var urls: [URL] = []
        
        let searchDirectories: [URL] = [
            customDirectoryURL,
            Self.customPacksDirectory.appendingPathComponent(id),
            Self.customPacksDirectory
        ].compactMap { $0 }
        
        for dir in searchDirectories {
            guard FileManager.default.fileExists(atPath: dir.path) else { continue }
            for ext in extensions {
                let fileURL = dir.appendingPathComponent("\(prefix).\(ext)")
                if FileManager.default.fileExists(atPath: fileURL.path) {
                    urls.append(fileURL)
                    break
                }
            }
            if !urls.isEmpty { break }
        }
        
        if urls.isEmpty {
            for ext in extensions {
                if let url = Bundle.main.url(forResource: prefix, withExtension: ext) {
                    urls.append(url)
                    break
                }
            }
        }
        
        // Fall back to left button sound if specific button sound not found
        if urls.isEmpty && button != .left {
            return soundURLs(for: .left, event: event)
        }
        
        return urls
    }
}

// MARK: - Bundled Mouse Sound Packs

extension MouseSoundPack {
    static let omron = MouseSoundPack(
        id: "mouse-omron",
        name: "Classic Omron",
        description: "Balanced, crisp dual-stage mechanical microswitch"
    )
    
    static let superlight = MouseSoundPack(
        id: "mouse-superlight",
        name: "Logitech G Pro Superlight",
        description: "Tactile, sharp esports gaming microswitch"
    )
    
    static let razerOptical = MouseSoundPack(
        id: "mouse-razer",
        name: "Razer Optical Switch",
        description: "Ultra-fast, punchy optical actuation click"
    )
    
    static let glorious = MouseSoundPack(
        id: "mouse-glorious",
        name: "Glorious Model O",
        description: "Lightweight, snappy Kailh microswitch"
    )
    
    static let silent = MouseSoundPack(
        id: "mouse-silent",
        name: "Silent Office Tactile",
        description: "Muted, dampened soft click for quiet focus"
    )
    
    static let defaultMousePack = omron
    
    static let bundledPacks: [MouseSoundPack] = [
        .omron,
        .superlight,
        .razerOptical,
        .glorious,
        .silent
    ]
    
    static var customPacks: [MouseSoundPack] {
        let dir = customPacksDirectory
        guard let items = try? FileManager.default.contentsOfDirectory(at: dir, includingPropertiesForKeys: [.isDirectoryKey]) else {
            return []
        }
        
        var packs: [MouseSoundPack] = []
        for item in items {
            var isDir: ObjCBool = false
            if FileManager.default.fileExists(atPath: item.path, isDirectory: &isDir), isDir.boolValue {
                let packId = item.lastPathComponent
                let packName = packId.split(separator: "-").map { $0.capitalized }.joined(separator: " ")
                packs.append(MouseSoundPack(
                    id: packId,
                    name: packName,
                    description: "Custom user mouse sound pack",
                    isCustom: true,
                    customDirectoryURL: item
                ))
            }
        }
        return packs
    }
    
    static var allPacks: [MouseSoundPack] {
        bundledPacks + customPacks
    }
    
    static var visiblePacks: [MouseSoundPack] {
        allPacks
    }
}
