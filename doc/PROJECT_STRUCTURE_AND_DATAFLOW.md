# MapleItemDB 项目结构与核心数据流

## 项目结构

- `src/MapleItemDB.Core`：领域模型与接口定义（`ItemEntity`、`SkillEntity`、`SetItemInfo`、`IItemRepository`、`IWzExtractor` 等）。
- `src/MapleItemDB.Infrastructure`：基础设施实现层，提供 SQLite 建库/迁移、Dapper 仓储、内存搜索索引。
- `src/MapleItemDB.WzExtraction`：WZ 数据提取层，负责从游戏目录读取资源并组装为业务实体。
- `src/MapleItemDB.UI`：WPF 客户端，负责依赖注入、用户交互、搜索过滤、详情展示与导入流程触发。
- `tests/WzLoadTest`：当前测试入口，主要用于手动验证 WZ 提取流程。
- `lib/WzComparerR2`：WZ 解析相关依赖代码。
- `data` / 根目录 `sn_map.json`：SN 映射等数据资源。

## 核心数据流

### 1. 启动与初始化

1. `App.xaml.cs` 启动后创建 DI 容器，注册 `SqliteConnectionFactory`、`DatabaseBootstrapper`、`ItemRepository`、`MainViewModel` 等服务。
2. 调用 `DatabaseBootstrapper.EnsureCreatedAsync()` 创建/迁移 `dim_items`、`dim_setitems`、`dim_skills` 表与索引。
3. 打开主窗口后执行 `MainViewModel.InitializeAsync()`，预加载 Id-Name 索引与套装缓存。

### 2. WZ 数据导入

1. UI 触发导入命令后调用 `IWzExtractor.ExtractAllAsync(...)`（实现为 `WzExtractionService`）。
2. 提取服务加载 WZ 文件与字符串池（`StringPoolBuilder`），并通过 `MacroResolver` 处理描述文本宏。
3. 分阶段提取装备、普通道具、套装、技能，并导出图标/预览图，最终生成 `ExtractionResult`。

### 3. 数据落库

1. `ItemRepository.BulkUpsertAsync(...)` 批量写入/更新 `dim_items`。
2. `ItemRepository.BulkUpsertSetItemsAsync(...)` 写入/更新 `dim_setitems`（JSON 序列化）。
3. `ItemRepository.BulkUpsertSkillsAsync(...)` 写入/更新 `dim_skills`。

### 4. 搜索与展示

1. 搜索输入优先走 `InMemorySearchIndex`（自动补全与快速 ID 命中）。
2. 普通道具查询使用 `ItemQueryFilter` 组合条件，交由 `ItemRepository.QueryAsync(...)` 执行。
3. 技能查询走 `SearchSkillsByNameAsync(...)`，再由 UI 层转换展示模型。
4. 选中条目后，`MainViewModel` 汇总属性、套装效果、技能摘要（`SkillSummaryParser`）并更新详情面板。

### 5. 缓存策略

- `InMemorySearchIndex`：维护 `item_id -> name` 全量内存索引，用于毫秒级搜索提示。
- `MainViewModel` 内部缓存：技能缓存、套装缓存，减少重复数据库查询并提升详情切换速度。
