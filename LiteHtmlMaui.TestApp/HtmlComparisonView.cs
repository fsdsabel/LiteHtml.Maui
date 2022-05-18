using LiteHtmlMaui.Controls;
using LiteHtmlMaui.TestApp.Resources;

namespace LiteHtmlMaui.TestApp;

public class HtmlLinkClickedEventArgs : EventArgs
{
    public HtmlLinkClickedEventArgs(string url)
    {
        Url = url;
    }

    public string Url { get; }
}

public class HtmlComparisonView : ContentView
{
    class MyLiteHtmlSource : ILiteHtmlSource
    {
        public string Html { get; set; }

        public string Css { get; set; }

        public Task<Stream> GetStreamForUrlAsync(string url)
        {
            if(url== "test.css")
            {
                return Task.FromResult(ResourceLoader.LoadStreamFromEmbeddedResource("Html.test.css"));
            }
            return Task.FromResult<Stream>(null);
        }
    }


    public static readonly BindableProperty HtmlProperty =
        BindableProperty.Create(nameof(Html), typeof(string), typeof(HtmlComparisonView),
            propertyChanged: (bindable, oldValue, newValue) =>
            {
                var control = (HtmlComparisonView)bindable;
                var html = newValue as string ?? "";
                html = $"<style>{ResourceLoader.LoadStringFromEmbeddedResource("Html.master.css")}{ResourceLoader.LoadStringFromEmbeddedResource("Html.test.css")}</style>{html}";

                control.HtmlSource = new HtmlWebViewSource
                {
                    Html = html
                };

                control.LiteHtmlSource = new MyLiteHtmlSource
                {
                    Html = html
                };
            });

    public static readonly BindableProperty HtmlSourceProperty =
        BindableProperty.Create(nameof(HtmlSource), typeof(HtmlWebViewSource), typeof(HtmlComparisonView));

    public static readonly BindableProperty LiteHtmlSourceProperty =
        BindableProperty.Create(nameof(LiteHtmlSource), typeof(ILiteHtmlSource), typeof(HtmlComparisonView));

    public event EventHandler<HtmlLinkClickedEventArgs> HtmlLinkClicked;

    public HtmlComparisonView()
	{
	}

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        var liteHtml = (LiteHtml)GetTemplateChild("PART_LiteHtml");
        liteHtml.Command = new Command(url => HtmlLinkClicked?.Invoke(liteHtml, new HtmlLinkClickedEventArgs(url as string)));
    }

    public string Html
    {
        get => (string)GetValue(HtmlProperty);
        set => SetValue(HtmlProperty, value);
    }

    public ILiteHtmlSource LiteHtmlSource
    {
        get => (ILiteHtmlSource)GetValue(LiteHtmlSourceProperty);
        set => SetValue(LiteHtmlSourceProperty, value);
    }

    public HtmlWebViewSource HtmlSource
    {
        get => (HtmlWebViewSource)GetValue(HtmlSourceProperty);
        set => SetValue(HtmlSourceProperty, value);
    }
}

class HtmlResource : IMarkupExtension<string>
{
    public string HtmlFile { get; set; }

    
    public string ProvideValue(IServiceProvider serviceProvider)
    {
        return ResourceLoader.LoadStringFromEmbeddedResource("Html." + HtmlFile);
    }

    object IMarkupExtension.ProvideValue(IServiceProvider serviceProvider)
    {
        return ProvideValue(serviceProvider);
    }
}