using CoreGraphics;
using LiteHtmlMaui.Controls;
using LiteHtmlMaui.Handlers.Native;
using Microsoft.Maui.Handlers;
using System.Windows.Input;
using UIKit;

namespace LiteHtmlMaui.Handlers
{
    public class IOSLiteHtmlView : UIView
    {
        private string? _html;
        private IOSLiteHtmlDocumentView _documentView;
        private Func<string, Task<Stream?>>? _externalResourceResolver;

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

        public void LoadHtml(string? html, string? userCss, Func<string, Task<Stream?>>? resourceResolver)
        {
            _html = html;
            _externalResourceResolver = resourceResolver;
            _documentView.LoadHtml(html, userCss ?? "");
            OnRedraw();
        }

        public ICommand? Command { get; set; }

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

        public override CGSize SizeThatFits(CGSize size)
        {
            _documentView.SetViewportSize(new Microsoft.Maui.Graphics.Size(size.Width, size.Height));
            var measuredSize = _documentView.MeasureDocument(new Microsoft.Maui.Graphics.Size(size.Width, size.Height));
            return new CGSize((int)measuredSize.Width, (int)measuredSize.Height);
        }

        public override void LayoutSubviews()
        {
            base.LayoutSubviews();
            _documentView.SetViewportSize(new Microsoft.Maui.Graphics.Size(Frame.Width, Frame.Height));
            SetNeedsDisplay();
        }

        public override void Draw(CGRect rect)
        {
            using (CGContext g = UIGraphics.GetCurrentContext())
            {
                _documentView.DrawDocument(g, (int)_documentView.ViewportSize.Width, (int)_documentView.ViewportSize.Height);
            }
        }
    }


    public partial class LiteHtmlHandler : ViewHandler<ILiteHtml, IOSLiteHtmlView>
    {
        protected override IOSLiteHtmlView CreatePlatformView()
        {
            return new IOSLiteHtmlView();
        }

        public static void MapHtml(LiteHtmlHandler handler, ILiteHtml liteHtml)
        {
            handler.PlatformView.Html = liteHtml.Html;
        }

        public static void MapSource(LiteHtmlHandler handler, ILiteHtml liteHtml)
        {
            if (liteHtml.Source != null)
            {
                handler.PlatformView.LoadHtml(liteHtml.Source.Html, liteHtml.Source.Css, liteHtml.Source.GetStreamForUrlAsync);
            }
        }

        public static void MapCommand(LiteHtmlHandler handler, ILiteHtml liteHtml)
        {
            handler.PlatformView.Command = liteHtml.Command;
        }
    }
}
