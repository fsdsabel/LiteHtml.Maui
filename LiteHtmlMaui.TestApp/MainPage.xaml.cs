using System.Windows.Input;

namespace LiteHtmlMaui.TestApp;

public partial class MainPage : ContentPage
{
	int count = 0;

	public MainPage()
	{
		InitializeComponent();
        BindingContext = new MainPageViewModel();
    }

}

public class MainPageViewModel 
{
    public MainPageViewModel()
    {
        HyperlinkClickedCommand = new Command(OnHyperlinkClicked);
        
    }

    private void OnHyperlinkClicked(object obj)
    {
        App.Current.MainPage.DisplayAlert("Hyperlink clicked", obj?.ToString() ?? "", "OK");
    }

    public ICommand HyperlinkClickedCommand { get; }
}

