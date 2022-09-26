using LiteHtmlMaui.Handlers.Native;
using LiteHtmlMaui.Controls;
using LiteHtmlMaui.Handlers;

namespace Microsoft.Maui.Hosting
{
    /// <summary>
    /// HostBuilder Extensions
    /// </summary>
    public static class LiteHtmlAppHostBuilderExtensions
    {
        /// <summary>
        /// Use LiteHtml with custom configuration.
        /// </summary>
        public static MauiAppBuilder UseLiteHtml(this MauiAppBuilder builder, Action<ILiteHtmlConfiguration> configureDelegate)
        {
            var config = new LiteHtmlConfiguration();
            configureDelegate(config);
            LiteHtmlDocumentView.Configure(config);
            builder.ConfigureMauiHandlers(handlers =>
            {
                handlers.AddLiteHtml();
            });
            return builder;
        }

        /// <summary>
        /// Use LiteHtml with default configuration.
        /// </summary>
        public static MauiAppBuilder UseLiteHtml(this MauiAppBuilder builder)
        {
            return builder.UseLiteHtml(o => o.UseDefaultMasterStyleSheet());
        }

        /// <summary>
        /// Adds LiteHtml to the list of available control handlers.
        /// </summary>
        public static IMauiHandlersCollection AddLiteHtml(this IMauiHandlersCollection handlersCollection)
        {
            handlersCollection.AddHandler<LiteHtml, LiteHtmlHandler>();
            return handlersCollection;
        }
    }

}
