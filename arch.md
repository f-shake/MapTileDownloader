# 架构文档 (Architecture Documentation)

## 1. 项目概述
本项目 `MapTileDownloader` 是一个地图瓦片下载工具，支持从多种数据源下载瓦片，并提供合并、转换等功能。

## 2. 架构设计

### 2.1 模块划分

#### 2.1.1 核心模块 (`MapTileDownloader`)
- **服务层 (Services)**
  - `MbtilesService`: 管理 MBTiles 数据库的读写操作。
  - `TileDownloadService`: 负责从远程服务器下载地图瓦片。
  - `TileMergeService`: 提供瓦片合并功能。
  - `TileConvertService`: 支持瓦片格式转换。
  - `TileIntersectionService`: 处理瓦片的地理空间计算。
- **模型层 (Models)**
  - `IDownloadingTile`: 定义瓦片下载的接口。
  - `MbtilesInfo`: 描述 MBTiles 数据库的元信息。
- **工具类 (Utilities)**
  - `CoordinateSystemUtility`: 提供坐标系转换功能。
  - `ImageUtility`: 处理图像相关的操作。

#### 2.1.2 用户界面模块 (`MapTileDownloader.UI`)
- **视图模型 (ViewModels)**
  - `DownloadViewModel`: 管理下载任务的逻辑。
  - `MainViewModel`: 主界面的业务逻辑。
  - `MapAreaSelectorViewModel`: 处理地图区域选择逻辑。
- **视图层 (Views)**
  - `MainView`: 主界面布局。
  - `DownloadPanel`: 下载任务面板。
- **地图服务 (Mapping)**
  - `MapService`: 提供地图渲染和交互功能。
  - `MapView`: 地图显示组件。

### 2.2 数据流
1. 用户通过 UI 发起下载请求。
2. `DownloadViewModel` 调用 `TileDownloadService` 下载瓦片。
3. 下载完成后，`TileMergeService` 或 `TileConvertService` 处理瓦片。
4. 结果存储到 MBTiles 数据库或显示在 `MapView` 中。

## 3. 技术栈
- **核心模块**: .NET 8.0, C#
- **用户界面**: Avalonia UI (跨平台 UI 框架)
- **地图渲染**: Mapsui (开源地图控件)

## 4. 后续优化建议
- 增加对更多瓦片数据源的支持。
- 优化瓦片下载的并发性能。
- 提供更灵活的地图显示配置。