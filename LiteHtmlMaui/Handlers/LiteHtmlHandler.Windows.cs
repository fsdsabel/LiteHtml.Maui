﻿using LiteHtmlMaui.Controls;
using LiteHtmlMaui.Handlers.Native;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.UI.Xaml;
using Microsoft.Maui.Handlers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using System.Diagnostics.CodeAnalysis;
using System.Windows.Input;
using Microsoft.UI.Input;
using Colors = Microsoft.UI.Colors;
using Rect = Windows.Foundation.Rect;
using Size = Windows.Foundation.Size;
using Microsoft.UI.Xaml;

namespace LiteHtmlMaui.Handlers
{
    // TODO: use real dpi (canvas is in dips: https://microsoft.github.io/Win2D/WinUI2/html/DPI.htm    
    // TODO: clipping
    /// <summary>
    /// Windows implementation
    /// </summary>
    public class WindowsLiteHtmlControl : Control
    {
        private WindowsLiteHtmlDocumentView _documentView;

        private static readonly CanvasDevice SharedDevice = CanvasDevice.GetSharedDevice();

        private string? _html;
        private Func<string, Task<Stream?>>? _externalResourceResolver;
        private string _userCss = "";
        private readonly Dictionary<string, string> _controlCssProperties = new();
        private readonly IFontManager _fontManager;
        private CanvasControl? _canvas;
        private double _lastRasterizationScale = 1.0;

        /// <summary>
        /// Constructor
        /// </summary>
        public WindowsLiteHtmlControl(IFontManager fontManager)
        {
            _fontManager = fontManager;
            DefaultStyleKey = typeof(WindowsLiteHtmlControl);
            CreateDocumentView(SharedDevice);
            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            _lastRasterizationScale = XamlRoot?.RasterizationScale ?? 1;
            if (_html != null)
            {
                LoadHtml(_html, _userCss, _externalResourceResolver);
            }
        }

        /// <inheritdoc />
        protected override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            _canvas = (CanvasControl) GetTemplateChild("Canvas");
            _canvas.Draw += OnDraw;            
            _canvas.CustomDevice = SharedDevice;
            XamlRoot.Changed += (s, e) =>
            {
                if (_lastRasterizationScale != (XamlRoot?.RasterizationScale ?? 1))
                {
                    _lastRasterizationScale = XamlRoot?.RasterizationScale ?? 1;
                    _documentView.ReloadDocument();
                }
            };            
        }

        [MemberNotNull(nameof(_documentView))]
        private void CreateDocumentView(ICanvasResourceCreator canvasResourceCreator)
        {
            
            _documentView = new WindowsLiteHtmlDocumentView(
                _fontManager,
                canvasResourceCreator,
                () =>
                {
                    return (float)(96.0 * XamlRoot?.RasterizationScale ?? 96.0);                    
                },
                ResolveResource,
                () =>
                {
                    DispatcherQueue.TryEnqueue(() =>
                    {
                        InvalidateMeasure();
                        InvalidateArrange();
                        _canvas?.Invalidate();
                    });
                },
                OnSetCursor);
            _documentView.AnchorClicked += OnAnchorClicked;
        }

        private void OnAnchorClicked(object? sender, string url)
        {
            if (Command?.CanExecute(url) ?? false)
            {
                Command?.Execute(url);
            }
        }

