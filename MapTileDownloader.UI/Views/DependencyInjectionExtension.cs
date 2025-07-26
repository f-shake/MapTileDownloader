using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace MapTileDownloader.UI.Views;

public class DependencyInjectionExtension : MarkupExtension
{
    public DependencyInjectionExtension()
    {
    }

    public DependencyInjectionExtension(Type type)
    {
        Type = type;
    }

    public Type Type { get; set; }

    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        return App.Services.GetRequiredService(Type);
    }
}