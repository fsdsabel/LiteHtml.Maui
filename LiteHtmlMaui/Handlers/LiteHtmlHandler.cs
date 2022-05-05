using LiteHtmlMaui.Controls;
using Microsoft.Maui;
using Microsoft.Maui.Handlers;
using System;

namespace LiteHtmlMaui.Handlers
{
    public partial class LiteHtmlHandler 
    {
        public static PropertyMapper<ILiteHtml, LiteHtmlHandler> LiteHtmlMapper = new PropertyMapper<ILiteHtml, LiteHtmlHandler>(ViewHandler.ViewMapper)
        {
            [nameof(ILiteHtml.Html)] = MapHtml,
            [nameof(ILiteHtml.Source)] = MapSource,
            [nameof(ILiteHtml.Command)] = MapCommand
        };

        public LiteHtmlHandler() : base(LiteHtmlMapper)
        {
        }

        public LiteHtmlHandler(PropertyMapper mapper) : base(mapper)
        {

        }     
    }
}
