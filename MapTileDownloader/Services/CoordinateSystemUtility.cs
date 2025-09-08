using ProjNet.CoordinateSystems;
using ProjNet.CoordinateSystems.Transformations;

namespace MapTileDownloader.Services;

public static class CoordinateSystemUtility
{
    private static readonly CoordinateSystem webMercator = ProjectedCoordinateSystem.WebMercator;

    private static readonly CoordinateSystem wgs84 = GeographicCoordinateSystem.WGS84;

    public static readonly ICoordinateTransformation WebMercatorToWgs84 =
        new CoordinateTransformationFactory().CreateFromCoordinateSystems(webMercator, wgs84);


    public static readonly ICoordinateTransformation Wgs84ToWebMercator =
        new CoordinateTransformationFactory().CreateFromCoordinateSystems(wgs84, webMercator);
}