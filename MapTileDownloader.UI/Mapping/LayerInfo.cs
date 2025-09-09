using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using BruTile.Predefined;
using BruTile.Web;
using DynamicData;
using Mapsui;
using Mapsui.Layers;
using Mapsui.Tiling.Layers;

namespace MapTileDownloader.UI.Mapping;

public class LayerInfo : INotifyPropertyChanged
{
    private LayerInfo(string name, LayerCollection layers, BaseLayer layer)
    {
        Name = name;
        Layers = layers ?? throw new ArgumentNullException(nameof(layers));
        Layer = layer;
        Layer.PropertyChanged += Layer_PropertyChanged;
    }

    public event PropertyChangedEventHandler PropertyChanged;

    public IEnumerable<IFeature> Features
    {
        get
        {
            if (Layer is MemoryLayer ml)
            {
                return ml.Features;
            }

            throw new InvalidOperationException("要素操作仅针对MemoryLayer");
        }
        set
        {
            if (Layer is MemoryLayer ml)
            {
                ml.Features = value;
            }
            else
            {
                throw new InvalidOperationException("要素操作仅针对MemoryLayer");
            }
        }
    }

    public bool IsVisible
    {
        get => Layer.Enabled;
        set
        {
            Layer.Enabled = value;
            OnPropertyChanged();
        }
    }

    public BaseLayer Layer { get; private set; }

    public LayerCollection Layers { get; }

    public string Name { get; }

    public static LayerInfo CreateAndInsert(string name, LayerCollection layers, BaseLayer layer)
    {
        var layerInfo = new LayerInfo(name, layers, layer);
        layers.Add(layer);
        return layerInfo;
    }

    public void Replace(BaseLayer newLayer)
    {
        bool isVisible = IsVisible;
        var index = Layers.IndexOf(Layer);
        if (!Layers.Remove(Layer))
        {
            throw new InvalidOperationException("图层集合中找不到该图层");
        }

        Layer.PropertyChanged -= Layer_PropertyChanged;

        Layer = newLayer;
        Layer.PropertyChanged += Layer_PropertyChanged;
        Layers.Insert(index, Layer);
        IsVisible = isVisible;
    }

    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private void Layer_PropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(BaseLayer.Enabled))
        {
            OnPropertyChanged(nameof(IsVisible));
        }
    }
}