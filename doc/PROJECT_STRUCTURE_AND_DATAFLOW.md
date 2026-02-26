# MapleItemDB 项目结构与核心数据流

## 架构总览

分层架构：**Presentation (UI / CLI)** → **Application** → **Domain (Core)** → **Infrastructure**

```
┌─────────────┐  ┌──────────────┐
│ MapleItemDB │  │ MapleItemDB  │   Presentation
│     .UI     │  │    .Cli      │   (仅依赖 Application 用例)
└──────┬──────┘  └──────┬───────┘
       │                │
       └───────┬────────┘
               ▼
       ┌───────────────┐
       │  MapleItemDB  │               Application
       │ .Application  │               (用例编排 + 格式化)
       └───────┬───────┘
               │
       ┌───────┴───────┐
       ▼               ▼
┌────────────┐  ┌──────────────────┐
│ MapleItemDB│  │   MapleItemDB    │   Domain / Infrastructure
│   .Core    │  │ .Infrastructure  │
└────────────┘  └──────────────────┘
                        │
               ┌────────┴────────┐
               ▼                 ▼
       ┌──────────────┐  ┌──────────────┐
       │  MapleItemDB │  │  MapleItemDB │  Extraction / DI
       │ .WzExtraction│  │  .Bootstrap  │
       └──────────────┘  └──────────────┘
```

## 项目结构

