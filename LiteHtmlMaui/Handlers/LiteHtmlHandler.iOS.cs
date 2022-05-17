using CoreGraphics;
using Foundation;
using LiteHtmlMaui.Controls;
using LiteHtmlMaui.Handlers.Native;
using Microsoft.Maui.Handlers;
using System;
using System.IO;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Input;
using UIKit;

namespace LiteHtmlMaui.Handlers
{
    public class IOSLiteHtmlView : UIView
    {
        private string? _html;
        private IOSLiteHtmlDocumentView _documentView;
        private Func<string, Task<Stream?>>? _externalResourceResolver;
        private Size _intrinsicContentSize = Size.Zero;

        public IOSLiteHtmlView()
        {
            _documentView = new IOSLiteHtmlDocumentView(ResolveResource, () =>
            {
                InvokeOnMainThread(() =>
                {
                    // TODO: scrollview is not updated - should we do that here??
                    // https://stackoverflow.com/questions/2944294/how-do-i-auto-size-a-uiscrollview-to-fit-its-content

                    SizeToFit();
                    SetNeedsLayout();
                    
                    //Superview.SetNeedsLayout();
                    //Superview.LayoutIfNeeded();
                    //Superview.SetNeedsDisplay();
                    SetNeedsDisplay();
                });
            });
            _documentView.AnchorClicked += OnAnchorClicked;
            Opaque = false;
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
                /*if (_html != value)
                {
                    _html = value;
                    _externalResourceResolver = null;
                    _documentView.LoadHtml(value);
                    SetNeedsLayout();
                }*/
            }
        }

        public void LoadHtml(string? html, string? userCss, Func<string, Task<Stream?>>? resourceResolver)
        {
            _html = html;
            _externalResourceResolver = resourceResolver;
            _documentView.LoadHtml(html, userCss ?? "");
            SetNeedsLayout();
        }

        public ICommand? Command { get; set; }


        public override CGSize IntrinsicContentSize => _intrinsicContentSize;

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
