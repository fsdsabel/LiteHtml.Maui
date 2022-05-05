using LiteHtmlMaui.Resources;

namespace LiteHtmlMaui.Hosting
{
    public static class LiteHtmlConfigurationBuilderExtensions
    {
        /// <summary>
        /// Use the embedded master style sheet for rendering with LiteHtml.
        /// </summary>
        public static ILiteHtmlConfiguration UseDefaultMasterStyleSheet(this ILiteHtmlConfiguration liteHtmlConfiguration)
        {
            using var mastercss = typeof(LiteHtmlConfigurationBuilderExtensions).Assembly.GetManifestResourceStream(typeof(ResourcesAnchor), "master.css")
                ?? throw new Exception("Master style sheet resource not found");
            
            using var reader = new StreamReader(mastercss);
            liteHtmlConfiguration.MasterStyleSheet = reader.ReadToEnd();
            return liteHtmlConfiguration;
        }

        /// <summary>
        /// Use the given <paramref name="css"/> string as master style sheet for rendering with LiteHtml.
        /// </summary>
        public static ILiteHtmlConfiguration UseMasterStyleSheet(this ILiteHtmlConfiguration liteHtmlConfiguration, string css)
        {
            liteHtmlConfiguration.MasterStyleSheet = css;
            return liteHtmlConfiguration;
        }
    }

}
