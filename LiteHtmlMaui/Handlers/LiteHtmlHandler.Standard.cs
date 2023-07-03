using LiteHtmlMaui.Controls;
using Microsoft.Maui;
using Microsoft.Maui.Handlers;
using System;

namespace LiteHtmlMaui.Handlers
{
    public partial class LiteHtmlHandler : ViewHandler<ILiteHtml, object>
    {
        protected override object CreatePlatformView()
        {
            return new object();
        }

        /// <summary>
        /// Maps HTML
        /// </summary>
        public static void MapHtml(LiteHtmlHandler handler, ILiteHtml liteHtml)
        {            
        }

        /// <summary>
        /// Maps a source
        /// </summary>
        public static void MapSource(LiteHtmlHandler handler, ILiteHtml liteHtml)
        {
        }

        /// <summary>
        /// Maps the command
        /// </summary>
        public static void MapCommand(LiteHtmlHandler handler, ILiteHtml liteHtml)
        {
        }

        /// <summary>
        /// Maps the text color
        /// </summary>
        public static void MapTextColor(LiteHtmlHandler handler, ILiteHtml liteHtml)
        {
        }

        /// <summary>
        /// Maps the font
        /// </summary>
        public static void MapFont(LiteHtmlHandler handler, ILiteHtml liteHtml)
        {
        }

        /// <summary>
        /// Maps the character spacing
        /// </summary>
        public static void MapCharacterSpacing(LiteHtmlHandler handler, ILiteHtml liteHtml)
        {
        }
    }
}
