using LiteHtmlMaui.Controls;
using System.Windows.Input;

namespace LiteHtmlMaui.TestApp;

public partial class TextPage : ContentPage
{
	public TextPage()
	{
		InitializeComponent();
	}

    private async void HtmlComparisonView_HtmlLinkClicked(object sender, HtmlLinkClickedEventArgs e)
    {
		switch(e.Url)
        {
            case "#top":
                await FindScrollView(sender as LiteHtml)?.ScrollToAsync(0, 0, true);
                break;
            default:
                await App.Current.MainPage.DisplayAlert("Link clicked", e.Url, "OK");
                break;
        }
    }

    private ScrollView? FindScrollView(Element? view)
    {
        if (view is ScrollView || view == null) return view as ScrollView;
        return FindScrollView(view.Parent);
    }
}