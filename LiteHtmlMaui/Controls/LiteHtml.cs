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
    public interface ILiteHtml : IView
    {
        string? Html { get; }

        ILiteHtmlSource? Source { get; }

        ICommand? Command { get; }
    }

    public interface ILiteHtmlSource
    {
        string? Html { get; }

        string? Css { get; }
    }

    public class LiteHtml : View, IElementConfiguration<LiteHtml>, ILiteHtml
    {
        public static readonly BindableProperty HtmlProperty = BindableProperty.Create(nameof(ILiteHtml.Html), typeof(string),
            typeof(ILiteHtml));

        public static readonly BindableProperty SourceProperty = BindableProperty.Create(nameof(ILiteHtml.Source), typeof(ILiteHtmlSource),
            typeof(ILiteHtml));

        public static readonly BindableProperty CommandProperty = BindableProperty.Create(nameof(ILiteHtml.Command), typeof(ICommand),
            typeof(ILiteHtml), null);

        private readonly Lazy<PlatformConfigurationRegistry<LiteHtml>> _platformConfigurationRegistry;

        public LiteHtml()
        {
            _platformConfigurationRegistry = new Lazy<PlatformConfigurationRegistry<LiteHtml>>(() => new PlatformConfigurationRegistry<LiteHtml>(this));
        }

        public string Html
        {
            get => (string)GetValue(HtmlProperty);
            set => SetValue(HtmlProperty, value);
        }

        public ILiteHtmlSource Source
        {
            get => (ILiteHtmlSource)GetValue(SourceProperty);
            set => SetValue(SourceProperty, value);
        }

        public ICommand Command
        {
            get => (ICommand)GetValue(CommandProperty);
            set => SetValue(CommandProperty, value);
        }

        public IPlatformElementConfiguration<T, LiteHtml> On<T>() where T : IConfigPlatform
        {            
            return _platformConfigurationRegistry.Value.On<T>();
        }
    }
}
