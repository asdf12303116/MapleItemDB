# MapleItemDB 项目结构与核心数据流

## 项目结构

```
maplestory_toolbox/
├── src/
│   ├── MapleItemDB.Core/              # 领域模型与接口
│   │   ├── Interfaces/
│   │   │   ├── ICacheManager.cs       # 缓存管理接口
│   │   │   ├── IItemRepository.cs     # 道具仓储接口
│   │   │   ├── IStringResolver.cs     # 字符串解析接口
│   │   │   └── IWzExtractor.cs        # WZ 提取器接口
│   │   ├── Models/
│   │   │   ├── ItemEntity.cs          # 道具主实体
│   │   │   ├── ItemCategory.cs        # 道具分类枚举
│   │   │   ├── ItemQueryFilter.cs     # 查询筛选条件
│   │   │   ├── SkillEntity.cs         # 技能实体
│   │   │   ├── SetItemInfo.cs         # 套装信息
│   │   │   └── StatsLine.cs           # 属性行 (UI 展示用)
│   │   └── Data/
│   │       └── SnMap.cs               # SN 映射表 (嵌入资源)
│   │
│   ├── MapleItemDB.Infrastructure/    # 基础设施实现层
│   │   ├── Database/
│   │   │   ├── DatabaseBootstrapper.cs   # 建库/迁移/WAL 模式
│   │   │   └── SqliteConnectionFactory.cs
│   │   ├── Repositories/
│   │   │   └── ItemRepository.cs      # Dapper 仓储 (分批写入)
│   │   └── Cache/
│   │       └── InMemorySearchIndex.cs # 内存搜索索引
│   │
│   ├── MapleItemDB.WzExtraction/      # WZ 数据提取层
│   │   ├── Services/
│   │   │   ├── WzExtractionService.cs # 提取管道主服务
│   │   │   ├── EquipExtractor.cs      # 装备提取器
│   │   │   ├── GeneralItemExtractor.cs# 通用道具提取器
│   │   │   ├── SetItemExtractor.cs    # 套装信息提取器
│   │   │   ├── SkillExtractor.cs      # 技能提取器
│   │   │   ├── StringPoolBuilder.cs   # 字符串池构建器
│   │   │   ├── MacroResolver.cs       # 宏变量解析器
│   │   │   └── IconExporter.cs        # 图标导出器
│   │   └── Helpers/
│   │       └── WzNodeExtensions.cs    # WZ 节点扩展方法
│   │
│   ├── MapleItemDB.Cli/              # 命令行测试工具 (mapleidb)
│   │   └── Program.cs               # CLI 入口 (子命令分发 + JSON 输出)
│   │
│   └── MapleItemDB.UI/               # WPF 客户端
│       ├── App.xaml.cs                # 应用入口 + DI 容器
│       ├── ViewModels/
│       │   ├── MainViewModel.cs       # 主窗口 ViewModel (含 StatsDisplayHelper 等辅助类)
│       │   └── SkillSummaryParser.cs  # 技能描述解析器
│       ├── Views/
│       │   └── MainWindow.xaml(.cs)   # 主窗口
│       └── Converters/
│           └── Converters.cs          # 值转换器
│
├── tests/WzLoadTest/                  # 手动验证 WZ 提取流程
├── lib/WzComparerR2/                  # WZ 解析依赖库
└── data/                              # 数据资源
    ├── sn_map.json                    # SN 映射表
    ├── sn.txt                         # SN 原始数据
    └── convert_sn.ps1                 # SN 转换脚本
```

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

## 核心数据流

### 1. 启动与初始化

1. `App.xaml.cs` 启动后创建 DI 容器，注册服务：
   - `SqliteConnectionFactory`、`DatabaseBootstrapper` — 数据库
   - `IItemRepository` → `ItemRepository` — 仓储
   - `InMemorySearchIndex` — 内存索引
   - `IWzExtractor` → `WzExtractionService` — WZ 提取
   - `MainViewModel`、`MainWindow` — UI
