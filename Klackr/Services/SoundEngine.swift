//
//  SoundEngine.swift
//  Klackr
//
//  Audio engine for playing keyboard sounds with low latency
//

import Foundation
import AVFoundation
import Combine

/// High-performance audio engine for keyboard sounds
class SoundEngine: ObservableObject {
    
    // MARK: - Properties
    
    private var audioEngine: AVAudioEngine
    private var playerNodes: [AVAudioPlayerNode] = []
    private var mixerNode: AVAudioMixerNode
    
    /// Pre-loaded audio buffers for instant playback
    private var soundBuffers: [String: [AVAudioPCMBuffer]] = [:]
    
    /// The audio format currently used by all player nodes.
    private var playerOutputFormat: AVAudioFormat?
    
    /// Current sound pack
    private var currentPack: SoundPack?
    
    // MARK: - Mouse Audio Properties
    
    private var mousePlayerNodes: [AVAudioPlayerNode] = []
    private var mouseSoundBuffers: [String: [AVAudioPCMBuffer]] = [:]
    private var mousePlayerOutputFormat: AVAudioFormat?
    private var currentMousePack: MouseSoundPack?
    private let mousePoolSize = 4
    private var currentMousePlayerIndex = 0
    
    /// Mouse Volume (0.0 to 1.0)
    @Published var mouseVolume: Float = 0.8
    
    /// Volume (0.0 to 1.0)
    @Published var volume: Float = 0.8 {
        didSet {
            mixerNode.outputVolume = volume
        }
    }
    
    /// Number of player nodes for polyphony
    private let playerPoolSize = 8
    
    /// Current player index for round-robin scheduling
    private var currentPlayerIndex = 0
    
    // MARK: - Initialization
    
    init() {
        audioEngine = AVAudioEngine()
        mixerNode = audioEngine.mainMixerNode
        
        setupAudioEngine()
        // Sound pack will be loaded when MenuBarView appears
    }
    
    // MARK: - Setup
    
    private func setupAudioEngine() {
        // Create a pool of player nodes for keyboard polyphony
        for _ in 0..<playerPoolSize {
            let playerNode = AVAudioPlayerNode()
            audioEngine.attach(playerNode)
            playerNodes.append(playerNode)
        }
        
        // Create a dedicated pool of player nodes for mouse polyphony
        for _ in 0..<mousePoolSize {
            let playerNode = AVAudioPlayerNode()
            audioEngine.attach(playerNode)
            mousePlayerNodes.append(playerNode)
        }
    }
    
    // MARK: - Sound Pack Loading
    
    /// Load and buffer all sounds from a sound pack
    func loadSoundPack(_ pack: SoundPack) {
        guard pack.id != currentPack?.id else { return }
        
        currentPack = pack
        soundBuffers.removeAll()
        var playbackFormat: AVAudioFormat?
        
        // Pre-load all sounds for each key type and event
        for keyType in KeyType.allCases {
            for event in [KeyEvent.press, KeyEvent.release] {
                let key = bufferKey(for: keyType, event: event)
                let urls = pack.soundURLs(for: keyType, event: event)
                
                var buffers: [AVAudioPCMBuffer] = []
                for url in urls {
                    if let buffer = loadAudioBuffer(from: url) {
                        if playbackFormat == nil {
                            playbackFormat = buffer.format
                        }
                        
                        if let playbackFormat, formatsMatch(buffer.format, playbackFormat) {
                            buffers.append(buffer)
                        } else if let playbackFormat {
                            print("🔊 ⚠️ Skipping \(url.lastPathComponent) because its format \(buffer.format) does not match playback format \(playbackFormat)")
                        }
                    }
                }
                
                if !buffers.isEmpty {
                    soundBuffers[key] = buffers
                }
            }
        }
        
        if let playbackFormat {
            configurePlayerNodes(for: playbackFormat)
            startAudioEngineIfNeeded()
        }
        
        print("Loaded sound pack: \(pack.name) with \(soundBuffers.count) sound groups")
    }
    
    // MARK: - Mouse Sound Pack Loading
    
    /// Load and buffer all sounds from a mouse sound pack
    func loadMouseSoundPack(_ pack: MouseSoundPack) {
        guard pack.id != currentMousePack?.id else { return }
        
        currentMousePack = pack
        mouseSoundBuffers.removeAll()
        var playbackFormat: AVAudioFormat?
        
        for button in MouseButton.allCases {
            for event in [MouseEvent.press, MouseEvent.release] {
                let key = mouseBufferKey(for: button, event: event)
                let urls = pack.soundURLs(for: button, event: event)
                
                var buffers: [AVAudioPCMBuffer] = []
                for url in urls {
                    if let buffer = loadAudioBuffer(from: url) {
                        if playbackFormat == nil {
                            playbackFormat = buffer.format
                        }
                        
                        if let playbackFormat, formatsMatch(buffer.format, playbackFormat) {
                            buffers.append(buffer)
                        }
                    }
                }
                
                if !buffers.isEmpty {
                    mouseSoundBuffers[key] = buffers
                }
            }
        }
        
        if let playbackFormat {
            configureMousePlayerNodes(for: playbackFormat)
            startAudioEngineIfNeeded()
        }
        
        print("Loaded mouse sound pack: \(pack.name) with \(mouseSoundBuffers.count) groups")
    }
    
