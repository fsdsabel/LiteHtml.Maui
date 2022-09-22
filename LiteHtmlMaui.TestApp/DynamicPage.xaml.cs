using LiteHtmlMaui.Controls;
using System.ComponentModel;
using System.Windows.Input;

namespace LiteHtmlMaui.TestApp;

public partial class DynamicPage : ContentPage
{
	public DynamicPage()
	{
		InitializeComponent();
        BindingContext = new DynamicPageViewModel();        
	}
}

public class DynamicPageViewModel : INotifyPropertyChanged
{
    private ILiteHtmlSource _htmlSource;
	private string _html = "";
	private const string _htmlLine = "This is a line<br/>";

    class MyHtmlSource : ILiteHtmlSource
    {
        public string Html { get; set; }

        public string Css => "body { background:#888; }";

        public Task<Stream> GetStreamForUrlAsync(string url)
        {
            return Task.FromResult<Stream>(null);
        }
    }


    public DynamicPageViewModel()
    {
        OnUpdate();
		UpdateCommand = new Command(OnUpdate);
    }

    public ILiteHtmlSource HtmlSource
    {
		get => _htmlSource;
		set
		{
			if(_htmlSource != value)
            {
				_htmlSource = value;
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(HtmlSource)));
            }
		}
    }

	public ICommand UpdateCommand { get; }
    public void OnUpdate()
    {
		_html += _htmlLine;
        HtmlSource = new MyHtmlSource { Html = _html };
    }

    public event PropertyChangedEventHandler PropertyChanged;
}