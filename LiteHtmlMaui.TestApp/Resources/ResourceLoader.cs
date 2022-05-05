namespace LiteHtmlMaui.TestApp.Resources;

static class ResourceLoader
{
    public static string LoadStringFromEmbeddedResource(string name, System.Text.Encoding encoding = null)
    {
        try
        {
            using var stream = typeof(ResourceLoader).Assembly.GetManifestResourceStream(typeof(ResourceLoader), name);
            using var reader = new StreamReader(stream, encoding ?? System.Text.Encoding.UTF8);
            return reader.ReadToEnd();
        }
        catch
        {
            return null;
        }
    }
}