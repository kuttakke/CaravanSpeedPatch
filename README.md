# CaravanSpeedPatch

一个 RimWorld（环世界）Mod，使大地图远行队的移动速度随成员实际移速（仿生体、特性、药物等加成）缩放。支持 RimWorld 1.5、1.6、1.7，并可选支持 Vehicle Framework。

## 文件结构

```text
├── About/                  # Mod 元数据与预览图
├── Assemblies/             # 默认编译产物
├── 1.5/ 1.6/ 1.7/          # 对应游戏版本的编译产物
├── Languages/              # 中英文翻译文件
└── Source/CaravanSpeedPatch/
    ├── CaravanSpeedPatch.sln
    ├── CaravanSpeedPatch/  # 1.5 / 1.6 项目 (net472)
    │   ├── Mod.cs          # Mod 设置与界面
    │   └── Patch.cs        # Harmony 补丁与寻路保护
    └── CaravanSpeedPatch17/# 1.7 项目 (net10.0)
```

## 依赖

- **游戏运行**：[Harmony](https://steamcommunity.com/sharedfiles/filedetails/?id=2009463077)（`brrainz.harmony`）
- **源码编译**：.NET 10 SDK（若仅编译 1.5/1.6 可用 .NET 8/9 SDK），NuGet 会自动还原 `Krafs.Rimworld.Ref` 与 `Lib.Harmony`

## 编译

在仓库根目录执行：

```powershell
dotnet build Source/CaravanSpeedPatch/CaravanSpeedPatch.sln -c Release
```

编译产物会自动复制到 `Assemblies/` 以及 `1.5/`、`1.6/`、`1.7/` 对应的 `Assemblies/` 目录。

## 开源协议

[MIT](LICENSE)
