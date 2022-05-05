using System.Threading;

namespace LiteHtmlMaui.Handlers.Native
{
    class LiteHtmlBitmap<TBitmap>
    {
        private int _refcount = 1;

        public LiteHtmlBitmap(TBitmap img)
        {
            Image = img;
        }

        public TBitmap Image { get; private set; }

        public void Addref()
        {
            Interlocked.Increment(ref _refcount);
        }

        public void Release()
        {
            Interlocked.Decrement(ref _refcount);
        }

        public bool CanRelease => _refcount <= 0;
    }
}