2. 调用 `DatabaseBootstrapper.EnsureCreatedAsync()` 创建/迁移 `dim_items`、`dim_setitems`、`dim_skills` 表与索引，并启用 WAL 模式。
3. 打开主窗口后执行 `MainViewModel.InitializeAsync()`，预加载 Id-Name 索引与套装缓存。

### 2. WZ 数据导入

1. UI 触发导入命令后调用 `IWzExtractor.ExtractAllAsync(...)`（实现为 `WzExtractionService`）。
2. 提取服务自动检测 WZ 格式（传统单文件或 KMST1125 文件夹式），加载 WZ 文件与字符串池（`StringPoolBuilder`），通过 `MacroResolver` 处理描述文本宏。
3. 分阶段提取：
   - `EquipExtractor` — 从 Character 节点提取装备 (含需求属性、核心数值、reqJob、动态属性、标志位)
   - `GeneralItemExtractor` — 从 Item 节点提取各分类 (Consume/Etc/Install/Cash/Pet)
   - `SetItemExtractor` — 从 SetItemInfo 提取套装数据
   - `SkillExtractor` — 从 Skill 节点提取技能数据
4. `IconExporter` 导出图标与预览图为内存 PNG BLOB。
5. 从 `SnMap` 映射表填充商城 SN 编号。
6. 返回 `ExtractionResult(Items, SetItems, Skills)`。

### 3. 数据落库

数据库写入在 `Task.Run` 中执行，避免 SQLite 同步 I/O 阻塞 UI 线程。

1. `ItemRepository.BulkUpsertAsync(...)` — 分批写入 `dim_items`（每 1000 行一批），带进度回调。
2. `ItemRepository.BulkUpsertSetItemsAsync(...)` — 分批写入 `dim_setitems`（JSON 序列化）。
3. `ItemRepository.BulkUpsertSkillsAsync(...)` — 分批写入 `dim_skills`（每 500 行一批），带进度回调。

写入优化策略：
- **分批事务**：避免单次大事务长时间持锁，每批独立 `BEGIN/COMMIT`。
- **PRAGMA 优化**：写入期间设置 `synchronous=NORMAL` / `cache_size=-64000` / `temp_store=MEMORY`，完成后恢复默认。
- **WAL 模式**：在 `DatabaseBootstrapper` 中启用，持久化设置，减少写入时的锁竞争。

### 4. 提取进度映射

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

### 5. 搜索与展示

1. 搜索输入优先走 `InMemorySearchIndex`（自动补全与快速 ID 命中）。
2. 普通道具查询使用 `ItemQueryFilter` 组合条件，交由 `ItemRepository.QueryAsync(...)` 执行，支持 `JSON_EXTRACT` 查询动态属性。
3. 技能查询走 `SearchSkillsByNameAsync(...)`，再由 UI 层转换为 `ItemEntity` 复用 DataGrid 展示。
4. 选中条目后，`MainViewModel` 汇总属性、套装效果、技能摘要（`SkillSummaryParser`）并更新详情面板。

### 6. 详情面板属性格式化

`StatsDisplayHelper.FormatStats()` 将 `ItemEntity` 格式化为游戏风格文本行（`StatsLine`），展示顺序：

1. **特殊标志** (橙色置顶)：唯一道具、不可交易、装备后不可交易、账号内共享、限时道具、星之力增强道具、不可使用潜能、固定潜能
2. **职业限制**：解析 `ReqJob` 位掩码，非全职业装备显示 "职业:战士, 魔法师" 等
3. **武器分类**：武器/副手武器显示细分类型（如 "分类 : 双手斧"）
4. **需求等级**
5. **升级可用次数** (来自 DynamicStats)
6. **核心属性**：力量/敏捷/智力/运气/HP/MP/攻击力/魔攻/物防/魔防
7. **动态属性**：Boss 伤害、无视防御、总伤害、全属性、移速、跳跃、击退
8. **攻击速度** (等级映射为名称，如 "快(4)")
9. **消耗品效果** (如有)

