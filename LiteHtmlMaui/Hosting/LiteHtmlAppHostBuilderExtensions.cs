using LiteHtmlMaui.Handlers.Native;
using LiteHtmlMaui.Controls;
using LiteHtmlMaui.Handlers;

namespace LiteHtmlMaui.Hosting
{
    public static class LiteHtmlAppHostBuilderExtensions
    {
        /// <summary>
        /// Configure global LiteHtml settings.
        /// </summary>
        public static MauiAppBuilder ConfigureLiteHtml(this MauiAppBuilder builder, Action<ILiteHtmlConfiguration> configureDelegate)
        {
            var config = new LiteHtmlConfiguration();
            configureDelegate(config);
            LiteHtmlDocumentView.Configure(config);
            builder.ConfigureMauiHandlers(handlers =>
            {
                handlers.AddLiteHtml();
            });
            // builder.ConfigureServices<LiteHtmlConfigurationBuilder>((_, configBuilder) => configureDelegate(configBuilder));
            return builder;
        }

        public static IMauiHandlersCollection AddLiteHtml(this IMauiHandlersCollection handlersCollection)
        {
            handlersCollection.AddHandler<LiteHtml, LiteHtmlHandler>();
            return handlersCollection;
        }
    }

}
