﻿using Avalonia.Input;
using Mapsui;
using Mapsui.Extensions;
using Mapsui.Layers;
using Mapsui.Nts;
using Mapsui.UI.Avalonia.Extensions;
using NetTopologySuite.Geometries;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Geometry = NetTopologySuite.Geometries.Geometry;

namespace MapTileDownloader.UI.Mapping;

public partial class MapView
{
    private static Cursor DefaultCursor = Cursor.Default;
    private static Cursor NoneCursor = new Cursor(StandardCursorType.None);
    private bool isDrawing = false;
    private Avalonia.Point mouseDownPoint;
    private TaskCompletionSource<Coordinate[]> tcs;
    private List<MPoint> vertices = new List<MPoint>();
    public void DisplayPolygon(Coordinate[] coordinates)
    {
        if (coordinates == null || coordinates.Length < 3)
        {
           drawingLayer.Features = [];
            Refresh();
            return;
        }

        var points = coordinates.Select(c => new MPoint(c.X, c.Y)).ToList();
        var polygon = new Polygon(points.ToClosedLinearRing());
        var feature = new GeometryFeature(polygon);
        drawingLayer.Features = [feature];
        ZoomToGeometry(polygon);
        Refresh();
    }

    public Task<Coordinate[]> DrawAsync(CancellationToken cancellationToken = default)
    {
        if (cancellationToken != default)
        {
            cancellationToken.Register(CancelDrawing);
        }

        tcs = new TaskCompletionSource<Coordinate[]>();
        StartDrawing();
        return tcs.Task;
    }

    public Coordinate[] FinishDrawing()
    {
        if (vertices.Count < 3)
        {
            CancelDrawing();
            return null;
        }

        EndDrawing();
        Refresh();
        Debug.Assert(drawingLayer.Features.Count() == 1);
        var feature = drawingLayer.Features.Single();
        Debug.Assert(feature is GeometryFeature);
        var geometry = (feature as GeometryFeature).Geometry;
        Debug.Assert(geometry is Polygon);
        var polygon = geometry as Polygon;
        var results = polygon.Shell.Coordinates[..^1].ToArray();
        Debug.Assert(results is { Length: >= 3 });
        if (tcs != null)
        {
            tcs.SetResult(results);
            tcs = null;
        }

        return results;
    }

    private void CancelDrawing()
    {
        EndDrawing();
        drawingLayer.Features = [];
        Refresh();
        if (tcs != null)
        {
            tcs.SetException(new OperationCanceledException("取消了绘制"));
            tcs = null;
        }
    }

    private void EndDrawing()
    {
        isDrawing = false;
        vertices.Clear();
        mousePositionLayer.Features = [];
    }

    private void InitializeDrawing()
    {
        // 绑定鼠标事件
        PointerPressed += OnPointerPressed;
        PointerMoved += OnPointerMoved;
        PointerReleased += OnPointerReleased;
    }

    private void OnPointerMoved(object sender, PointerEventArgs e)
    {
        Cursor = isDrawing ? NoneCursor : DefaultCursor;
        if (!isDrawing)
        {
            return;
        }

        var screenPosition = e.GetPosition(this).ToScreenPosition();
        var worldPosition = Map.Navigator.Viewport.ScreenToWorld(screenPosition);

        if (vertices.Count > 0)
        {
            // 实时更新最后一个点（跟随鼠标）
            vertices[^1] = worldPosition;
            UpdateDrawing();
        }

        mousePositionLayer.Features = [new PointFeature(worldPosition)];
        Refresh();
    }

    private void OnPointerPressed(object sender, PointerPressedEventArgs e)
    {
        if (!isDrawing)
        {
            return;
        }

        mouseDownPoint = e.GetPosition(this);
        var properties = e.GetCurrentPoint(this).Properties;
        if (properties.PointerUpdateKind == PointerUpdateKind.RightButtonPressed)
        {
            Withdraw();
        }
        else if (properties.PointerUpdateKind == PointerUpdateKind.LeftButtonPressed)
        {
            if (e.ClickCount == 2)
            {
                //最后一个点是双击的第一次添加的，不需要
                vertices.RemoveAt(vertices.Count - 1);
                UpdateDrawing();
                FinishDrawing();
                return;
            }

            var screenPosition = e.GetPosition(this).ToScreenPosition();
            var worldPosition = Map.Navigator.Viewport.ScreenToWorld(screenPosition);

            vertices.Add(worldPosition);
            if (vertices.Count == 1)
            {
                vertices.Add(worldPosition);
            }

            UpdateDrawing();
        }
    }

    private void OnPointerReleased(object sender, PointerReleasedEventArgs e)
    {
        if (!isDrawing)
        {
            return;
        }

        //拖动地图，不进行落点
        if (e.InitialPressMouseButton == MouseButton.Left)
        {
            var point = e.GetPosition(this);
            if (Math.Sqrt(Math.Pow(point.X - mouseDownPoint.X, 2) + Math.Pow(point.Y - mouseDownPoint.Y, 2)) > 10)
            {
                Withdraw();
            }
        }
    }

    private void StartDrawing()
    {
        vertices.Clear();
        isDrawing = true;
    }
    private void UpdateDrawing()
    {
        if (vertices.Count < 2)
        {
            drawingLayer.Features = [];
        }
        else
        {
            Geometry geometry = null;
            if (vertices.Count < 3)
            {
                geometry = new LineString([.. vertices.Select(p => new Coordinate(p.X, p.Y))]);
            }
            else
            {
                geometry = new Polygon(vertices.ToClosedLinearRing());
            }

            drawingLayer.Features = [new GeometryFeature(geometry)];
        }

        Refresh();
    }

    private void Withdraw()
    {
        if (vertices.Count > 2)
        {
            vertices.RemoveAt(vertices.Count - 1);
        }
        else
        {
            vertices.Clear();
        }

        UpdateDrawing();
    }
}