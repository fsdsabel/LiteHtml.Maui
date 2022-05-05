using Microsoft.Maui.Controls;

[assembly: XmlnsDefinition("http://schemas.microsoft.com/dotnet/2021/mauilitehtml", "LiteHtmlMaui.Controls")]

#if IOS
[assembly: ObjCRuntime.LinkWith(
        "libgumbo.a",
        ForceLoad = true,
        LinkerFlags = "",
        IsCxx = true,
        Frameworks = ""
    )
]
[assembly: ObjCRuntime.LinkWith(
        "liblitehtml.a",
        ForceLoad = true,
        LinkerFlags = "",
        IsCxx = true,        
        Frameworks = ""
    )
]
[assembly: ObjCRuntime.LinkWith(
        "liblitehtml-maui.a",
        ForceLoad = true,
        LinkerFlags = "",
        IsCxx = true,
        Frameworks = ""
    )
]
#endif