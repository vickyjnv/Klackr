//
//  SoundPackHubView.swift
//  Klackr
//
//  Sound pack manager, importer, and community catalog view
//

import SwiftUI

struct SoundPackHubView: View {
    @EnvironmentObject var settings: SettingsManager
    @EnvironmentObject var soundEngine: SoundEngine
    @StateObject private var downloader = SoundPackDownloader.shared
    
    enum HubTab: String, CaseIterable {
        case keyboard = "Keyboard Switches"
        case mouse = "Mouse Clicks"
    }
    
    @State private var selectedTab: HubTab = .keyboard
    @State private var availableKeyboardPacks: [SoundPack] = SoundPack.visiblePacks
    @State private var availableMousePacks: [MouseSoundPack] = MouseSoundPack.visiblePacks
    @State private var importStatusMessage: String? = nil
    
    var body: some View {
        VStack(spacing: 0) {
            // Header
            HStack(spacing: 12) {
                Image(systemName: selectedTab == .keyboard ? "keyboard.badge.waveform" : "cursorarrow.click.2")
                    .font(.system(size: 28))
                    .foregroundStyle(.tint)
                
                VStack(alignment: .leading, spacing: 2) {
                    Text("Sound Pack Hub")
                        .font(.system(size: 18, weight: .bold))
                    Text(selectedTab == .keyboard ? "Select, import, or download keyboard acoustic profiles." : "Choose high-tactility mechanical & optical mouse microswitches.")
                        .font(.system(size: 12))
                        .foregroundColor(.secondary)
                }
                
                Spacer()
            }
            .padding()
            .background(Color(NSColor.controlBackgroundColor))
            
            // Tab Picker
            Picker("", selection: $selectedTab) {
                ForEach(HubTab.allCases, id: \.self) { tab in
                    Text(tab.rawValue).tag(tab)
                }
            }
            .pickerStyle(.segmented)
            .padding(.horizontal)
            .padding(.vertical, 8)
            
            Divider()
            
            // Content List
            ScrollView {
                if selectedTab == .keyboard {
                    keyboardTabContent
                } else {
                    mouseTabContent
                }
            }
        }
        .frame(width: 480, height: 530)
        .onAppear {
            refreshPacks()
        }
    }
    
    // MARK: - Keyboard Tab
    