```
maplestory_toolbox/
├── src/
│   ├── MapleItemDB.Core/                    # 领域模型与接口定义
│   │   ├── Interfaces/
│   │   │   ├── IItemReadRepository.cs       # 道具读仓储接口
│   │   │   ├── IItemWriteRepository.cs      # 道具写仓储接口
│   │   │   ├── ISetItemReadRepository.cs    # 套装读仓储接口
│   │   │   ├── ISetItemWriteRepository.cs   # 套装写仓储接口
│   │   │   ├── ISkillReadRepository.cs      # 技能读仓储接口
│   │   │   ├── ISkillWriteRepository.cs     # 技能写仓储接口
│   │   │   ├── ISearchIndex.cs              # 内存搜索索引接口
│   │   │   ├── IWzExtractor.cs              # WZ 提取器接口 (+ExtractionResult, ExtractionProgress)
│   │   │   ├── IStringResolver.cs           # 字符串解析接口
│   │   │   └── ICacheManager.cs             # 缓存管理接口
│   │   ├── Models/
│   │   │   ├── ItemEntity.cs                # 道具主实体
│   │   │   ├── SkillEntity.cs               # 技能实体
│   │   │   ├── SetItemInfo.cs               # 套装信息 (+SetItemPart, SetItemEffect, SetItemActiveSkill)
│   │   │   ├── ItemCategory.cs              # 道具分类枚举
│   │   │   ├── ItemQueryFilter.cs           # 查询筛选条件
│   │   │   └── StatsLine.cs                 # 属性行 (UI 展示用)
│   │   └── Data/
│   │       └── SnMap.cs                     # SN 映射表 (嵌入资源)
│   │
│   ├── MapleItemDB.Application/             # 用例编排层
│   │   ├── Contracts/                       # DTO / 请求-响应模型
│   │   │   ├── SearchContracts.cs           # SearchRequest, SearchResult
│   │   │   ├── ExtractImportContracts.cs    # ExtractImportRequest, ExtractImportResult
│   │   │   ├── ItemDetailResult.cs          # 道具详情结果
│   │   │   ├── ItemLookupResult.cs          # 道具查找结果
│   │   │   ├── DatabaseStatsResult.cs       # 数据库统计结果
│   │   │   └── ProgressReport.cs            # 统一进度模型
│   │   └── UseCases/                        # 用例接口与实现
│   │       ├── IExtractAndImportUseCase.cs  # 提取+导入+索引刷新
│   │       ├── ExtractAndImportUseCase.cs
│   │       ├── IInitializeCatalogUseCase.cs # 初始化搜索索引+套装缓存
│   │       ├── InitializeCatalogUseCase.cs
│   │       ├── ISearchItemsUseCase.cs       # 道具搜索 (含技能转道具)
│   │       ├── SearchItemsUseCase.cs
│   │       ├── ISearchSkillsUseCase.cs      # 技能搜索
│   │       ├── SearchSkillsUseCase.cs
│   │       ├── IGetItemByIdUseCase.cs       # 按 ID 查询道具+套装
│   │       ├── GetItemByIdUseCase.cs
│   │       ├── IGetItemDetailUseCase.cs     # 道具详情格式化
│   │       ├── GetItemDetailUseCase.cs
│   │       ├── IGetStatsUseCase.cs          # 数据库统计
│   │       ├── GetStatsUseCase.cs
│   │       └── ItemDetailFormattingHelper.cs # 属性格式化 (StatsLine 生成)
│   │
│   ├── MapleItemDB.Infrastructure/          # 基础设施实现层
│   │   ├── Database/
│   │   │   ├── DatabaseBootstrapper.cs      # 建库 + WAL + 调用 MigrationRunner
│   │   │   ├── SqliteConnectionFactory.cs   # 连接工厂
│   │   │   └── Migrations/
│   │   │       ├── IDbMigration.cs          # 迁移接口 (Version/Description/Execute)
│   │   │       ├── MigrationRunner.cs       # 迁移执行器 (_migrations 表管理)
│   │   │       └── AllMigrations.cs         # Migration001~007 实现
│   │   ├── Repositories/
│   │   │   ├── Read/
│   │   │   │   ├── ItemReadRepository.cs    # 道具查询 (Dapper)
│   │   │   │   ├── SetItemReadRepository.cs # 套装查询
│   │   │   │   └── SkillReadRepository.cs   # 技能查询
│   │   │   ├── Write/
│   │   │   │   ├── ItemWriteRepository.cs   # 道具批量写入
│   │   │   │   ├── SetItemWriteRepository.cs# 套装批量写入
│   │   │   │   └── SkillWriteRepository.cs  # 技能批量写入
│   │   │   └── Shared/
│   │   │       ├── RepositoryHelper.cs      # PRAGMA 优化 + Chunk 分批
│   │   │       ├── BlobAssetHelper.cs       # BLOB 哈希去重与ID映射
│   │   │       ├── ItemRow.cs               # 道具 ORM 行模型
│   │   │       └── SkillRow.cs              # 技能 ORM 行模型
│   │   └── Cache/
│   │       └── InMemorySearchIndex.cs       # 内存搜索索引实现
│   │
│   ├── MapleItemDB.Bootstrap/               # DI 组合根
│   │   └── ServiceCollectionExtensions.cs   # AddMapleItemDb() 扩展方法
│   │
│   ├── MapleItemDB.WzExtraction/            # WZ 数据提取层
│   │   ├── Services/
│   │   │   ├── WzExtractionService.cs       # 提取管道主服务
│   │   │   ├── EquipExtractor.cs            # 装备提取器
│   │   │   ├── GeneralItemExtractor.cs      # 通用道具提取器
│   │   │   ├── SetItemExtractor.cs          # 套装信息提取器
│   │   │   ├── SkillExtractor.cs            # 技能提取器
│   │   │   ├── StringPoolBuilder.cs         # 字符串池构建器
│   │   │   ├── MacroResolver.cs             # 宏变量解析器
│   │   │   └── IconExporter.cs              # 图标导出器
│   │   └── Helpers/
│   │       └── WzNodeExtensions.cs          # WZ 节点扩展方法
│   │
│   ├── MapleItemDB.Cli/                     # 命令行工具 (mapleidb)
│   │   └── Program.cs                       # CLI 入口 (通过用例执行)
│   │
│   └── MapleItemDB.UI/                      # WPF 客户端
│       ├── App.xaml.cs                      # 应用入口 + DI + DatabaseBootstrapper
│       ├── ServiceRegistration.cs           # UI DI 配置
│       ├── ViewModels/
│       │   ├── MainViewModel.cs             # 主窗口 ViewModel (UI 状态管理)
│       │   └── SkillSummaryParser.cs        # 技能描述解析器
│       ├── MainWindow.xaml(.cs)             # 主窗口
│       └── Converters/
│           └── Converters.cs                # 值转换器
│
├── tests/WzLoadTest/                        # 手动验证 WZ 提取流程
├── lib/WzComparerR2/                        # WZ 解析依赖库
└── data/                                    # 数据资源
    ├── sn_map.json                          # SN 映射表
    ├── sn.txt                               # SN 原始数据
    └── convert_sn.ps1                       # SN 转换脚本
```

