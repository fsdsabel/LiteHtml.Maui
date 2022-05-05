using LiteHtmlMaui.Controls;
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
    // TODO: move style from app.xaml to generic.xaml?
    // TODO: clipping
    public class WindowsLiteHtmlControl : Control
    {
        private WindowsLiteHtmlDocumentView _documentView;

        private static readonly CanvasDevice SharedDevice = CanvasDevice.GetSharedDevice();

        private string? _html;
        private string _userCss = "";
        private CanvasControl? _canvas;
        private double _lastRasterizationScale = 1.0;

        public WindowsLiteHtmlControl()
        {
            DefaultStyleKey = typeof(WindowsLiteHtmlControl);
            CreateDocumentView(SharedDevice);
            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            _lastRasterizationScale = XamlRoot?.RasterizationScale ?? 1;
            if (_html != null)
            {
                LoadHtml(_html, _userCss);
            }
        }

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
            var client = new HttpClient();
            return await client.GetStreamAsync(url);
        }

        public string? Html
        {
            get => _html;
            set
            {
                if (IsLoaded)
                {
                    if (value != null && _html != value)
                    {
                        _html = value;
                        _documentView?.LoadHtml(value);
                        _canvas?.Invalidate();
                        InvalidateMeasure();
                    }
                }
                else
                {
                    _html = value;
                    _userCss = "";
                }
            }
        }

        public void LoadHtml(string? html, string? userCss)
        {
            if (IsLoaded)
            {
                _html = html;
                _documentView.LoadHtml(html, userCss ?? "");
                _canvas?.Invalidate();
                InvalidateMeasure();
            }
            else
            {
                _html = html;
                _userCss = userCss ?? "";
            }
        }        


        public ICommand? Command { get; set; }

        private void OnDraw(CanvasControl sender, CanvasDrawEventArgs args)
        {
            args.DrawingSession.Clear((Background as Microsoft.UI.Xaml.Media.SolidColorBrush)?.Color ?? Colors.Transparent);
            _documentView?.DrawDocument(args.DrawingSession, (int)RenderSize.Width, (int)RenderSize.Height);
        }



        protected override Size MeasureOverride(Size availableSize)
        {
            _documentView.SetViewportSize(new Microsoft.Maui.Graphics.Size(availableSize.Width, availableSize.Height));
            var measuredSize = _documentView.MeasureDocument(new Microsoft.Maui.Graphics.Size(availableSize.Width, availableSize.Height));

            
            var result = new Size(measuredSize.Width, measuredSize.Height);            
            _canvas?.Measure(result);
            return result;
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            _documentView.SetViewportSize(new Microsoft.Maui.Graphics.Size(finalSize.Width, finalSize.Height));
            _canvas?.Arrange(new Rect(0, 0, finalSize.Width, finalSize.Height));
           
            return finalSize;
        }

        

        protected override void OnPointerMoved(PointerRoutedEventArgs e)
        {
            ReportEvent(LiteHtmlEvent.Move, e);
            base.OnPointerMoved(e);
        }


        protected override void OnPointerPressed(PointerRoutedEventArgs e)
        {
            ReportEvent(LiteHtmlEvent.Down, e);
            base.OnPointerPressed(e);
        }

        protected override void OnPointerReleased(PointerRoutedEventArgs e)
        {
            ReportEvent(LiteHtmlEvent.Up, e);
            base.OnPointerReleased(e);
        }

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


    public partial class LiteHtmlHandler : ViewHandler<ILiteHtml, WindowsLiteHtmlControl>
    {
        protected override WindowsLiteHtmlControl CreatePlatformView()
        {
            return new WindowsLiteHtmlControl();
        }

        public static void MapHtml(LiteHtmlHandler handler, ILiteHtml liteHtml)
        {
            handler.PlatformView.Html = liteHtml.Html;
        }

        public static void MapSource(LiteHtmlHandler handler, ILiteHtml liteHtml)
        {
            if (liteHtml.Source != null)
            {
                handler.PlatformView.LoadHtml(liteHtml.Source.Html, liteHtml.Source.Css);
            }
        }

        public static void MapCommand(LiteHtmlHandler handler, ILiteHtml liteHtml)
        {
            handler.PlatformView.Command = liteHtml.Command;
        }
    }
}