    private func loadAudioBuffer(from url: URL) -> AVAudioPCMBuffer? {
        do {
            let audioFile = try AVAudioFile(forReading: url)
            let format = audioFile.processingFormat
            let frameCount = UInt32(audioFile.length)
            
            print("🔊 Loading: \(url.lastPathComponent) - \(format.sampleRate)Hz, \(format.channelCount)ch, \(frameCount) frames, duration: \(Double(frameCount)/format.sampleRate)s")
            
            guard let buffer = AVAudioPCMBuffer(pcmFormat: format, frameCapacity: frameCount) else {
                print("🔊 ❌ Failed to create buffer for \(url.lastPathComponent)")
                return nil
            }
            
            try audioFile.read(into: buffer)
            print("🔊 ✅ Loaded: \(url.lastPathComponent)")
            return buffer
        } catch {
            print("🔊 ❌ Failed to load audio file \(url.lastPathComponent): \(error)")
            return nil
        }
    }
    
    private func bufferKey(for keyType: KeyType, event: KeyEvent) -> String {
        "\(keyType.rawValue)_\(event == .press ? "press" : "release")"
    }
    
    private func mouseBufferKey(for button: MouseButton, event: MouseEvent) -> String {
        "mouse_\(button.rawValue)_\(event == .press ? "press" : "release")"
    }
    
    private func configureMousePlayerNodes(for format: AVAudioFormat) {
        guard !formatsMatch(mousePlayerOutputFormat, format) else { return }
        
        audioEngine.stop()
        
        for player in mousePlayerNodes {
            player.stop()
            audioEngine.disconnectNodeOutput(player)
            audioEngine.connect(player, to: mixerNode, format: format)
        }
        
        mousePlayerOutputFormat = format
    }
    
    private func configurePlayerNodes(for format: AVAudioFormat) {
        guard !formatsMatch(playerOutputFormat, format) else { return }
        
        audioEngine.stop()
        
        for player in playerNodes {
            player.stop()
            audioEngine.disconnectNodeOutput(player)
            audioEngine.connect(player, to: mixerNode, format: format)
        }
        
        playerOutputFormat = format
    }
    
    private func formatsMatch(_ lhs: AVAudioFormat?, _ rhs: AVAudioFormat) -> Bool {
        guard let lhs else { return false }
        
        return lhs.channelCount == rhs.channelCount
            && lhs.sampleRate == rhs.sampleRate
            && lhs.commonFormat == rhs.commonFormat
            && lhs.isInterleaved == rhs.isInterleaved
    }
    
    private func startAudioEngineIfNeeded() {
        guard !audioEngine.isRunning else { return }
        
        do {
            try audioEngine.start()
        } catch {
            print("Failed to start audio engine: \(error)")
        }
    }
    
    // MARK: - Playback
    
    /// Play a sound for the given key type and event
    func playSound(for keyType: KeyType, event: KeyEvent) {
        let key = bufferKey(for: keyType, event: event)
        
        guard let buffers = soundBuffers[key], !buffers.isEmpty else {
            // Fall back to regular key sound if specific sound not found
            if keyType != .regular {
                playSound(for: .regular, event: event)
            }
            return
        }
        
        startAudioEngineIfNeeded()
        
        // Pick a random variation
        let buffer = buffers.randomElement()!
        
        // Get the next player from the pool (round-robin)
        let player = playerNodes[currentPlayerIndex]
        currentPlayerIndex = (currentPlayerIndex + 1) % playerPoolSize
        
        // Stop any currently playing sound on this player
        player.stop()
        
        // Apply random pitch and volume variation for natural feel
        let pitchVariation = Float.random(in: 0.95...1.05)
        let volumeVariation = Float.random(in: 0.9...1.0)
        
        player.volume = volume * volumeVariation
        player.rate = pitchVariation
        
        // Schedule and play
        player.scheduleBuffer(buffer, at: nil, options: .interrupts, completionHandler: nil)
        
        if !player.isPlaying {
            player.play()
        }
    }
    
    /// Play a preview sound for the current pack
    func playPreview() {
        playSound(for: .regular, event: .press)
        
        // Slight delay then play release sound
        DispatchQueue.main.asyncAfter(deadline: .now() + 0.1) { [weak self] in
            self?.playSound(for: .regular, event: .release)
        }
    }
    
    /// Play a sound for a mouse button click or release
    func playMouseSound(for button: MouseButton, event: MouseEvent) {
        let key = mouseBufferKey(for: button, event: event)
        
        guard let buffers = mouseSoundBuffers[key], !buffers.isEmpty else {
            // Fall back to left button sound
            if button != .left {
                playMouseSound(for: .left, event: event)
            }
            return
        }
        
        startAudioEngineIfNeeded()
        
        let buffer = buffers.randomElement()!
        
        guard !mousePlayerNodes.isEmpty else { return }
        let player = mousePlayerNodes[currentMousePlayerIndex]
        currentMousePlayerIndex = (currentMousePlayerIndex + 1) % mousePoolSize
        
        player.stop()
        
        let pitchVariation = Float.random(in: 0.97...1.03)
        let volumeVariation = Float.random(in: 0.92...1.0)
        
        player.volume = mouseVolume * volumeVariation
        player.rate = pitchVariation
        
        player.scheduleBuffer(buffer, at: nil, options: .interrupts, completionHandler: nil)
        
        if !player.isPlaying {
            player.play()
        }
    }
    
    /// Play a preview sound for the current mouse pack
    func playMousePreview() {
        playMouseSound(for: .left, event: .press)
        DispatchQueue.main.asyncAfter(deadline: .now() + 0.08) { [weak self] in
            self?.playMouseSound(for: .left, event: .release)
        }
    }
    
    // MARK: - Engine Control
    
    func pause() {
        audioEngine.pause()
    }
    
    func resume() {
        do {
            try audioEngine.start()
        } catch {
            print("Failed to resume audio engine: \(error)")
        }
    }
}