        private void OnSetCursor(string cursorName)
        {
            InputSystemCursor? mappedCursor = cursorName switch
            {
                "pointer" => InputSystemCursor.Create(InputSystemCursorShape.Hand),
                "text" => InputSystemCursor.Create(InputSystemCursorShape.IBeam),
                "all-scroll" => InputSystemCursor.Create(InputSystemCursorShape.SizeAll),
                "help" => InputSystemCursor.Create(InputSystemCursorShape.Help),
                "w-resize" => InputSystemCursor.Create(InputSystemCursorShape.SizeWestEast),
                "e-resize" => InputSystemCursor.Create(InputSystemCursorShape.SizeWestEast),
                "ew-resize" => InputSystemCursor.Create(InputSystemCursorShape.SizeWestEast),
                "n-resize" => InputSystemCursor.Create(InputSystemCursorShape.SizeNorthSouth),
                "s-resize" => InputSystemCursor.Create(InputSystemCursorShape.SizeNorthSouth),
                "ne-resize" => InputSystemCursor.Create(InputSystemCursorShape.SizeNortheastSouthwest),
                "nesw-resize" => InputSystemCursor.Create(InputSystemCursorShape.SizeNortheastSouthwest),
                "sw-resize" => InputSystemCursor.Create(InputSystemCursorShape.SizeNortheastSouthwest),
                "nwse-resize" => InputSystemCursor.Create(InputSystemCursorShape.SizeNorthwestSoutheast),
                "se-resize" => InputSystemCursor.Create(InputSystemCursorShape.SizeNorthwestSoutheast),
                "nw-resize" => InputSystemCursor.Create(InputSystemCursorShape.SizeNorthwestSoutheast),
                "not-allowed" => InputSystemCursor.Create(InputSystemCursorShape.UniversalNo),
                "no-drop" => InputSystemCursor.Create(InputSystemCursorShape.UniversalNo),
                _ => null
            };
            ProtectedCursor = mappedCursor;
        }

        private async Task<Stream> ResolveResource(string url)
        {
            if(_externalResourceResolver != null)
            {
                var result = await _externalResourceResolver(url);
                if (result != null) return result;
            }
            
            var client = new HttpClient();
            return await client.GetStreamAsync(url);
        }

        /// <summary>
        /// Loaded HTML
        /// </summary>
        public string? Html
        {
            get => _html;
            set
            {
                if(_html != value)
                {
                    LoadHtml(value, null, null);
                }
            }
        }

        /// <summary>
        /// Loads the given HTML and CSS
        /// </summary>
        public void LoadHtml(string? html, string? userCss, Func<string, Task<Stream?>>? resourceResolver)
        {
            _html = html;
            _externalResourceResolver = resourceResolver;
            if (IsLoaded)
            {
                var css = $"html{{ {string.Join("", _controlCssProperties.Select(kv => $"{kv.Key}:{kv.Value};"))} }}body{{margin:0;}}" + (userCss ?? "");
                _documentView.LoadHtml(html, css);
                _canvas?.Invalidate();
                InvalidateMeasure();
            }
            else
            {
                _userCss = userCss ?? "";                
            }
        }        

        /// <summary>
        /// Anchor command
        /// </summary>
        public ICommand? Command { get; set; }

        internal void SetCssControlProperty(string name, string? value)
        {
            if (value is null)
            {
                _controlCssProperties.Remove(name);
            }
            else
            {
                _controlCssProperties[name] = value;
            }
            LoadHtml(Html, _userCss, _externalResourceResolver);
        }

        private void OnDraw(CanvasControl sender, CanvasDrawEventArgs args)
        {
            sender.DpiScale = (float)XamlRoot.RasterizationScale;
            args.DrawingSession.TextAntialiasing = Microsoft.Graphics.Canvas.Text.CanvasTextAntialiasing.Auto;
            args.DrawingSession.Clear((Background as Microsoft.UI.Xaml.Media.SolidColorBrush)?.Color ?? Colors.Transparent);
            _documentView?.DrawDocument(args.DrawingSession, (int)RenderSize.Width, (int)RenderSize.Height);
        }


        /// <inheritdoc />
        protected override Size MeasureOverride(Size availableSize)
        {
            _documentView.SetViewportSize(new Microsoft.Maui.Graphics.Size(availableSize.Width, availableSize.Height));
            var measuredSize = _documentView.MeasureDocument(new Microsoft.Maui.Graphics.Size(availableSize.Width, availableSize.Height));

            
            var result = new Size(measuredSize.Width, measuredSize.Height);            
            _canvas?.Measure(new Size(result.Width, result.Height));
            return result;
        }

        /// <inheritdoc />
        protected override Size ArrangeOverride(Size finalSize)
        {
            _documentView.SetViewportSize(new Microsoft.Maui.Graphics.Size(finalSize.Width, finalSize.Height));
            _canvas?.Arrange(new Rect(0, 0, finalSize.Width, finalSize.Height));
           
            return finalSize;
        }


