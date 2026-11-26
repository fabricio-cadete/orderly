import SwiftUI
import WidgetKit

struct TodayEntry: TimelineEntry {
    let date: Date
}

struct TodayProvider: TimelineProvider {
    func placeholder(in context: Context) -> TodayEntry { TodayEntry(date: .now) }

    func getSnapshot(in context: Context, completion: @escaping (TodayEntry) -> Void) {
        completion(TodayEntry(date: .now))
    }

    func getTimeline(in context: Context, completion: @escaping (Timeline<TodayEntry>) -> Void) {
        completion(Timeline(entries: [TodayEntry(date: .now)], policy: .atEnd))
    }
}

struct TodayWidget: Widget {
    var body: some WidgetConfiguration {
        StaticConfiguration(kind: "app.orderly.today", provider: TodayProvider()) { _ in
            Text("Hoje")
                .containerBackground(OrderlyTheme.Dark.background, for: .widget)
        }
        .configurationDisplayName("Hoje")
        .description("Suas tarefas de hoje.")
    }
}

@main
struct OrderlyWidgetsBundle: WidgetBundle {
    var body: some Widget {
        TodayWidget()
    }
}
