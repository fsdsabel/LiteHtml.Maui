namespace Microsoft.Maui.Hosting
{
    /// <summary>
    /// Configuration of the LiteHtml control
    /// </summary>
    public interface ILiteHtmlConfiguration
    {
        /// <summary>
        /// Sets the master stylesheet of LiteHtml. This defines
        /// globally basic rendering and layout properties.
        /// </summary>
        string MasterStyleSheet { get; set; }
    }

}
