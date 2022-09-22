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
        private Func<string, Task<Stream?>>? _externalResourceResolver;
        private string? _html;

        public AndroidLiteHtmlView(Context context) : base(context)
        {
            _documentView = new AndroidLiteHtmlDocumentView(context, ResolveResource, OnRedraw);
            _documentView.AnchorClicked += OnAnchorClicked;                      
        }

        private void OnRedraw()
        {
            RequestLayout();
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
            if (_externalResourceResolver != null)
            {
                var result = _externalResourceResolver(url).GetAwaiter().GetResult();
                if (result != null) return Task.FromResult(result);
            }

            var client = new HttpClient();
            return Task.FromResult(client.GetStreamAsync(url).GetAwaiter().GetResult());  // async crashes :(            
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

        protected override void OnMeasure(int widthMeasureSpec, int heightMeasureSpec)
        {
            var widthMode = MeasureSpec.GetMode(widthMeasureSpec);
            double widthSize = MeasureSpec.GetSize(widthMeasureSpec);
            var heightMode = MeasureSpec.GetMode(heightMeasureSpec);
            double heightSize = MeasureSpec.GetSize(heightMeasureSpec);
            if (widthMode == MeasureSpecMode.Unspecified)
            {
                widthSize = double.PositiveInfinity;
            }
            if (heightMode == MeasureSpecMode.Unspecified)
            {
                heightSize = double.PositiveInfinity;
            }
            _documentView.SetViewportSize(new Microsoft.Maui.Graphics.Size(widthSize, heightSize));
            var size = _documentView.MeasureDocument(new Microsoft.Maui.Graphics.Size(widthSize, heightSize));
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

    /// <summary>
    /// LiteHtml MAUI Handler for Android
    /// </summary>
    public partial class LiteHtmlHandler : ViewHandler<ILiteHtml, AndroidLiteHtmlView>
    {
        /// <inheritdoc />
        protected override AndroidLiteHtmlView CreatePlatformView()
        {
            var view = new AndroidLiteHtmlView(Context!);            
            return view;
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
    }
}
