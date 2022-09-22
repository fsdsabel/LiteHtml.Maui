namespace LiteHtmlMaui.Controls
{
    /// <summary>
    /// Defines an HTML and CSS source for the <see cref="LiteHtml"/> control.
    /// </summary>
    public interface ILiteHtmlSource
    {
        /// <summary>
        /// Returns the HTML to show
        /// </summary>
        string? Html { get; }

        /// <summary>
        /// Returns CSS to use for layouting
        /// </summary>
        string? Css { get; }

        /// <summary>
        /// Returns data for the given URL (i.e. image data or CSS)
        /// </summary>
        /// <param name="url">Request URL</param>
        /// <returns>Data stream or null, if the url cannot be opened.</returns>
        Task<Stream?> GetStreamForUrlAsync(string url);
    }
}