        /// <inheritdoc />
        protected override void OnPointerMoved(PointerRoutedEventArgs e)
        {
            ReportEvent(LiteHtmlEvent.Move, e);
            base.OnPointerMoved(e);
        }

        /// <inheritdoc />
        protected override void OnPointerPressed(PointerRoutedEventArgs e)
        {
            ReportEvent(LiteHtmlEvent.Down, e);
            base.OnPointerPressed(e);
        }

        /// <inheritdoc />
        protected override void OnPointerReleased(PointerRoutedEventArgs e)
        {
            ReportEvent(LiteHtmlEvent.Up, e);
            base.OnPointerReleased(e);
        }

        /// <inheritdoc />
        protected override void OnPointerExited(PointerRoutedEventArgs e)
        {
            ReportEvent(LiteHtmlEvent.Leave, e);
            base.OnPointerExited(e);
        }

        private void ReportEvent(LiteHtmlEvent e, PointerRoutedEventArgs pointerEvent)
        {
            var p = pointerEvent.GetCurrentPoint(this);
            if (_documentView.ReportEvent(e, (int)p.Position.X, (int)p.Position.Y, (int)p.Position.X, (int)p.Position.Y))
            {
                _canvas?.Invalidate();
            }
        }
    }

    /// <summary>
    /// LiteHtml MAUI Handler for Windows
    /// </summary>
    public partial class LiteHtmlHandler : ViewHandler<ILiteHtml, WindowsLiteHtmlControl>
    {
        /// <inheritdoc />
        protected override WindowsLiteHtmlControl CreatePlatformView()
        {
            var fontManager = MauiContext!.Services.GetRequiredService<IFontManager>();
            return new WindowsLiteHtmlControl(fontManager);
        }

        /// <summary>
        /// Maps HTML
        /// </summary>
        public static void MapHtml(LiteHtmlHandler handler, ILiteHtml liteHtml)
        {
            handler.PlatformView.Html = liteHtml.Html;
        }

        /// <summary>
        /// Maps a source
        /// </summary>
        public static void MapSource(LiteHtmlHandler handler, ILiteHtml liteHtml)
        {
            if (liteHtml.Source != null)
            {
                handler.PlatformView.LoadHtml(liteHtml.Source.Html, liteHtml.Source.Css, liteHtml.Source.GetStreamForUrlAsync);
            }
        }

        /// <summary>
        /// Maps the command
        /// </summary>
        public static void MapCommand(LiteHtmlHandler handler, ILiteHtml liteHtml)
        {
            handler.PlatformView.Command = liteHtml.Command;
        }

        /// <summary>
        /// Maps the text color
        /// </summary>
        public static void MapTextColor(LiteHtmlHandler handler, ILiteHtml liteHtml)
        {
            if (liteHtml.TextColor is null)
            {
                handler.PlatformView.SetCssControlProperty("color", null);
            }
            else
            {
                handler.PlatformView.SetCssControlProperty("color", $"rgba({(byte)(liteHtml.TextColor.Red * 255)},{(byte)(liteHtml.TextColor.Green * 255)},{(byte)(liteHtml.TextColor.Blue * 255)},{liteHtml.TextColor.Alpha})");
            }
        }

        /// <summary>
        /// Maps the font
        /// </summary>
        public static void MapFont(LiteHtmlHandler handler, ILiteHtml liteHtml)
        {  
            
            handler.PlatformView.SetCssControlProperty("font-size", $"{liteHtml.Font.Size}pt");
            if (liteHtml.Font.Family != null)
            {
                handler.PlatformView.SetCssControlProperty("font-family", liteHtml.Font.Family);
            }
            else
            {
                handler.PlatformView.SetCssControlProperty("font-family", null);
            }
        }

        /// <summary>
        /// Maps the character spacing
        /// </summary>
        public static void MapCharacterSpacing(LiteHtmlHandler handler, ILiteHtml liteHtml)
        {
            handler.PlatformView.SetCssControlProperty("letter-spacing", $"{liteHtml.CharacterSpacing}pt");            
        }
    }
}