### 7. 缓存策略

- `InMemorySearchIndex`：维护 `item_id -> name` 全量内存索引，用于毫秒级搜索提示。
- `MainViewModel` 内部缓存：技能搜索缓存 (`_skillSearchCache`)、套装缓存 (`_setItems`)，减少重复数据库查询并提升详情切换速度。

## 数据库 Schema

### dim_items

```sql
CREATE TABLE dim_items (
    item_id       INTEGER PRIMARY KEY,
    name          TEXT NOT NULL,
    description   TEXT,
    category      TEXT NOT NULL,        -- Equip/Consume/Etc/Setup/Cash/Pet
    sub_category  TEXT,                 -- Weapon/Cap/Coat/SecondWeapon 等
    req_level     INTEGER,
    req_str       INTEGER,
    req_dex       INTEGER,
    req_int       INTEGER,
    req_luk       INTEGER,
    req_job       INTEGER,              -- 职业需求位掩码
    inc_str       INTEGER,
    inc_dex       INTEGER,
    inc_int       INTEGER,
    inc_luk       INTEGER,
    inc_pad       INTEGER,
    inc_mad       INTEGER,
    inc_pdd       INTEGER,
    inc_mdd       INTEGER,
    inc_mhp       INTEGER,
    inc_mmp       INTEGER,
    dynamic_stats TEXT,                 -- JSON: boss_dmg, ied, total_dmg, 标志位等
    consume_spec  TEXT,                 -- JSON: 消耗品效果
    is_cash       INTEGER DEFAULT 0,
    price         INTEGER,
    icon_data     BLOB,                 -- PNG 二进制
    preview_data  BLOB,                 -- PNG 二进制
    setitem_id    INTEGER,
    sn            INTEGER,
    time_limited  INTEGER DEFAULT 0,
    extracted_at  TEXT NOT NULL
);
-- 索引: idx_items_name, idx_items_category, idx_items_cash
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
    icon_data      BLOB,                -- PNG 二进制
    is_hidden      INTEGER DEFAULT 0,
    level_effects  TEXT,                -- JSON: 各等级效果
    skill_h        TEXT,                -- 等级效果描述模板
    common_props   TEXT,                -- JSON: common 属性公式字典
    extracted_at   TEXT NOT NULL
);
-- 索引: idx_skills_name, idx_skills_job
```

### 迁移历史

按 `DatabaseBootstrapper.EnsureCreatedAsync()` 中 `SafeAddColumnAsync` 顺序：

1. `dim_items` ADD `setitem_id` INTEGER
2. `dim_items` ADD `icon_data` BLOB / `preview_data` BLOB
3. `dim_skills` ADD `skill_h` TEXT / `common_props` TEXT
4. `dim_items` ADD `sn` INTEGER
5. `dim_items` ADD `time_limited` INTEGER DEFAULT 0
6. `dim_items` ADD `req_job` INTEGER
7. PRAGMA `journal_mode=WAL`

## CLI 测试工具 (mapleidb)

命令行测试工具，用于验证数据提取与查询功能，所有查询命令输出纯 JSON。

- **可执行文件名**: `mapleidb`（项目 `MapleItemDB.Cli`，`AssemblyName` 为 `mapleidb`）
- **默认数据库**: `D:\GAME\MapleStory228\Data\mapleitemdb.db`
- **DI 复用**: 通过 `ServiceRegistration.Configure` 注册服务，禁用日志（`LogLevel.None`），不依赖 `MainViewModel`，直接使用 `IItemRepository` 查询
- **输出约定**: JSON 格式，camelCase 属性名，枚举输出为字符串，`byte[]` 字段序列化为 `true/false` 表示是否有数据

### 子命令

