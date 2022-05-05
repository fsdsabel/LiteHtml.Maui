using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace LiteHtmlMaui.Handlers.Native
{
    delegate Task<Stream> LoadImageDataDelegate(string url);
    class LiteHtmlImageCache<TBitmap>
    {
        private readonly Dictionary<string, LiteHtmlBitmap<TBitmap>> _bitmaps = new Dictionary<string, LiteHtmlBitmap<TBitmap>>();
        private readonly object _lock = new object();
        private readonly Func<Stream, Task<TBitmap>> _imageFactory;

        public LiteHtmlImageCache(Func<Stream, Task<TBitmap>> imageFactory)
        {
            _imageFactory = imageFactory;
        }

        public LiteHtmlBitmap<TBitmap>? GetImage(string src)
        {
            return GetImage(src, true);
        }

        public LiteHtmlBitmap<TBitmap>? GetImage(string src, bool addref)
        {
            lock(_lock)
            {
                if(_bitmaps.TryGetValue(src, out var bitmap))
                {
                    if (addref) bitmap.Addref();
                    return bitmap;
                }
                return null;
            }            
        }

        public bool IsLoading(string url)
        {
            lock(_loading)
            {
                return _loading.ContainsKey(url);
            }
        }

        private readonly Dictionary<string, ManualResetEventSlim> _loading = new Dictionary<string, ManualResetEventSlim>();

        public async Task<LiteHtmlBitmap<TBitmap>?> GetOrCreateImageAsync(string src, LoadImageDataDelegate loadImageData)
        {
            var bmp = GetImage(src);
            if (bmp != null)
            {
                return bmp;
            }

            ManualResetEventSlim? myDoneEvent = null;
            try
            {
                lock(_loading)
                {                    
                    if(_loading.TryGetValue(src, out var doneEvent))
                    {
                        // already loading
                        doneEvent.Wait();
                        return GetImage(src);
                    }
                    _loading.Add(src, myDoneEvent = new ManualResetEventSlim());
                }
                var stream = await loadImageData(src);
                if (stream == null)
                {
                    return null;
                }

                bmp = new LiteHtmlBitmap<TBitmap>(await _imageFactory(stream));
                lock (_bitmaps)
                {
                    _bitmaps[src] = bmp;
                }
                myDoneEvent.Set();

                return bmp;
            }
            catch(Exception ex)
            {
                myDoneEvent?.Set();
                Debug.WriteLine(ex);
                return null;
            }
        }

        public void Clean()
        {
            lock(_lock)
            {
                var toRelease = _bitmaps.Where(b => b.Value.CanRelease).ToArray();
                foreach(var src in toRelease)
                {
                    _bitmaps.Remove(src.Key);
                }
            }
        }

    }
}
