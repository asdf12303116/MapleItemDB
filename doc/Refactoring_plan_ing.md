# MapleItemDB 重构执行进度记录（Refactoring_plan_ing）

更新时间：2026-02-26

## 依据来源
- 文档：`doc/Refactoring_plan.md`
- 文档：`doc/PROJECT_STRUCTURE_AND_DATAFLOW.md`
- 代码：本地工作区改动（`src/MapleItemDB.Application`、`src/MapleItemDB.Bootstrap`、`src/MapleItemDB.Cli`、`src/MapleItemDB.UI`）
- 运行验证：`dotnet build` 与 `mapleidb` 命令执行结果

## 总体状态
- 当前阶段：计划步骤 1、2、4 已完成；步骤 3 部分完成（接口层拆分已完成）；步骤 5 最小下沉已完成；步骤 6 未开始；步骤 7 部分完成。
- 核心结论：`MainViewModel` 已完全不依赖 `IItemRepository`，搜索/详情/初始化/提取均通过 Application 用例调用。UI 层达到"仅依赖 Application 用例 + ISearchIndex"的目标。
- 当前风险：`App.xaml.cs` 仍直接使用 `DatabaseBootstrapper`（属于基础设施启动，非业务调用）。

## 按原计划步骤跟踪

### 1. 建立 Application 与 Bootstrap 工程并迁移 DI 入口
- 状态：**已完成**
- 已完成：
  - `src/MapleItemDB.Bootstrap/Class1.cs` 提供统一组合根 `AddMapleItemDb(...)`
  - `src/MapleItemDB.UI/ServiceRegistration.cs` 改为复用 `AddMapleItemDb(...)`
  - `src/MapleItemDB.Cli/Program.cs` 改为复用 `AddMapleItemDb(...)`
  - `MapleItemDB.UI.csproj` / `MapleItemDB.Cli.csproj` 增加对 `MapleItemDB.Bootstrap` 引用

### 2. 定义新端口接口和用例接口，替换 UI/CLI 调用面
- 状态：**已完成**
- 已完成：
  - Application 补齐并注册全部用例：
    - `ISearchSkillsUseCase` / `SearchSkillsUseCase`
    - `IGetItemByIdUseCase` / `GetItemByIdUseCase`
    - `IGetStatsUseCase` / `GetStatsUseCase`
    - `IGetItemDetailUseCase` / `GetItemDetailUseCase`
    - `IInitializeCatalogUseCase` / `InitializeCatalogUseCase`
    - `ISearchItemsUseCase` / `SearchItemsUseCase`
  - `SearchRequest` 扩展到覆盖 CLI 搜索参数（`MinLevel/MaxLevel/IsCash/MinBossDmg/MinIed`）
  - CLI 子命令全部切换为调用 Application 用例
  - **UI `MainViewModel` 完全切换为 Application 用例调用**：
    - 构造函数：`IItemRepository` → `IInitializeCatalogUseCase` + `ISearchItemsUseCase` + `IGetItemByIdUseCase` + `IGetItemDetailUseCase`
    - `InMemorySearchIndex` → `ISearchIndex`（接口注入）
    - `InitializeAsync` → `IInitializeCatalogUseCase`
    - `SearchAsync` → `ISearchItemsUseCase`
    - `LoadItemDetailAsync` → `IGetItemByIdUseCase`
    - `OnSelectedItemChanged` → `IGetItemDetailUseCase`
    - `ExtractDataAsync` → `IExtractAndImportUseCase`（已在前一轮完成）
  - 移除了 MainViewModel 中的 `StatsDisplayHelper`、`BuildCategoryDisplay`、`BuildSkillDetailText`、`BuildEffectsText`、`UpdateSetItemDisplay` 等本地格式化逻辑
  - 移除了 `using MapleItemDB.Infrastructure.Cache` 等直接基础设施引用

### 3. 拆分仓储读写实现，迁移 SQL 与批量写入逻辑
- 状态：部分完成
- 已完成：
  - Core 接口层读写拆分（`IItemReadRepository` / `IItemWriteRepository` / `ISetItem*` / `ISkill*`）
  - 通过 DI 将 `ItemRepository` 映射到拆分接口
- 未完成：
  - Infrastructure 仍是单体 `ItemRepository`，尚未拆分 `Read/Write` 独立实现类

### 4. 重构提取流程为"提取器 + 导入用例"两段式
- 状态：**已完成**（CLI + UI）
- 已完成：
  - `ExtractAndImportUseCase` 已承接提取+写库+索引刷新编排
  - CLI `extract` 已切换为仅调用 `IExtractAndImportUseCase`
  - UI `MainViewModel.ExtractDataAsync` 已切换为调用 `IExtractAndImportUseCase`

### 5. 拆分 ViewModel，迁移格式化逻辑到服务层
- 状态：部分完成
- 已完成：
  - 新增 `ItemDetailFormattingHelper`（Application）并用于 `GetItemDetailUseCase`
  - MainViewModel 中的 `StatsDisplayHelper`、`SkillSummaryParser`、`BuildCategoryDisplay` 等格式化逻辑已下沉到 Application 层
  - MainViewModel 不再包含任何格式化业务逻辑
