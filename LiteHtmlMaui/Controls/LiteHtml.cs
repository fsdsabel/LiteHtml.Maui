using Microsoft.Maui;
using Microsoft.Maui.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace LiteHtmlMaui.Controls
{

    /// <summary>
    /// LiteHtml is a control that renders HTML
    /// </summary>
    public class LiteHtml : View, IElementConfiguration<LiteHtml>, ILiteHtml
    {
        /// <summary>
        /// HTML to render
        /// </summary>
        public static readonly BindableProperty HtmlProperty = BindableProperty.Create(nameof(ILiteHtml.Html), typeof(string),
            typeof(ILiteHtml));

        /// <summary>
        /// HTML Source to use instead of <see cref="HtmlProperty"/>
        /// </summary>
        public static readonly BindableProperty SourceProperty = BindableProperty.Create(nameof(ILiteHtml.Source), typeof(ILiteHtmlSource),
            typeof(ILiteHtml));

        /// <summary>
        /// Command that is executed when an anchor is clicked. The commands argument is a string containing the URL
        /// </summary>
        public static readonly BindableProperty CommandProperty = BindableProperty.Create(nameof(ILiteHtml.Command), typeof(ICommand),
            typeof(ILiteHtml), null);

        private readonly Lazy<PlatformConfigurationRegistry<LiteHtml>> _platformConfigurationRegistry;

        /// <summary>
        /// Constructor
        /// </summary>
        public LiteHtml()
        {
            _platformConfigurationRegistry = new Lazy<PlatformConfigurationRegistry<LiteHtml>>(() => new PlatformConfigurationRegistry<LiteHtml>(this));
        }

        /// <summary>
        /// HTML to render
        /// </summary>
        public string Html
        {
            get => (string)GetValue(HtmlProperty);
            set => SetValue(HtmlProperty, value);
        }

        /// <summary>
        /// HTML Source to use instead of <see cref="HtmlProperty"/>
        /// </summary>
        public ILiteHtmlSource Source
        {
            get => (ILiteHtmlSource)GetValue(SourceProperty);
            set => SetValue(SourceProperty, value);
        }

        /// <summary>
        /// Command that is executed when an anchor is clicked. The commands argument is a string containing the URL
        /// </summary>
        public ICommand Command
        {
            get => (ICommand)GetValue(CommandProperty);
            set => SetValue(CommandProperty, value);
        }

        /// <inheritdoc />
        public IPlatformElementConfiguration<T, LiteHtml> On<T>() where T : IConfigPlatform
        {            
            return _platformConfigurationRegistry.Value.On<T>();
        }
    }
}