## 依赖关系

| 工程 | 依赖 | 说明 |
|------|------|------|
| Core | (无) | 纯领域模型与接口，零外部依赖 |
| Application | Core | 用例编排，依赖 Core 接口 |
| Infrastructure | Core | 实现 Core 定义的仓储/索引接口 |
| WzExtraction | Core | 实现 `IWzExtractor`，依赖 WzComparerR2 |
| Bootstrap | Application, Infrastructure, WzExtraction | DI 组合根，注册全部服务 |
| UI | Bootstrap, Application, Core | 仅依赖 Application 用例接口 + `ISearchIndex` |
| Cli | Bootstrap, Application, Core | 仅依赖 Application 用例接口 |

## DI 注册概览

`Bootstrap.ServiceCollectionExtensions.AddMapleItemDb()` 统一注册所有服务：

| 类别 | 生命周期 | 注册 |
|------|---------|------|
| 连接工厂 | Singleton | `SqliteConnectionFactory` |
| 数据库迁移 | Singleton | `IDbMigration` × 7, `MigrationRunner`, `DatabaseBootstrapper` |
| 读仓储 | Singleton | `IItemReadRepository`, `ISetItemReadRepository`, `ISkillReadRepository` |
| 写仓储 | Singleton | `IItemWriteRepository`, `ISetItemWriteRepository`, `ISkillWriteRepository` |
| 搜索索引 | Singleton | `ISearchIndex` → `InMemorySearchIndex` |
| WZ 提取器 | Transient | `IWzExtractor` → `WzExtractionService` |
| 用例 | Transient | 7 个用例接口 → 实现类 |

UI 层额外注册 `MainViewModel`（Singleton）和 `MainWindow`（Transient）。

## 领域模型

### ItemEntity (dim_items)

| 属性 | 类型 | 说明 |
|------|------|------|
| ItemId | int | 道具 ID (主键) |
| Name | string | 道具名称 (已解析宏变量) |
| Description | string? | 道具描述 |
| Category | ItemCategory | 分类: Equip/Consume/Etc/Setup/Cash/Pet/Skill |
| SubCategory | string? | 子分类: Weapon/Cap/Coat/SecondWeapon 等 |
| ReqLevel/Str/Dex/Int/Luk | int? | 装备需求属性 |
| ReqJob | int? | 职业需求 (位掩码: 1=战士, 2=魔法师, 4=弓箭手, 8=飞侠, 16=海盗) |
| IncSTR/DEX/INT/LUK | int? | 属性加成 |
| IncPAD/MAD/PDD/MDD | int? | 攻防加成 |
| IncMHP/MMP | int? | HP/MP 加成 |
| DynamicStats | string? | 动态属性 JSON (boss_dmg, ied, total_dmg, attack_speed, 标志位等) |
| ConsumeSpec | string? | 消耗品效果 JSON (hp/mp/buff 等) |
| IsCash | bool | 是否商城道具 |
| Price | int? | NPC 售价 |
| IconData | byte[]? | 图标 PNG 二进制 |
| PreviewData | byte[]? | 预览图 PNG 二进制 |
| SetItemId | int? | 套装 ID |
| Sn | int? | 商城 SN 编号 |
| TimeLimited | bool | 是否限时道具 |
| ExtractedAt | DateTime | 提取时间戳 |

### SkillEntity (dim_skills)