    private var keyboardTabContent: some View {
        VStack(alignment: .leading, spacing: 16) {
            // Installed Section
            VStack(alignment: .leading, spacing: 8) {
                Text("INSTALLED KEYBOARD PACKS (\(availableKeyboardPacks.count))")
                    .font(.system(size: 11, weight: .semibold))
                    .foregroundColor(.secondary)
                    .padding(.horizontal, 4)
                
                ForEach(availableKeyboardPacks) { pack in
                    HStack(spacing: 12) {
                        Button {
                            settings.selectSoundPack(pack)
                            soundEngine.loadSoundPack(pack)
                            soundEngine.playPreview()
                        } label: {
                            HStack {
                                Image(systemName: settings.selectedSoundPack.id == pack.id ? "checkmark.circle.fill" : "circle")
                                    .foregroundColor(settings.selectedSoundPack.id == pack.id ? .green : .secondary)
                                    .font(.system(size: 15))
                                
                                VStack(alignment: .leading, spacing: 2) {
                                    HStack(spacing: 6) {
                                        Text(pack.name)
                                            .font(.system(size: 13, weight: .medium))
                                        
                                        if pack.isCustom {
                                            Text("CUSTOM")
                                                .font(.system(size: 9, weight: .bold))
                                                .padding(.horizontal, 4)
                                                .padding(.vertical, 1)
                                                .background(Color.blue.opacity(0.15))
                                                .foregroundColor(.blue)
                                                .cornerRadius(3)
                                        }
                                    }
                                    
                                    Text(pack.description)
                                        .font(.system(size: 11))
                                        .foregroundColor(.secondary)
                                }
                                
                                Spacer()
                            }
                            .contentShape(Rectangle())
                        }
                        .buttonStyle(.plain)
                        
                        Button {
                            soundEngine.loadSoundPack(pack)
                            soundEngine.playPreview()
                        } label: {
                            Image(systemName: "speaker.wave.2.fill")
                                .font(.system(size: 12))
                                .foregroundColor(.secondary)
                                .padding(6)
                                .background(Circle().fill(Color(NSColor.separatorColor).opacity(0.3)))
                        }
                        .buttonStyle(.plain)
                        .help("Preview sound")
                        
                        if pack.isCustom {
                            Button {
                                if downloader.deleteCustomPack(pack) {
                                    refreshPacks()
                                }
                            } label: {
                                Image(systemName: "trash")
                                    .font(.system(size: 12))
                                    .foregroundColor(.red.opacity(0.8))
                                    .padding(6)
                            }
                            .buttonStyle(.plain)
                            .help("Delete custom pack")
                        }
                    }
                    .padding(10)
                    .background(
                        RoundedRectangle(cornerRadius: 8)
                            .fill(settings.selectedSoundPack.id == pack.id ? Color.accentColor.opacity(0.08) : Color(NSColor.controlBackgroundColor))
                    )
                }
            }
            
            Divider().padding(.vertical, 4)
            
            // Import & Directory Actions
            VStack(alignment: .leading, spacing: 8) {
                Text("IMPORT & EXTERNAL PACKS")
                    .font(.system(size: 11, weight: .semibold))
                    .foregroundColor(.secondary)
                    .padding(.horizontal, 4)
                
                HStack(spacing: 10) {
                    Button {
                        selectFolderToImport()
                    } label: {
                        Label("Import Pack Folder...", systemImage: "folder.badge.plus")
                            .font(.system(size: 12, weight: .medium))
                            .frame(maxWidth: .infinity)
                            .padding(.vertical, 8)
                    }
                    .buttonStyle(.borderedProminent)
                    
                    Button {
                        downloader.openPacksFolder()
                    } label: {
                        Label("Open in Finder", systemImage: "arrow.up.forward.app")
                            .font(.system(size: 12, weight: .medium))
                            .frame(maxWidth: .infinity)
                            .padding(.vertical, 8)
                    }
                    .buttonStyle(.bordered)
                }
                
                if let msg = importStatusMessage {
                    Text(msg)
                        .font(.system(size: 11))
                        .foregroundColor(.blue)
                        .padding(.horizontal, 4)
                }
            }
            
            Divider().padding(.vertical, 4)
            
            // Online Community Repositories
            VStack(alignment: .leading, spacing: 8) {
                Text("COMMUNITY SOUND PACKS")
                    .font(.system(size: 11, weight: .semibold))
                    .foregroundColor(.secondary)
                    .padding(.horizontal, 4)
                
                Text("Download hundreds of community-recorded switch packs from Mechvibes, then import them with one click.")
                    .font(.system(size: 12))
                    .foregroundColor(.secondary)
                    .padding(.horizontal, 4)
                
                HStack(spacing: 10) {
                    Button {
                        if let url = URL(string: "https://mechvibes.com/soundpacks") {
                            NSWorkspace.shared.open(url)
                        }
                    } label: {
                        Label("Mechvibes Directory", systemImage: "safari")
                            .font(.system(size: 12))
                            .frame(maxWidth: .infinity)
                            .padding(.vertical, 6)
                    }
                    .buttonStyle(.bordered)
                    
                    Button {
                        if let url = URL(string: "https://github.com/hainguyents13/mechvibes-soundpacks") {
                            NSWorkspace.shared.open(url)
                        }
                    } label: {
                        Label("GitHub Repo", systemImage: "link")
                            .font(.system(size: 12))
                            .frame(maxWidth: .infinity)
                            .padding(.vertical, 6)
                    }
                    .buttonStyle(.bordered)
                }
            }
        }
        .padding()
    }
    
