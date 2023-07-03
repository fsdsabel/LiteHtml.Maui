using CoreGraphics;
using LiteHtmlMaui.Controls;
using LiteHtmlMaui.Handlers.Native;
using Microsoft.Maui.Handlers;
using System.Windows.Input;
using UIKit;

namespace LiteHtmlMaui.Handlers
{
    /// <summary>
    /// iOS implementation
    /// </summary>
    public class IOSLiteHtmlView : UIView
    {
        private string? _html, _userCss;
        private IOSLiteHtmlDocumentView _documentView;
        private Func<string, Task<Stream?>>? _externalResourceResolver;
        private readonly Dictionary<string, string> _controlCssProperties = new();

        /// <summary>
        /// Constructor
        /// </summary>
        public IOSLiteHtmlView()
        {
            _documentView = new IOSLiteHtmlDocumentView(ResolveResource, OnRedraw);
            _documentView.AnchorClicked += OnAnchorClicked;
            Opaque = false;
        }

        private void OnRedraw()
        {
            InvokeOnMainThread(() =>
            {
                // TODO: scrollview is not updated - should we do that here??
                // https://stackoverflow.com/questions/2944294/how-do-i-auto-size-a-uiscrollview-to-fit-its-content

                SizeToFit();
                SetNeedsLayout();
                //LayoutIfNeeded();
                Superview?.SetNeedsLayout();
                //Superview?.LayoutIfNeeded();
                //Superview.SetNeedsDisplay();
                SetNeedsDisplay();
                RecalculateScrollViewers(this);
            });

            static void RecalculateScrollViewers(UIView child)
            {
                if (child.Superview == null) return;
                if (child.Superview is UIScrollView scrollView)
                {
                    RecalculateContentSize(scrollView);
                }
                RecalculateScrollViewers(child.Superview);
            }

            static void RecalculateContentSize(UIScrollView scrollView)
            {
                scrollView.ShowsVerticalScrollIndicator = false;
                scrollView.ShowsHorizontalScrollIndicator = false;
                var totalRect = RecursiveUnionInDepthFor(scrollView);
                scrollView.ContentSize = new CGSize(totalRect.Width, totalRect.Height);

                static CGRect RecursiveUnionInDepthFor(UIView view)
                {
                    var totalRect = CGRect.Empty;
                    foreach (var subView in view.Subviews.Where(v => !v.Hidden))
                    {
                        totalRect = totalRect.UnionWith(subView.Frame);
                    }
                    return totalRect;
                }

                scrollView.ShowsVerticalScrollIndicator = true;
                scrollView.ShowsHorizontalScrollIndicator = true;
            }
        }

        private void OnAnchorClicked(object? sender, string url)
        {
            if (Command?.CanExecute(url) ?? false)
            {
                Command?.Execute(url);
            }
        }

        /// <summary>
        /// Loaded HTML
        /// </summary>
        public string? Html
        {
            get => _html;
            set
            {
                if (_html != value)
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
            _userCss = userCss;

            var css = $"html{{ {string.Join("", _controlCssProperties.Select(kv => $"{kv.Key}:{kv.Value};"))} }}body{{margin:0;}}" + (userCss ?? "");
            _documentView.LoadHtml(html, css);
            OnRedraw();
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

        private async Task<Stream> ResolveResource(string url)
        {
            if (_externalResourceResolver != null)
            {
                var result = await _externalResourceResolver(url);
                if (result != null) return result;
            }

            var client = new HttpClient(new NSUrlSessionHandler());
            return client.GetStreamAsync(url).GetAwaiter().GetResult(); // await blocks
        }

        /// <inheritdoc />
        public override CGSize SizeThatFits(CGSize size)
        {
            _documentView.SetViewportSize(new Microsoft.Maui.Graphics.Size(size.Width, size.Height));
            var measuredSize = _documentView.MeasureDocument(new Microsoft.Maui.Graphics.Size(size.Width, size.Height));
            return new CGSize((int)measuredSize.Width, (int)measuredSize.Height);
        }

        /// <inheritdoc />
        public override void LayoutSubviews()
        {
            base.LayoutSubviews();
            _documentView.SetViewportSize(new Microsoft.Maui.Graphics.Size(Frame.Width, Frame.Height));
            SetNeedsDisplay();
        }

        /// <inheritdoc />
        public override void Draw(CGRect rect)
        {
            using (CGContext g = UIGraphics.GetCurrentContext())
            {
                _documentView.DrawDocument(g, (int)_documentView.ViewportSize.Width, (int)_documentView.ViewportSize.Height);
            }
        }
    }

    /// <summary>
    /// LiteHtml MAUI Handler for iOS
    /// </summary>
    public partial class LiteHtmlHandler : ViewHandler<ILiteHtml, IOSLiteHtmlView>
    {
        /// <inheritdoc />
        protected override IOSLiteHtmlView CreatePlatformView()
        {
            return new IOSLiteHtmlView();
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