| 属性 | 类型 | 说明 |
|------|------|------|
| SkillId | int | 技能 ID (主键) |
| Name | string | 技能名称 |
| Description | string? | 技能描述 |
| JobId | int | 所属职业 ID |
| MaxLevel | int | 最大等级 |
| IconData | byte[]? | 图标 PNG 二进制 |
| IsHidden | bool | 是否隐藏技能 |
| LevelEffectsJson | string? | 各等级效果 JSON |
| SkillH | string? | 等级效果描述模板 (来自 String.wz 的 h 字段) |
| CommonPropsJson | string? | common 属性公式字典 JSON |
| ExtractedAt | DateTime | 提取时间戳 |

### SetItemInfo (dim_setitems)

整条套装序列化为 JSON 存储于 `data_json` 列，包含 `SetItemId`、`SetItemName`、`CompleteCount`、`Parts[]`（部位信息）、`Effects[]`（套装效果，含 `RequiredCount`、`Props{}`、`ActiveSkills[]`）。

### ItemQueryFilter

| 属性 | 类型 | 说明 |
|------|------|------|
| Keyword | string? | 模糊搜索 (名称/描述/ID) |
| Category | ItemCategory? | 分类筛选 |
| SubCategory | string? | 子分类筛选 |
| MinLevel/MaxLevel | int? | 等级范围 |
| IsCash | bool? | 商城道具筛选 |
| HasSn | bool? | 是否存在 SN |
| MinBossDmg | int? | 最小 Boss 伤害 (JSON_EXTRACT) |
| MinIed | int? | 最小无视防御 (JSON_EXTRACT) |
| Limit / Offset | int | 分页参数 (Limit=0 不限制) |

## Application 用例接口

| 用例 | 职责 | 依赖 |
|------|------|------|
| `IExtractAndImportUseCase` | WZ 提取 → 写库 → 索引刷新 | IWzExtractor + 6 个写/读仓储 |
| `IInitializeCatalogUseCase` | 启动时预加载 ID-Name 索引与套装缓存 | IItemReadRepository + ISetItemReadRepository |
| `ISearchItemsUseCase` | 组合条件搜索道具（含技能转道具） | IItemReadRepository + ISkillReadRepository |
| `ISearchSkillsUseCase` | 技能名称搜索 | ISkillReadRepository |
| `IGetItemByIdUseCase` | 按 ID 查询道具 + 关联套装 | IItemReadRepository + ISetItemReadRepository |
| `IGetItemDetailUseCase` | 格式化道具详情（属性行 + 套装效果 + 技能描述） | 纯格式化，无仓储依赖 |
| `IGetStatsUseCase` | 数据库统计信息 | 3 个读仓储 |

## 核心数据流

### 1. 启动与初始化

```
App.xaml.cs
  │ 1. ServiceRegistration.Configure() → AddMapleItemDb() 注册所有服务
  │ 2. DatabaseBootstrapper.EnsureCreatedAsync()
  │    ├── 创建基础 Schema (dim_blob_assets, dim_items, dim_setitems, dim_skills + 索引)
  │    ├── 启用 WAL 模式
  │    └── MigrationRunner.RunAsync() — 按编号执行未执行的迁移
  │ 3. MainWindow.Show()
  │ 4. MainViewModel.InitializeAsync()
  │    └── IInitializeCatalogUseCase.ExecuteAsync()
  │        ├── IItemReadRepository.GetIdNameIndexAsync() → ISearchIndex.Load()
  │        └── ISetItemReadRepository.GetAllSetItemsAsync() → _setItems 缓存
  ▼
就绪
```

### 2. WZ 数据提取与导入

