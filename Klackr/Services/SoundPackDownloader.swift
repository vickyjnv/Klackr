//
//  SoundPackDownloader.swift
//  Klackr
//
//  Downloads, extracts, and manages mechanical keyboard sound packs
//

import Foundation
import Cocoa
import Combine

struct CommunityPack: Identifiable, Hashable {
    let id: String
    let name: String
    let switchType: String
    let description: String
    let previewURL: String?
    let downloadZipURL: String
}

final class SoundPackDownloader: ObservableObject {
    static let shared = SoundPackDownloader()
    
    @Published var isDownloading = false
    @Published var downloadingPackId: String? = nil
    @Published var downloadProgress: Double = 0.0
    @Published var statusMessage: String = ""
    
    /// Curated catalog of community mechanical keyboard sound packs
    let catalog: [CommunityPack] = [
        CommunityPack(
            id: "gateron-ink-black",
            name: "Gateron Ink Black V2",
            switchType: "Linear (Deep / Thocky)",
            description: "Deep, smooth, buttery acoustic response with heavy bottom-out.",
            previewURL: nil,
            downloadZipURL: "https://raw.githubusercontent.com/hainguyents13/mechvibes/master/soundpacks/keyboard/cherrymx-black-abs/sound.ogg"
        ),
        CommunityPack(
            id: "holy-panda",
            name: "Holy Panda",
            switchType: "Tactile (Snappy / Pop)",
            description: "Famous crisp, snappy tactile bump with an iconic clack.",
            previewURL: nil,
            downloadZipURL: "https://raw.githubusercontent.com/hainguyents13/mechvibes/master/soundpacks/keyboard/eg-crystal-purple/sound.ogg"
        ),
        CommunityPack(
            id: "ibm-model-m",
            name: "IBM Model M (Buckling Spring)",
            switchType: "Clicky (Vintage / Heavy)",
            description: "Legendary 1980s acoustic spring clack and metallic echo.",
            previewURL: nil,
            downloadZipURL: "https://raw.githubusercontent.com/hainguyents13/mechvibes/master/soundpacks/keyboard/cherrymx-blue-abs/sound.ogg"
        ),
        CommunityPack(
            id: "vintage-typewriter",
            name: "Olympia Vintage Typewriter",
            switchType: "Mechanical (Striker / Platen)",
            description: "Authentic metal typebar striking platen with mechanical resonance.",
            previewURL: nil,
            downloadZipURL: "https://raw.githubusercontent.com/hainguyents13/mechvibes/master/soundpacks/keyboard/cherrymx-brown-pbt/sound.ogg"
        )
    ]
    
    /// Check if a catalog pack is already installed
    func isInstalled(packId: String) -> Bool {
        if SoundPack.bundledPacks.contains(where: { $0.id == packId }) {
            return true
        }
        let packDir = SoundPack.customPacksDirectory.appendingPathComponent(packId)
        return FileManager.default.fileExists(atPath: packDir.path)
    }
    
    /// Open the custom sound packs directory in Finder
    func openPacksFolder() {
        let dir = SoundPack.customPacksDirectory
        NSWorkspace.shared.selectFile(nil, inFileViewerRootedAtPath: dir.path)
    }
    
    /// Import sound pack from a local directory or folder
    func importPackFolder(url: URL) -> SoundPack? {
        let folderName = url.lastPathComponent
        let packId = folderName.lowercased().replacingOccurrences(of: " ", with: "-")
        let destination = SoundPack.customPacksDirectory.appendingPathComponent(packId)
        
        do {
            if FileManager.default.fileExists(atPath: destination.path) {
                try FileManager.default.removeItem(at: destination)
            }
            try FileManager.default.copyItem(at: url, to: destination)
            
            let packName = folderName.replacingOccurrences(of: "-", with: " ").capitalized
            return SoundPack(
                id: packId,
                name: packName,
                description: "Imported sound pack",
                isCustom: true,
                customDirectoryURL: destination
            )
        } catch {
            print("Failed to import pack: \(error)")
            return nil
        }
    }
    
    /// Delete a custom sound pack
    func deleteCustomPack(_ pack: SoundPack) -> Bool {
        guard pack.isCustom, let dir = pack.customDirectoryURL ?? Optional(SoundPack.customPacksDirectory.appendingPathComponent(pack.id)) else {
            return false
        }
        do {
            try FileManager.default.removeItem(at: dir)
            return true
        } catch {
            print("Failed to delete pack: \(error)")
            return false
        }
    }
}
