using NetTopologySuite.Geometries;
using ProjNet.CoordinateSystems;
using ProjNet.CoordinateSystems.Transformations;

namespace MapTileDownloader.Services;

//下面的代码，在AOT时报错

// 2025-09-09 16:13:12.760 +08:00 [ERR] 计算MBTiles bounds失败
// System.TypeInitializationException: A type initializer threw an exception. To determine which type, inspect the InnerException's StackTrace property.
//  ---> System.TypeInitializationException: A type initializer threw an exception. To determine which type, inspect the InnerException's StackTrace property.
//  ---> System.ArgumentException: The provided type is lacking a suitable constructor (Parameter 'type')
//    at ProjNet.CoordinateSystems.Projections.ProjectionsRegistry.Register(String, Type) + 0x236
//    at ProjNet.CoordinateSystems.Projections.ProjectionsRegistry..cctor() + 0xa5
//    at System.Runtime.CompilerServices.ClassConstructorRunner.EnsureClassConstructorRun(StaticClassConstructionContext*) + 0xb9
//    --- End of inner exception stack trace ---
//    at System.Runtime.CompilerServices.ClassConstructorRunner.EnsureClassConstructorRun(StaticClassConstructionContext*) + 0x14a
//    at System.Runtime.CompilerServices.ClassConstructorRunner.CheckStaticClassConstructionReturnGCStaticBase(StaticClassConstructionContext*, Object) + 0xd
//    at ProjNet.CoordinateSystems.Projections.ProjectionsRegistry.CreateProjection(String, IEnumerable`1) + 0x1ae
//    at ProjNet.CoordinateSystems.Transformations.CoordinateTransformationFactory.Proj2Geog(ProjectedCoordinateSystem, GeographicCoordinateSystem) + 0x3f
//    at ProjNet.CoordinateSystems.Transformations.CoordinateTransformationFactory.CreateFromCoordinateSystems(CoordinateSystem, CoordinateSystem) + 0x5f
//    at MapTileDownloader.Services.CoordinateSystemUtility..cctor() + 0x87
//    at System.Runtime.CompilerServices.ClassConstructorRunner.EnsureClassConstructorRun(StaticClassConstructionContext*) + 0xb9
//    --- End of inner exception stack trace ---
//    at System.Runtime.CompilerServices.ClassConstructorRunner.EnsureClassConstructorRun(StaticClassConstructionContext*) + 0x14a
//    at System.Runtime.CompilerServices.ClassConstructorRunner.CheckStaticClassConstructionReturnGCStaticBase(StaticClassConstructionContext*, Object) + 0xd
//    at MapTileDownloader.Services.TileIntersectionService.GetWorldExtent(Int32, Int32, Int32, Int32, Int32) + 0x130
//    at MapTileDownloader.Services.MbtilesService.<GetMbtilesInfoAsyncInternal>d__24.MoveNext() + 0x8a0

// public static class CoordinateSystemUtility
// {
//     private static readonly CoordinateSystem webMercator = ProjectedCoordinateSystem.WebMercator;
//
//     private static readonly CoordinateSystem wgs84 = GeographicCoordinateSystem.WGS84;
//
//     public static readonly ICoordinateTransformation WebMercatorToWgs84 =
//         new CoordinateTransformationFactory().CreateFromCoordinateSystems(webMercator, wgs84);
//
//
//     public static readonly ICoordinateTransformation Wgs84ToWebMercator =
//         new CoordinateTransformationFactory().CreateFromCoordinateSystems(wgs84, webMercator);
// }

public static class CoordinateSystemUtility
{
    private const double EarthRadius = 6378137.0;
    private const double OriginShift = 2 * Math.PI * EarthRadius / 2.0;

    /// <summary>
    /// 将 WGS84 经纬度 (度) 转换为 WebMercator (米)
    /// </summary>
    public static (double x, double y) Wgs84ToWebMercator(double lon, double lat)
    {
        double x = lon * OriginShift / 180.0;
        double y = Math.Log(Math.Tan((90 + lat) * Math.PI / 360.0)) * EarthRadius;
        return (x, y);
    }

    /// <summary>
    /// 将 WebMercator (米) 转换为 WGS84 经纬度 (度)
    /// </summary>
    public static (double lon, double lat) WebMercatorToWgs84(double x, double y)
    {
        double lon = (x / OriginShift) * 180.0;
        double lat = (y / EarthRadius);
        lat = 180.0 / Math.PI * (2 * Math.Atan(Math.Exp(lat)) - Math.PI / 2.0);
        return (lon, lat);
    }

    /// <summary>
    /// 批量转换：WGS84 → WebMercator
    /// </summary>
    public static (double x, double y)[] Wgs84ToWebMercator((double lon, double lat)[] coords)
        => coords.Select(c => Wgs84ToWebMercator(c.lon, c.lat)).ToArray();

    /// <summary>
    /// 批量转换：WebMercator → WGS84
    /// </summary>
    public static (double lon, double lat)[] WebMercatorToWgs84((double x, double y)[] coords)
        => coords.Select(c => WebMercatorToWgs84(c.x, c.y)).ToArray();
}