```
UI: ExtractDataCommand / CLI: extract
  └── IExtractAndImportUseCase.ExecuteAsync(ExtractImportRequest)
      │
      ├── 1. IWzExtractor.ExtractAllAsync(gameDirectory, progress)
      │      ├── 检测 WZ 格式 (传统/KMST1125)
      │      ├── 加载 WZ 文件 + StringPoolBuilder
      │      ├── EquipExtractor — 装备提取
      │      ├── GeneralItemExtractor — 通用道具提取
      │      ├── SetItemExtractor — 套装提取
      │      ├── SkillExtractor — 技能提取
      │      ├── IconExporter — 图标/预览图导出
      │      └── SnMap — 填充商城 SN 编号
      │
      ├── 2. IItemWriteRepository.BulkUpsertAsync() — 道具写入 (1000行/批)
      ├── 3. ISetItemWriteRepository.BulkUpsertSetItemsAsync() — 套装写入
      ├── 4. ISkillWriteRepository.BulkUpsertSkillsAsync() — 技能写入 (500行/批)
      │
      └── 5. 返回 ExtractImportResult (Bundle + SearchIndex + SetItems)
           └── UI/CLI 刷新内存索引与套装缓存
```

写入优化策略：
- **分批事务**：每批独立 `BEGIN/COMMIT`，避免长时间持锁
- **PRAGMA 优化**：写入期间 `synchronous=NORMAL` / `cache_size=-64000` / `temp_store=MEMORY`，完成后恢复
- **WAL 模式**：`DatabaseBootstrapper` 中启用，持久化设置

### 3. 提取进度映射

| 阶段 | 进度区间 | 说明 |
|------|---------|------|
| 加载 WZ 文件 | 0% ~ 5% | WZ 文件 I/O |
| 构建字符串池 | 5% ~ 10% | String.wz 解析 |
| 提取装备 | 10% ~ 25% | Character 节点遍历 |
| 提取道具 | 25% ~ 40% | Item 节点遍历 |
| 提取套装 | 40% ~ 45% | SetItemInfo 解析 |
| 提取技能 | 45% ~ 55% | Skill 节点遍历 |
| 导出图标 | 55% ~ 90% | PNG 编码 (耗时最长) |
| 写入道具 | 90% ~ 95% | dim_items 分批写入 |
| 写入技能 | 95% ~ 97% | dim_skills 分批写入 |
| 刷新索引 | 97% ~ 100% | 内存索引 + 套装缓存重载 |

### 4. 搜索与展示

```
UI: SearchCommand / CLI: search
  └── ISearchItemsUseCase.ExecuteAsync(SearchRequest)
      ├── SearchRequest → ItemQueryFilter 映射
      ├── IItemReadRepository.QueryAsync(filter)
      │   └── SQL: SELECT ... WHERE 条件组合 (支持 JSON_EXTRACT)
      ├── (如无分类限制) ISkillReadRepository.SearchSkillsByNameAsync()
      │   └── 技能转为 ItemEntity 复用 DataGrid 展示
      └── 返回 SearchResult { Items, SkillsById, IsSkillMode }

UI 自动补全:
  SearchText 变化 → ISearchIndex.Search(keyword, 20) → 毫秒级匹配
```

### 5. 详情展示

```
SelectedItem 变化
  └── IGetItemDetailUseCase.ExecuteAsync(selectedItem, skillsById, setItems)
      ├── ItemDetailFormattingHelper.FormatStats() → List<StatsLine>
      │   格式化顺序:
      │   1. 特殊标志 (橙色): 唯一/不可交易/账号共享/限时/星之力/潜能限制
      │   2. 职业限制: 解析 ReqJob 位掩码
      │   3. 武器分类: 细分类型 (如 "分类 : 双手斧")
      │   4. 需求等级
      │   5. 升级次数 (DynamicStats)
      │   6. 核心属性: STR/DEX/INT/LUK/HP/MP/PAD/MAD/PDD/MDD
      │   7. 动态属性: Boss伤害/无视防御/总伤害/全属性/移速/跳跃/击退
      │   8. 攻击速度 (等级→名称映射)
      │   9. 消耗品效果
      │
      ├── 套装效果文本 (如有 SetItemId)
      ├── 技能详情文本 (如为技能模式)
      └── 返回 ItemDetailResult { FormattedStats, SetItemDisplayText, SkillDetailText, ... }
```

### 6. 缓存策略

