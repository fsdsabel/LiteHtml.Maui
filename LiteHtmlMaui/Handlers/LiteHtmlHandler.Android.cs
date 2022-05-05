using Android.Content;
using Android.Graphics;
using Android.Views;
using LiteHtmlMaui.Controls;
using LiteHtmlMaui.Handlers.Native;
using Microsoft.Maui.Handlers;
using System.Windows.Input;
using View = Android.Views.View;

namespace LiteHtmlMaui.Handlers
{

    public class AndroidLiteHtmlView : View
    {
        private AndroidLiteHtmlDocumentView _documentView;      
        private string? _html;

        public AndroidLiteHtmlView(Context context) : base(context)
        {
            _documentView = new AndroidLiteHtmlDocumentView(context, ResolveResource);
            _documentView.AnchorClicked += OnAnchorClicked;
        }

        private void OnAnchorClicked(object? sender, string url)
        {
            if (Command?.CanExecute(url) ?? false)
            {
                Command?.Execute(url);
            }
        }

        private Task<Stream> ResolveResource(string url)
        {
            var client = new HttpClient();
            return Task.FromResult(client.GetStreamAsync(url).GetAwaiter().GetResult());  // async crashes :(
        }

        public string? Html
        {
            get => _html;
            set
            {
                if(_html != value)
                {
                    _html = value;
                    _documentView.LoadHtml(value);
                }
            }
        }

        public void LoadHtml(string? html, string? userCss)
        {
            _html = html;
            _documentView.LoadHtml(html, userCss ?? "");
        }

        public ICommand? Command { get; set; }

        protected override void OnMeasure(int widthMeasureSpec, int heightMeasureSpec)
        {
            _documentView.SetViewportSize(new Microsoft.Maui.Graphics.Size(widthMeasureSpec, heightMeasureSpec));
            var size = _documentView.MeasureDocument(new Microsoft.Maui.Graphics.Size(widthMeasureSpec, heightMeasureSpec));
            SetMeasuredDimension((int)size.Width, (int)size.Height);            
        }

        protected override void OnDraw(Canvas? canvas)
        {
            if (canvas != null)
            {
                _documentView.DrawDocument(canvas, Width, Height);
            }
        }
        
        protected override void OnSizeChanged(int w, int h, int oldw, int oldh)
        {
            _documentView.SetViewportSize(new Microsoft.Maui.Graphics.Size(w, h));
        }


        public override bool OnTouchEvent(MotionEvent? e)
        {
            if (e == null)
            {
                return false;
            }

            System.Diagnostics.Debug.WriteLine(e.Action);
            LiteHtmlEvent lhe = e.Action switch
            {
                MotionEventActions.Down => LiteHtmlEvent.Down,
                MotionEventActions.Up => LiteHtmlEvent.Up,
                MotionEventActions.Cancel => LiteHtmlEvent.Leave,
                MotionEventActions.Move => LiteHtmlEvent.Move,
                _ => LiteHtmlEvent.None
            };

            if(lhe == LiteHtmlEvent.None)
            {
                return false;
            }

            if(_documentView.ReportEvent(lhe, (int)e.GetX(), (int)e.GetY(), (int)e.GetX(), (int)e.GetY()))
            {
                Invalidate();
            }
            return true;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _documentView.Dispose();
            }
            base.Dispose(disposing);
        }

    }


    public partial class LiteHtmlHandler : ViewHandler<ILiteHtml, AndroidLiteHtmlView>
    {
       
        protected override AndroidLiteHtmlView CreatePlatformView()
        {
            var view = new AndroidLiteHtmlView(Context!);            
            return view;
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
