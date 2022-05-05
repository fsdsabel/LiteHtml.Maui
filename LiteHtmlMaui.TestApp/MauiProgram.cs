﻿using LiteHtmlMaui.Hosting;

namespace LiteHtmlMaui.TestApp;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp() 
    {
       // Thread.Sleep(TimeSpan.FromSeconds(10)); 
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureLiteHtml(o => o.UseDefaultMasterStyleSheet())
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        return builder.Build();
    }
}
