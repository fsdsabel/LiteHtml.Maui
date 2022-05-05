using CoreGraphics;
using Foundation;
using LiteHtmlMaui.Controls;
using LiteHtmlMaui.Handlers.Native;
using Microsoft.Maui.Handlers;
using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows.Input;
using UIKit;

namespace LiteHtmlMaui.Handlers
{
    public class IOSLiteHtmlView : UIView
    {
        private string? _html;
        private IOSLiteHtmlDocumentView _documentView;

        public IOSLiteHtmlView()
        {
            _documentView = new IOSLiteHtmlDocumentView(ResolveResource);
            Opaque = false;
        }

        public string? Html
        {
            get => _html;
            set
            {
                if (_html != value)
                {
                    _html = value;
                    _documentView.LoadHtml(value);
                    SetNeedsLayout();
                }
            }
        }

        public void LoadHtml(string? html, string? userCss)
        {
            _html = html;
            _documentView.LoadHtml(html, userCss ?? "");
            SetNeedsLayout();
        }

        public ICommand? Command { get; set; }


        private Task<Stream> ResolveResource(string url)
        {
            var client = new HttpClient(new NSUrlSessionHandler());
            return Task.FromResult(client.GetStreamAsync(url).GetAwaiter().GetResult()); // await blocks

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
        }

        public override void Draw(CGRect rect)
        {
            using (CGContext g = UIGraphics.GetCurrentContext())
            {
                _documentView.DrawDocument(g, (int)_documentView.ViewportSize.Width, (int)_documentView.ViewportSize.Height);
            }
        }
        // SetNeedsDisplay
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
                handler.PlatformView.LoadHtml(liteHtml.Source.Html, liteHtml.Source.Css);
            }
        }

        public static void MapCommand(LiteHtmlHandler handler, ILiteHtml liteHtml)
        {
            handler.PlatformView.Command = liteHtml.Command;
        }
    }
}