| 缓存 | 位置 | 说明 |
|------|------|------|
| `ISearchIndex` | InMemorySearchIndex (Singleton) | `item_id → name` 全量索引，毫秒级搜索提示 |
| `_setItems` | MainViewModel | 套装信息缓存，避免重复查询 |
| `_skillSearchCache` | MainViewModel | 当次搜索的技能缓存，详情切换时复用 |

## 数据库 Schema

### dim_blob_assets

```sql
CREATE TABLE dim_blob_assets (
    blob_id         INTEGER PRIMARY KEY,
    content_hash    BLOB NOT NULL,            -- SHA-256 二进制(32字节)
    content_length  INTEGER NOT NULL,
    blob_data       BLOB NOT NULL,            -- PNG 二进制
    created_at      TEXT NOT NULL
);
CREATE UNIQUE INDEX idx_blob_hash_len ON dim_blob_assets(content_hash, content_length);
```

### dim_items

```sql
CREATE TABLE dim_items (
    item_id          INTEGER PRIMARY KEY,
    name             TEXT NOT NULL,
    description      TEXT,
    category         TEXT NOT NULL,        -- Equip/Consume/Etc/Setup/Cash/Pet
    sub_category     TEXT,                 -- Weapon/Cap/Coat/SecondWeapon 等
    req_level        INTEGER,
    req_str          INTEGER,
    req_dex          INTEGER,
    req_int          INTEGER,
    req_luk          INTEGER,
    req_job          INTEGER,              -- 职业需求位掩码
    inc_str          INTEGER,
    inc_dex          INTEGER,
    inc_int          INTEGER,
    inc_luk          INTEGER,
    inc_pad          INTEGER,
    inc_mad          INTEGER,
    inc_pdd          INTEGER,
    inc_mdd          INTEGER,
    inc_mhp          INTEGER,
    inc_mmp          INTEGER,
    dynamic_stats    TEXT,                 -- JSON: boss_dmg, ied, total_dmg, 标志位等
    consume_spec     TEXT,                 -- JSON: 消耗品效果
    is_cash          INTEGER DEFAULT 0,
    price            INTEGER,
    icon_blob_id     INTEGER,              -- 外键 -> dim_blob_assets.blob_id
    preview_blob_id  INTEGER,              -- 外键 -> dim_blob_assets.blob_id
    setitem_id       INTEGER,
    sn               INTEGER,
    time_limited     INTEGER DEFAULT 0,
    extracted_at     TEXT NOT NULL
);
-- 索引: idx_items_name, idx_items_category, idx_items_cash,
--      idx_items_icon_blob_id, idx_items_preview_blob_id
```

### dim_setitems

```sql
CREATE TABLE dim_setitems (
    setitem_id    INTEGER PRIMARY KEY,
    name          TEXT NOT NULL,
    data_json     TEXT NOT NULL          -- 完整套装 JSON 序列化
);
```

### dim_skills

```sql
CREATE TABLE dim_skills (
    skill_id       INTEGER PRIMARY KEY,
    name           TEXT NOT NULL,
    description    TEXT,
    job_id         INTEGER NOT NULL,
    max_level      INTEGER DEFAULT 0,
    icon_blob_id   INTEGER,               -- 外键 -> dim_blob_assets.blob_id
    is_hidden      INTEGER DEFAULT 0,
    level_effects  TEXT,                  -- JSON: 各等级效果
    skill_h        TEXT,                  -- 等级效果描述模板
    common_props   TEXT,                  -- JSON: common 属性公式字典
    extracted_at   TEXT NOT NULL
);
-- 索引: idx_skills_name, idx_skills_job, idx_skills_icon_blob_id
```

### _migrations

```sql
CREATE TABLE _migrations (
    version     INTEGER PRIMARY KEY,    -- 迁移编号
    description TEXT NOT NULL,          -- 迁移描述
    applied_at  TEXT NOT NULL           -- 执行时间 (ISO 8601)
);
```

### 迁移历史

由 `MigrationRunner` 管理，按编号顺序幂等执行：