```
mapleidb extract [--wz <dir>] [--db <path>]
mapleidb search [keyword] [options] [--db <path>]
mapleidb skill [keyword] [--limit <n>] [--db <path>]
mapleidb get <id> [--db <path>]
mapleidb stats [--db <path>]
```

#### extract — WZ 数据提取

从 WZ 数据目录提取道具/技能/套装数据并写入数据库。进度信息输出到 stderr，最终统计以 JSON 输出到 stdout。

| 参数 | 说明 | 默认值 |
|------|------|--------|
| `--wz <dir>` | WZ 数据目录 | `D:\GAME\MapleStory228\Data` |
| `--db <path>` | 数据库路径 | 同全局默认 |

调用链: `IWzExtractor.ExtractAllAsync` → `IItemRepository.BulkUpsert*`

#### search — 道具查询

覆盖 `ItemQueryFilter` 全部字段，输出 JSON 数组。

| 参数 | 对应 Filter 字段 | 说明 |
|------|-----------------|------|
| `[keyword]` (位置参数) | `Keyword` | 模糊搜索名称/描述/ID |
| `--category <cat>` | `Category` | Equip/Consume/Etc/Setup/Cash/Pet |
| `--sub <sub>` | `SubCategory` | Weapon/Cap/Coat 等 |
| `--min-level <n>` | `MinLevel` | 最小等级 |
| `--max-level <n>` | `MaxLevel` | 最大等级 |
| `--cash` | `IsCash=true` | 仅商城道具 |
| `--has-sn` | `HasSn=true` | 仅含 SN |
| `--min-boss <n>` | `MinBossDmg` | 最小 Boss 伤害 |
| `--min-ied <n>` | `MinIed` | 最小无视防御 |
| `--limit <n>` | `Limit` | 结果数限制 (默认 50) |

调用链: `IItemRepository.QueryAsync(filter)`

#### skill — 技能查询

输出 JSON 数组。

| 参数 | 说明 |
|------|------|
| `[keyword]` | 技能名称模糊搜索 |
| `--limit <n>` | 结果数限制 (默认 50) |

调用链: `IItemRepository.SearchSkillsByNameAsync`

#### get — 道具详情

输出 JSON 对象 `{ item, setItem }`，包含道具全部属性及关联套装信息。

| 参数 | 说明 |
|------|------|
| `<id>` (必填) | 道具 ID |

调用链: `IItemRepository.GetByIdAsync` + `IItemRepository.GetAllSetItemsAsync`

#### stats — 数据库统计

输出 JSON 对象，包含各分类道具数量、套装数量、技能数量、商城道具数量、含 SN 道具数量。

### JSON 输出规则

| 规则 | 说明 |
|------|------|
| 属性命名 | `JsonNamingPolicy.CamelCase` |
| 枚举序列化 | `JsonStringEnumConverter`，输出原始名称 (如 `"Equip"`) |
| 中文编码 | `JavaScriptEncoder.UnsafeRelaxedJsonEscaping`，中文直输不转义 |
| blob 字段 | 自定义 `BlobHasValueConverter`，`byte[]` → `true` (有数据) / `false` (无数据) |
| null 字段 | 保留输出，便于验证完整 Schema |
| 缩进 | `WriteIndented = true` |

### 运行示例

```bash
# 构建
dotnet build src/MapleItemDB.Cli

# 帮助
dotnet run --project src/MapleItemDB.Cli -- --help
dotnet run --project src/MapleItemDB.Cli -- search --help

# 统计
dotnet run --project src/MapleItemDB.Cli -- stats

# 搜索装备
dotnet run --project src/MapleItemDB.Cli -- search 阿比斯 --category Equip --min-level 200

# 技能搜索
dotnet run --project src/MapleItemDB.Cli -- skill 终极攻击 --limit 10

# 道具详情
dotnet run --project src/MapleItemDB.Cli -- get 1572000

# 指定数据库
dotnet run --project src/MapleItemDB.Cli -- stats --db ./other.db
```
