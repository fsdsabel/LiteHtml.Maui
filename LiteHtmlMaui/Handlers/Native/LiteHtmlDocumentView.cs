using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Text;

namespace LiteHtmlMaui.Handlers.Native
{
    delegate Task<Stream> LiteHtmlResolveResourceDelegate(string url);

    delegate void LiteHtmlRedrawView();

    abstract class LiteHtmlDocumentView
    {
        protected static string? MasterStylesheet;

        internal static void Configure(ILiteHtmlConfiguration configuration)
        {
            MasterStylesheet = configuration.MasterStyleSheet;
        }
    }

    abstract class LiteHtmlDocumentView<THDC, TBitmap, TFont> : LiteHtmlDocumentView, IDisposable 
    {
        private LiteHtmlDocumentSafeHandle? _document;

        private MauiContainerCallbacks _callbacks;
        private bool disposedValue;
        private string? _html;
        private string _userCss = "";
        protected readonly LiteHtmlImageCache<TBitmap> _bitmaps;
        private readonly LiteHtmlResolveResourceDelegate _resolveResource;
        private readonly LiteHtmlRedrawView _redrawView;

        public event EventHandler<string>? AnchorClicked;

        protected LiteHtmlDocumentView(LiteHtmlResolveResourceDelegate resolveResource, LiteHtmlRedrawView redrawView)
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
                PtToPx = PtToPxCb,
                ImportCss = ImportCssCb,
                FreeString = FreeStringCb
            };
            _resolveResource = resolveResource ?? throw new ArgumentNullException(nameof(resolveResource));
            _redrawView = redrawView ?? throw new ArgumentNullException(nameof(redrawView));
        }

        public virtual void LoadHtml(string? html, string userCss = "")
        {
            UnloadDocument();
            _html = html;
            _userCss = userCss;
            if (html != null)
            {
                _document = LiteHtmlInterop.CreateDocument(_callbacks, html, MasterStylesheet, userCss);
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
                _document = LiteHtmlInterop.CreateDocument(_callbacks, _html, MasterStylesheet, _userCss);
            }
        }

        public virtual void UnloadDocument()
        {
            if (disposedValue) throw new ObjectDisposedException(nameof(LiteHtmlDocumentView<THDC, TBitmap, TFont>));

            _document?.Dispose();            
            _document = null;
        }

        protected abstract int PtToPxCb(int pt);
        protected virtual void OnAnchorClickCb(string url)
        {
            AnchorClicked?.Invoke(this, url);
        }
        protected abstract void GetDefaultsCb(ref Defaults defaults);
        protected abstract void DrawListMarkerCb(ref ListMarker listMarker, ref FontDesc font);


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
                if (!_bitmaps.IsLoading(url))
                {
                    Task.Run(async () =>
                    {
                        var ibmp = await _bitmaps.GetOrCreateImageAsync(url, LoadResourceAsync);
                        if (ibmp != null)
                        {
                            _redrawView();
                        }
                    });
                }

            }
        }


        protected abstract void DrawBordersCb(ref Borders borders, ref Position position, bool root);
        protected abstract void SetCursorCb(string cursor);
        protected abstract void DrawBackgroundCb(ref BackgroundPaint bg);
        protected abstract void FillFontMetricsCb(ref FontDesc font, ref FontMetrics fm);
        protected abstract void DrawTextCb(IntPtr hdc, string text, ref FontDesc font, WebColor color, ref Position position);
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

        protected virtual void ImportCssCb(out IntPtr text, string url, out IntPtr baseUrl)
        {
            text = IntPtr.Zero;
            baseUrl = IntPtr.Zero;
            var stream = _resolveResource(url).GetAwaiter().GetResult();
            if (stream != null)
            {
                using var reader = new StreamReader(stream);
                text = Marshal.StringToCoTaskMemUTF8(reader.ReadToEnd());
            }
        }

        private void FreeStringCb(IntPtr str)
        {
            Marshal.FreeCoTaskMem(str);
        }

        private static ConcurrentDictionary<FontDesc, TFont> _fontCache = new ConcurrentDictionary<FontDesc, TFont>();

        protected static TFont ResolveFont(ref FontDesc font, Func<FontDesc, bool, TFont?> createFont) 
        {
            if(_fontCache.TryGetValue(font, out var cachedFont))
            {
                return cachedFont;
            }

            TFont? result;
            var faceNames = font.FaceName.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            foreach(var name in faceNames)
            {
                var fontDesc = new FontDesc
                {
                    FaceName = name.Trim('"').Trim(),
                    Decoration = font.Decoration,
                    FontMetrics = font.FontMetrics,
                    Italic = font.Italic,
                    Size = font.Size,
                    Weight = font.Weight
                };
                result = createFont(fontDesc, false);
                if(result != null)
                {
                    _fontCache.TryAdd(font, result);
                    return result;
                }
            }

            result = createFont(font, true) ?? throw new InvalidOperationException("Font resolved to null");
            _fontCache.TryAdd(font, result);
            return result;
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
        

        protected string IndexToAlpha(int index, string digits)
        {
            const int ColumnBase = 26;
            const int DigitMax = 7; // ceil(log26(Int32.Max))

            if (index <= 0)
                return "";

            if (index <= ColumnBase)
                return digits[index - 1].ToString();

            var sb = new StringBuilder();
            sb.Append(' ', DigitMax);
            var current = index;
            var offset = DigitMax;
            while (current > 0)
            {
                sb[--offset] = digits[--current % ColumnBase];
                current /= ColumnBase;
            }
            return sb.ToString(offset, DigitMax - offset);
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