| 编号 | 描述 | SQL |
|------|------|-----|
| 001 | dim_items 新增 setitem_id | `ADD COLUMN setitem_id INTEGER` |
| 002 | dim_items 新增 BLOB 列 | `ADD COLUMN icon_data BLOB` / `preview_data BLOB` |
| 003 | dim_skills 新增列 | `ADD COLUMN skill_h TEXT` / `common_props TEXT` |
| 004 | dim_items 新增 sn | `ADD COLUMN sn INTEGER` |
| 005 | dim_items 新增 time_limited | `ADD COLUMN time_limited INTEGER DEFAULT 0` |
| 006 | dim_items 新增 req_job | `ADD COLUMN req_job INTEGER` |
| 007 | BLOB 拆分+哈希去重 | 重建 `dim_items/dim_skills`，新增 `dim_blob_assets`，主表保存 blob_id |

## CLI 测试工具 (mapleidb)

命令行工具，用于验证数据提取与查询功能，全部通过 Application 用例执行，输出纯 JSON。

- **可执行文件名**: `mapleidb`（项目 `MapleItemDB.Cli`，`AssemblyName` 为 `mapleidb`）
- **默认数据库**: `D:\GAME\MapleStory228\Data\mapleitemdb.db`
- **DI**: 通过 `AddMapleItemDb()` 注册服务，禁用日志 (`LogLevel.None`)
- **输出约定**: JSON，camelCase，枚举为字符串，`byte[]` 序列化为 `true/false`

### 子命令与调用链

```
mapleidb extract [--wz <dir>] [--db <path>]
  → IExtractAndImportUseCase.ExecuteAsync()

mapleidb search [keyword] [options] [--db <path>]
  → ISearchItemsUseCase.ExecuteAsync(SearchRequest)

mapleidb skill [keyword] [--limit <n>] [--db <path>]
  → ISearchSkillsUseCase.ExecuteAsync()

mapleidb get <id> [--db <path>]
  → IGetItemByIdUseCase.ExecuteAsync()

mapleidb stats [--db <path>]
  → IGetStatsUseCase.ExecuteAsync()
```

### search 参数

| 参数 | 对应字段 | 说明 |
|------|---------|------|
| `[keyword]` (位置参数) | Keyword | 模糊搜索名称/描述/ID |
| `--category <cat>` | Category | Equip/Consume/Etc/Setup/Cash/Pet |
| `--sub <sub>` | SubCategory | Weapon/Cap/Coat 等 |
| `--min-level <n>` | MinLevel | 最小等级 |
| `--max-level <n>` | MaxLevel | 最大等级 |
| `--cash` | IsCash=true | 仅商城道具 |
| `--has-sn` | HasSn=true | 仅含 SN |
| `--min-boss <n>` | MinBossDmg | 最小 Boss 伤害 |
| `--min-ied <n>` | MinIed | 最小无视防御 |
| `--limit <n>` | Limit | 结果数限制 (默认 50) |

### JSON 输出规则

| 规则 | 说明 |
|------|------|
| 属性命名 | `JsonNamingPolicy.CamelCase` |
| 枚举序列化 | `JsonStringEnumConverter`，输出原始名称 (如 `"Equip"`) |
| 中文编码 | `JavaScriptEncoder.UnsafeRelaxedJsonEscaping`，中文直输不转义 |
| blob 字段 | 自定义 `BlobHasValueConverter`，`byte[]` → `true/false` |
| null 字段 | 保留输出，便于验证完整 Schema |
| 缩进 | `WriteIndented = true` |

### 运行示例

```bash
dotnet run --project src/MapleItemDB.Cli -- stats
dotnet run --project src/MapleItemDB.Cli -- search 阿比斯 --category Equip --min-level 200
dotnet run --project src/MapleItemDB.Cli -- skill 终极攻击 --limit 10
dotnet run --project src/MapleItemDB.Cli -- get 1572000
dotnet run --project src/MapleItemDB.Cli -- extract --wz D:\GAME\MapleStory228\Data
```

