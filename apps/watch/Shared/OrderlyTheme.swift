import SwiftUI

/// Espelho dos tokens de `packages/ui/src/tokens.ts`.
enum OrderlyTheme {
    static let violet = Color(hex: 0x8B5CF6)
    static let lavender = Color(hex: 0xC4B5FD)

    enum Dark {
        static let background = Color(hex: 0x111114)
        static let surface = Color(hex: 0x24242B)
        static let text = Color(hex: 0xF4F4F5)
        static let priorities = [Color(hex: 0xF87171), Color(hex: 0xFB923C), Color(hex: 0x60A5FA), Color(hex: 0x71717A)]
    }

    enum Light {
        static let background = Color(hex: 0xF7F7F9)
        static let surface = Color(hex: 0xFFFFFF)
        static let text = Color(hex: 0x111114)
        static let priorities = [Color(hex: 0xDC2626), Color(hex: 0xEA580C), Color(hex: 0x2563EB), Color(hex: 0xA1A1AA)]
    }
}

extension Color {
    init(hex: UInt32) {
        self.init(
            red: Double((hex >> 16) & 0xFF) / 255,
            green: Double((hex >> 8) & 0xFF) / 255,
            blue: Double(hex & 0xFF) / 255
        )
    }
}
