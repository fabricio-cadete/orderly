import SwiftUI

@main
struct OrderlyWatchApp: App {
    var body: some Scene {
        WindowGroup {
            VStack(spacing: 4) {
                Text("Orderly.")
                    .font(.headline)
                Text("Hoje")
                    .foregroundStyle(OrderlyTheme.lavender)
            }
        }
    }
}
