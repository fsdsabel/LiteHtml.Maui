# LiteHtml.Maui

LiteHtml.Maui is a cross platform library for rendering HTML content with Microsoft MAUI.
It uses [litehtml](https://github.com/litehtml/litehtml) for parsing and layouting HTML. This library supports

- Windows (.NET 6 and .NET 7)
- Android (.NET 6 and .NET 7)
- iOS (.NET 6 and .NET 7)

## When to use

Use this library if a MAUI label is not enough but a WebView is too much overhead. This library is intended to be used for light workloads with HTML that you control. It is not meant to be a full web browser. Also this library does **not implement text selection capabilities**. This is mainly a limitation of litehtml.

