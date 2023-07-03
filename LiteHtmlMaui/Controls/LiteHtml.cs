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

        /// <summary>
		/// The backing store for the <see cref="FontFamily" /> bindable property.
		/// </summary>
		public static readonly BindableProperty FontFamilyProperty =
            BindableProperty.Create(nameof(FontFamily), typeof(string), typeof(ILiteHtml), default(string),
                                    propertyChanged: OnFontFamilyChanged);

        /// <summary>
        /// The backing store for the <see cref="FontSize" /> bindable property.
        /// </summary>
        public static readonly BindableProperty FontSizeProperty =
            BindableProperty.Create(nameof(FontSize), typeof(double), typeof(ILiteHtml), 0d,
                                    propertyChanged: OnFontSizeChanged,
                                    defaultValueCreator: FontSizeDefaultValueCreator);


        /// <summary>
        /// The backing store for the <see cref="ITextElement.TextColor" /> bindable property.
        /// </summary>
        public static readonly BindableProperty TextColorProperty =
            BindableProperty.Create(nameof(TextColor), typeof(Color), typeof(ILiteHtml), null,
                                    propertyChanged: OnTextColorPropertyChanged);

        /// <summary>
		/// The backing store for the <see cref="ITextElement.CharacterSpacing" /> bindable property.
		/// </summary>
		public static readonly BindableProperty CharacterSpacingProperty =
            BindableProperty.Create(nameof(CharacterSpacing), typeof(double), typeof(ILiteHtml), 0.0d,
                propertyChanged: OnCharacterSpacingPropertyChanged);

        
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

        /// <summary>
        /// Sets the default font family to be used. Can be overridden by HTML.
        /// </summary>
        public string FontFamily
        {
            get { return (string)GetValue(FontFamilyProperty); }
            set { SetValue(FontFamilyProperty, value); }
        }


        /// <summary>
        /// The default font size of the rendered HTML. Can be overridden by HTML.
        /// </summary>
        [System.ComponentModel.TypeConverter(typeof(FontSizeConverter))]
        public double FontSize
        {
            get { return (double)GetValue(FontSizeProperty); }
            set { SetValue(FontSizeProperty, value); }
        }

        /// <summary>
        /// The default color of the rendered HTML text. Can be overridden by HTML.
        /// </summary>
        public Color TextColor
        {
            get { return (Color)GetValue(TextColorProperty); }
            set { SetValue(TextColorProperty, value); }
        }

        /// <summary>
        /// The default character spacing of the rendered HTML text. Can be overridden by HTML.
        /// </summary>
        public double CharacterSpacing
        {
            get { return (double)GetValue(CharacterSpacingProperty); }
            set { SetValue(CharacterSpacingProperty, value); }
        }

        public Microsoft.Maui.Font Font 
        { 
            get
            {
                return Microsoft.Maui.Font.OfSize(FontFamily, FontSize);
            }        
        }


        /// <inheritdoc />
        public IPlatformElementConfiguration<T, LiteHtml> On<T>() where T : IConfigPlatform
        {            
            return _platformConfigurationRegistry.Value.On<T>();
        }

        static void OnFontFamilyChanged(BindableObject bindable, object oldValue, object newValue)
            => ((LiteHtml)bindable).OnFontFamilyChanged((string)oldValue, (string)newValue);

        static void OnFontSizeChanged(BindableObject bindable, object oldValue, object newValue)
            => ((LiteHtml)bindable).OnFontSizeChanged((double)oldValue, (double)newValue);

        

        static object FontSizeDefaultValueCreator(BindableObject bindable)
            => ((LiteHtml)bindable).FontSizeDefaultValueCreator();

        static void OnTextColorPropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            ((LiteHtml)bindable).HandleTextColorChanged();
        }

        private static void OnCharacterSpacingPropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            ((LiteHtml)bindable).InvalidateMeasure();
        }


        void OnFontFamilyChanged(string oldValue, string newValue) =>
            HandleFontChanged();

        void OnFontSizeChanged(double oldValue, double newValue) =>
            HandleFontChanged();

        double FontSizeDefaultValueCreator() => Handler?.MauiContext?.Services?.GetService<IFontManager>()?.DefaultFontSize ?? 0d;


        void HandleFontChanged()
        {
            Handler?.UpdateValue(nameof(ITextStyle.Font));
            InvalidateMeasure();
        }

        void HandleTextColorChanged()
        {
            Handler?.UpdateValue(nameof(ITextStyle.TextColor));            
        }
    }
}
