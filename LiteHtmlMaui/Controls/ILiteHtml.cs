using System.Windows.Input;

namespace LiteHtmlMaui.Controls
{
    /// <summary>
    /// Handler interface for <see cref="LiteHtml"/>
    /// </summary>
    public interface ILiteHtml : IView, ITextStyle
    {
        /// <summary>
        /// HTML to load
        /// </summary>
        string? Html { get; }

        /// <summary>
        /// HTML Source
        /// </summary>
        ILiteHtmlSource? Source { get; }

        /// <summary>
        /// Command that is executed when an anchor is clicked
        /// </summary>
        ICommand? Command { get; }
    }
}
