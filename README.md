# LiteHtml.Maui

LiteHtml.Maui is a cross platform library for rendering HTML content with Microsoft MAUI.
It uses [litehtml](https://github.com/litehtml/litehtml) for parsing and layouting HTML. This library supports

- Windows (.NET 8)
- Android (.NET 8)
- iOS (.NET 8)

## When to use

Use this library if a MAUI label is not enough but a WebView is too much overhead. This library is intended to be used for light workloads with HTML that you control. It is not meant to be a full web browser. Also this library does **not implement text selection capabilities**. This is mainly a limitation of litehtml.

## How to use

A basic example how to use the library can be found in the TestApp. Basically it's the following steps.


**In MauiProgram.cs**
```csharp
var builder = MauiApp.CreateBuilder();
    builder
        .UseMauiApp<App>()
        .UseLiteHtml() // Register the handlers, optionally you can register a custom master stylesheet.
        ...
```

**In your views**
```xml
<ContentPage ...
        xmlns:html="clr-namespace:LiteHtmlMaui.Controls;assembly=LiteHtmlMaui"/>
    <html:LiteHtml Html="Some HTML" />
<ContentPage>
```

If you want to use a custom image loader and control specific CSS, you can use an implementation of `ILiteHtmlSource` and assign an instance of this to `LiteHtml.Source`.

