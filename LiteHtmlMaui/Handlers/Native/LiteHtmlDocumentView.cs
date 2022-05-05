using LiteHtmlMaui.Controls;
using LiteHtmlMaui.Hosting;
using Microsoft.Maui.Graphics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace LiteHtmlMaui.Handlers.Native
{
    delegate Task<Stream> LiteHtmlResolveResourceDelegate(string url);

    abstract class LiteHtmlDocumentView
    {
        private static LiteHtmlContextSafeHandle _context = null!;

        protected LiteHtmlContextSafeHandle LiteHtmlContext => _context ?? throw new InvalidOperationException("LiteHtml not configured properly. Use ConfigureLiteHtml in Startup!");

        internal static void Configure(ILiteHtmlConfiguration configuration)
        {
            _context = LiteHtmlInterop.CreateContext();
            LiteHtmlInterop.LoadMasterStylesheet(_context, configuration.MasterStyleSheet);
        }
    }

    abstract class LiteHtmlDocumentView<THDC, TBitmap> : LiteHtmlDocumentView, IDisposable 
    {
        private LiteHtmlDocumentSafeHandle? _document;

        private MauiContainerCallbacks _callbacks;
        private bool disposedValue;
        private string? _html;
        private string _userCss;
        protected readonly LiteHtmlImageCache<TBitmap> _bitmaps;
        private readonly LiteHtmlResolveResourceDelegate _resolveResource;


        public event EventHandler<string>? AnchorClicked;

        protected LiteHtmlDocumentView(LiteHtmlResolveResourceDelegate resolveResource)
        {
            _bitmaps = new LiteHtmlImageCache<TBitmap>(CreatePlatformBitmapAsync);
            _callbacks = new MauiContainerCallbacks
            {
                TextWidth = TextWidthCb,
                GetClientRect = GetClientRectCb,
                DrawText = DrawTextCb,
                FillFontMetrics = FillFontMetricsCb,
                DrawBackground = DrawBackgroundCb,
                SetCursor = SetCursorCb,
                DrawBorders = DrawBordersCb,
                LoadImage = LoadImageCb,
                GetImageSize = GetImageSizeCb,
                DrawListMarker = DrawListMarkerCb,
                GetDefaults = GetDefaultsCb,
                OnAnchorClick = OnAnchorClickCb,
                PtToPx = PtToPxCb
            };
            _resolveResource = resolveResource;
        }

        public virtual void LoadHtml(string? html, string userCss = "")
        {
            UnloadDocument();
            _html = html;
            _userCss = userCss;
            if (html != null)
            {
                _document = LiteHtmlInterop.CreateDocument(LiteHtmlContext, _callbacks, html, userCss);
            }
        }

        public virtual void ReloadDocument()
        {
            if (_document != null)
            {
                UnloadDocument();
            }
            if (_html != null)
            {
                _document = LiteHtmlInterop.CreateDocument(LiteHtmlContext, _callbacks, _html, _userCss);
            }
        }

        public virtual void UnloadDocument()
        {
            if (disposedValue) throw new ObjectDisposedException(nameof(LiteHtmlDocumentView<THDC, TBitmap>));

            _document?.Dispose();            
            _document = null;
        }

        protected abstract int PtToPxCb(int pt);
        protected virtual void OnAnchorClickCb(string url)
        {
            AnchorClicked?.Invoke(this, url);
        }
        protected abstract void GetDefaultsCb(ref Defaults defaults);
        protected abstract void DrawListMarkerCb(IntPtr listMarker, ref FontDesc font);


        protected virtual void GetImageSizeCb(string source, string baseUrl, ref MauiSize size)
        {            
            var url = CombineUrl(baseUrl, source);
            var bmp = GetImage(url);
            if(bmp != null)
            {
                var imgSize = GetImageSize(bmp.Image);
                size.Width = imgSize.Width;
                size.Height = imgSize.Height;
            } 
        }


        protected virtual void LoadImageCb(string source, string baseUrl, bool redrawOnReady)
        {
            var url = CombineUrl(baseUrl, source);
            var bmp = GetImage(url);
            if (bmp == null)
            {
                //GC.Collect();
                // TODO load it in background and redraw if necessary
                // loading this in a thread crashes .. some GC related issue ...

                // execute in thread
                var ibmp = _bitmaps.GetOrCreateImageAsync(url, LoadResourceAsync).GetAwaiter().GetResult();
                if (ibmp != null && redrawOnReady)
                {
                    // TODO trigger redraw
                }

                // ~execute in thread

            } 
        }


        protected abstract void DrawBordersCb(ref Borders borders, ref Position position, bool root);
        protected abstract void SetCursorCb(string cursor);
        protected abstract void DrawBackgroundCb(ref BackgroundPaint bg);
        protected abstract void FillFontMetricsCb(ref FontDesc font, ref FontMetrics fm);
        protected abstract void DrawTextCb(IntPtr hdc, string text, ref FontDesc font, ref WebColor color, ref Position position);
        protected virtual Position GetClientRectCb()
        {
            return new Position
            {
                X = 0,
                Y = 0,
                Width = (int)ViewportSize.Width,
                Height = (int)ViewportSize.Height
            };
        }
        protected abstract int TextWidthCb(string text, ref FontDesc font);
        protected abstract Task<TBitmap> CreatePlatformBitmapAsync(Stream stream);
        protected virtual async Task<Stream> LoadResourceAsync(string url)
        {
            return await _resolveResource(url);
        }
        protected abstract MauiSize GetImageSize(TBitmap image);

        protected virtual LiteHtmlBitmap<TBitmap>? GetImage(string url)
        {
            return _bitmaps.GetImage(url);
        }

        public virtual void DrawDocument(THDC hdc, int width, int height)
        {
            if (ValidateDocument())
            {
                System.Diagnostics.Debug.WriteLine($"LiteHtml - drawing:  {width} {height}");
                LiteHtmlInterop.DrawDocument(_document, IntPtr.Zero, new MauiSize(width, height));
            }
        }


        public virtual Size MeasureDocument(Size availableSize)
        {
            if(ValidateDocument())
            {
                
                var availableMauiSize = new MauiSize { Width = ConvertDimension(availableSize.Width), Height = ConvertDimension(availableSize.Height) };

                var size = LiteHtmlInterop.MeasureDocument(_document, availableMauiSize);
                System.Diagnostics.Debug.WriteLine($"LiteHtml - Measured size: {size.Width} {size.Height}");
                return new Size(size.Width, size.Height);
            }
            return Size.Zero;
        }

        private int ConvertDimension(double dimension)
        {
            if (double.IsInfinity(dimension)) return -1;
            if (dimension >= int.MaxValue) return -1;
            return (int)dimension;
        }

        public virtual void SetViewportSize(Size finalSize)
        {
            ViewportSize = new Size
            {
                Width = ConvertDimension(finalSize.Width),
                Height = ConvertDimension(finalSize.Height)
            };
        }

        public Size ViewportSize { get; private set; }

        public virtual bool ReportEvent(LiteHtmlEvent e, int x, int y, int client_x, int client_y)
        {
            if (ValidateDocument())
            {
                return LiteHtmlInterop.ReportEvent(_document, e, x, y, client_x, client_y);
            }
            return false;
        }

    

        [MemberNotNullWhen(true, nameof(_document))]
        protected bool ValidateDocument()
        {
            if (_document is null || _document.IsInvalid)
            {
                return false;
            }
            return true;
        }

        protected string CombineUrl(string baseUrl, string url)
        {
            if (string.IsNullOrEmpty(baseUrl)) return url;
            return new Uri(new Uri(baseUrl), url).ToString();
        }
        

        protected virtual void Dispose(bool disposing)
        {

            if (!disposedValue)
            {
                UnloadDocument();
                disposedValue = true;
            }
        }

        public void Dispose()
        {       
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        ~LiteHtmlDocumentView()
        {
            Dispose(disposing: false);
        }
    }
}