    // MARK: - Mouse Tab
    
    private var mouseTabContent: some View {
        VStack(alignment: .leading, spacing: 16) {
            VStack(alignment: .leading, spacing: 8) {
                Text("INSTALLED MOUSE PACKS (\(availableMousePacks.count))")
                    .font(.system(size: 11, weight: .semibold))
                    .foregroundColor(.secondary)
                    .padding(.horizontal, 4)
                
                ForEach(availableMousePacks) { pack in
                    HStack(spacing: 12) {
                        Button {
                            settings.selectMouseSoundPack(pack)
                            soundEngine.loadMouseSoundPack(pack)
                            soundEngine.playMousePreview()
                        } label: {
                            HStack {
                                Image(systemName: settings.selectedMouseSoundPack.id == pack.id ? "checkmark.circle.fill" : "circle")
                                    .foregroundColor(settings.selectedMouseSoundPack.id == pack.id ? .blue : .secondary)
                                    .font(.system(size: 15))
                                
                                VStack(alignment: .leading, spacing: 2) {
                                    Text(pack.name)
                                        .font(.system(size: 13, weight: .medium))
                                    
                                    Text(pack.description)
                                        .font(.system(size: 11))
                                        .foregroundColor(.secondary)
                                }
                                
                                Spacer()
                            }
                            .contentShape(Rectangle())
                        }
                        .buttonStyle(.plain)
                        
                        Button {
                            soundEngine.loadMouseSoundPack(pack)
                            soundEngine.playMousePreview()
                        } label: {
                            Image(systemName: "speaker.wave.2.fill")
                                .font(.system(size: 12))
                                .foregroundColor(.secondary)
                                .padding(6)
                                .background(Circle().fill(Color(NSColor.separatorColor).opacity(0.3)))
                        }
                        .buttonStyle(.plain)
                        .help("Preview mouse click")
                    }
                    .padding(10)
                    .background(
                        RoundedRectangle(cornerRadius: 8)
                            .fill(settings.selectedMouseSoundPack.id == pack.id ? Color.blue.opacity(0.08) : Color(NSColor.controlBackgroundColor))
                    )
                }
            }
            
            Divider().padding(.vertical, 4)
            
            VStack(alignment: .leading, spacing: 8) {
                Text("MOUSE PACKS DIRECTORY")
                    .font(.system(size: 11, weight: .semibold))
                    .foregroundColor(.secondary)
                    .padding(.horizontal, 4)
                
                Button {
                    let dir = MouseSoundPack.customPacksDirectory
                    NSWorkspace.shared.selectFile(nil, inFileViewerRootedAtPath: dir.path)
                } label: {
                    Label("Open Mouse Packs in Finder", systemImage: "folder")
                        .font(.system(size: 12, weight: .medium))
                        .frame(maxWidth: .infinity)
                        .padding(.vertical, 8)
                }
                .buttonStyle(.bordered)
            }
        }
        .padding()
    }
    
    private func refreshPacks() {
        availableKeyboardPacks = SoundPack.visiblePacks
        availableMousePacks = MouseSoundPack.visiblePacks
    }
    
    private func selectFolderToImport() {
        let panel = NSOpenPanel()
        panel.canChooseFiles = false
        panel.canChooseDirectories = true
        panel.allowsMultipleSelection = false
        panel.message = "Select a Sound Pack Folder containing .mp3 or .wav key sounds"
        panel.prompt = "Import Sound Pack"
        
        if panel.runModal() == .OK, let url = panel.url {
            if let newPack = downloader.importPackFolder(url: url) {
                importStatusMessage = "Successfully imported \(newPack.name)!"
                refreshPacks()
                settings.selectSoundPack(newPack)
                soundEngine.loadSoundPack(newPack)
                soundEngine.playPreview()
            } else {
                importStatusMessage = "Could not import the selected folder."
            }
        }
    }
}