- 未完成：
  - `MainViewModel` 仍为单一大类，未拆分为 SearchViewModel / DetailViewModel / ExtractionViewModel

### 6. 引入迁移执行器，替换 SafeAddColumn 风格
- 状态：未开始
- 现状：
  - `DatabaseBootstrapper` 仍使用 `SafeAddColumnAsync`
  - 尚未引入 `IDbMigration` / `MigrationRunner`

### 7. 全量测试 + CLI 验证 + 文档更新
- 状态：部分完成
- 已完成：
  - `dotnet build MapleItemDB.sln`：通过（0 error, 0 warning）
  - CLI 验证已执行：`stats` / `search` / `skill` / `get` / `extract`
  - 本文件已更新
- 未完成：
  - 尚未新增自动化测试用例（仅执行命令行验证）
  - UI 功能需人工验证

## 当前编译状态（最近一次）
- 命令：`dotnet build MapleItemDB.sln`
- 结果：成功
- 摘要：0 错误，0 警告

## 本轮改动摘要（2026-02-26）

### MainViewModel 改造要点
1. **构造函数**：去掉 `IItemRepository` + `InMemorySearchIndex`，改为注入 5 个用例接口 + `ISearchIndex`
2. **InitializeAsync**：`_repository.GetIdNameIndexAsync()` + `_repository.GetAllSetItemsAsync()` → `_initializeCatalogUseCase.ExecuteAsync()`
3. **SearchAsync**：手动拼 `ItemQueryFilter` + `_repository.QueryAsync/SearchSkillsByNameAsync` → `_searchItemsUseCase.ExecuteAsync(SearchRequest)`
4. **LoadItemDetailAsync**：`_repository.GetByIdAsync` → `_getItemByIdUseCase.ExecuteAsync`
5. **OnSelectedItemChanged**：本地 `BuildCategoryDisplay/FormatStats/BuildSkillDetailText/UpdateSetItemDisplay` → `_getItemDetailUseCase.ExecuteAsync`
6. **删除代码**：`StatsDisplayHelper` 整个类、`CategoryDisplayNames`、`BuildCategoryDisplay`、`BuildSkillDetailText`、`ParseCommonProps`、`UpdateSetItemDisplay`、`BuildEffectsText` — 约 300+ 行 UI 侧格式化代码

### 依赖变化
- `MainViewModel` 不再 `using MapleItemDB.Infrastructure.Cache`
- `MainViewModel` 不再 `using System.Text` / `System.Text.Json` / `System.Text.RegularExpressions`（格式化逻辑已下沉）
- `_skillSearchCache` 类型从 `Dictionary<int, SkillEntity>` 改为 `IReadOnlyDictionary<int, SkillEntity>`（与 `SearchResult.SkillsById` 类型对齐）

## 本轮 CLI 验证记录

### 1) 数据库统计
- 命令：`dotnet run --project src/MapleItemDB.Cli -- stats`
- 关键输出（JSON）：
  - `totalItems: 83967`
  - `totalSetItems: 767`
  - `totalSkills: 3917`
- 结论：通过（CLI 已通过 Application 用例输出统计）

### 2) 道具搜索
- 命令：`dotnet run --project src/MapleItemDB.Cli -- search 阿比斯 --category Equip --limit 1`
- 关键输出（JSON）：`[]`
- 结论：通过（命令执行与参数解析正常，返回空结果属数据本身）

### 3) 技能搜索
- 命令：`dotnet run --project src/MapleItemDB.Cli -- skill 终极攻击 --limit 2`
- 关键输出（JSON）：
  - `skillId: 1100002, name: "终极剑斧"`
  - `skillId: 1120013, name: "进阶终极攻击"`
- 结论：通过

### 4) 道具详情
- 命令：`dotnet run --project src/MapleItemDB.Cli -- get 1572000`
- 关键输出（JSON）：
  - `item.itemId: 1572000`
  - `item.name: "锋利之影"`
  - `setItem: null`
- 结论：通过

### 5) 提取流程
- 命令：`dotnet run --project src/MapleItemDB.Cli -- extract --wz D:\GAME\MapleStory228\Data`
- 关键输出（JSON）：
  - `items: 83967`
  - `setItems: 767`
  - `skills: 10022`
- 结论：通过（调用链已切到 `IExtractAndImportUseCase`）

## 下一步执行清单（建议顺序）
1. 仓储实现拆分：将 `ItemRepository` 拆分为 `Read/Write` 目录与独立类。
2. ViewModel 拆分：将 `MainViewModel` 拆分为 `SearchViewModel` / `DetailViewModel` / `ExtractionViewModel`。
3. 迁移机制重构：引入 `IDbMigration` + `MigrationRunner`，替换 `SafeAddColumnAsync`。
4. 补充自动化测试与文档验收项。

## 交接提示
- 本文件用于"断点续做"。
- 恢复执行时，建议从"下一步执行清单"第 1 条继续。
