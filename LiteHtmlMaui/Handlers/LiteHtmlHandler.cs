using LiteHtmlMaui.Controls;
using Microsoft.Maui;
using Microsoft.Maui.Handlers;
using System;

namespace LiteHtmlMaui.Handlers
{
    public partial class LiteHtmlHandler 
    {
        /// <summary>
        /// Mapper Functions
        /// </summary>
        public static PropertyMapper<ILiteHtml, LiteHtmlHandler> LiteHtmlMapper = new PropertyMapper<ILiteHtml, LiteHtmlHandler>(ViewHandler.ViewMapper)
        {
            [nameof(ILiteHtml.Html)] = MapHtml,
            [nameof(ILiteHtml.Source)] = MapSource,
            [nameof(ILiteHtml.Command)] = MapCommand,
            [nameof(ILiteHtml.TextColor)] = MapTextColor,
            [nameof(ILiteHtml.Font)] = MapFont,
            [nameof(ILiteHtml.CharacterSpacing)] = MapCharacterSpacing,
        };

        /// <summary>
        /// Constructor
        /// </summary>
        public LiteHtmlHandler() : base(LiteHtmlMapper)
        {
        }

        /// <summary>
        /// Constructor
        /// </summary>
        public LiteHtmlHandler(PropertyMapper mapper) : base(mapper)
        {

        }     
    }
}
