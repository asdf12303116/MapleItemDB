  # MapleItemDB 架构升级方案（激进重构，单次合并）

  ## 摘要

  目标是把当前“UI 直连仓储/提取器、核心类过重、跨层职责混杂”的结构，重构为清晰的分层架构：Presentation(UI/CLI) ->
  Application -> Domain(Core) -> Infrastructure。
  本次允许重定义接口，按一次性大改交付，但通过模块化拆分保证后续维护和测试成本显著下降。

  ## 依据来源

  - 文档：doc/PROJECT_STRUCTURE_AND_DATAFLOW.md
  - 代码：src/MapleItemDB.UI/ViewModels/MainViewModel.cs、src/MapleItemDB.Infrastructure/Repositories/
    ItemRepository.cs、src/MapleItemDB.WzExtraction/Services/WzExtractionService.cs、src/MapleItemDB.UI/
    ServiceRegistration.cs、src/MapleItemDB.Cli/Program.cs、src/MapleItemDB.Infrastructure/Database/
    DatabaseBootstrapper.cs

  ## 目标架构

  1. 新增 MapleItemDB.Application 工程，承载用例编排，不再让 UI/CLI 直接调用仓储和提取器。
  2. MapleItemDB.Core 仅保留领域模型、领域服务接口、值对象和业务规则，不含基础设施细节。
  3. MapleItemDB.Infrastructure 仅实现端口接口（SQLite、WZ 适配、缓存适配）。
  4. MapleItemDB.UI 和 MapleItemDB.Cli 只依赖 Application 层的查询/命令接口。
  5. 新增独立 MapleItemDB.Bootstrap（或 MapleItemDB.CompositionRoot）统一 DI，去除 UI 工程对 CLI 的“反向复用”。

  ## 公共接口/类型变更（重点）

  1. 废弃 IItemRepository 巨型接口，拆为：
      - IItemQueryService（查询道具）
      - ISkillQueryService（查询技能）
      - ISetItemQueryService（查询套装）
      - IItemImportWriter（批量写入道具/技能/套装）
  2. IWzExtractor 重定义为纯提取职责：
      - 输入：ExtractionRequest
      - 输出：RawExtractionBundle
      - 不包含数据库写入逻辑
  3. 新增 Application 用例接口：
      - IExtractAndImportUseCase
      - ISearchItemsUseCase
      - ISearchSkillsUseCase
      - IGetItemDetailUseCase
      - IGetStatsUseCase
  4. 新增统一进度模型（替换散落的 tuple）：
      - ProgressReport { Stage, Current, Total, Message }
  5. InMemorySearchIndex 抽象为 ISearchIndex，由 Application 决定刷新时机。
  6. StatsDisplayHelper、SkillSummaryParser 从 MainViewModel 拆出为 Application/Domain 的格式化服务接口，UI 仅绑定结果
     DTO。

  ## 关键重构点

  1. 拆分 MainViewModel：
      - SearchViewModel（搜索条件与结果）
      - DetailViewModel（详情和格式化展示）
      - ExtractionViewModel（提取导入进度）
  2. 拆分 ItemRepository：
      - ItemReadRepository（Query SQL）
      - ItemWriteRepository（BulkUpsert + 事务策略）
      - SetItemRepository、SkillRepository
  3. 把“提取 + 导入 + 索引刷新”从 ViewModel 下沉到 ExtractAndImportUseCase。
  4. 把 DatabaseBootstrapper 的“Schema + SafeAddColumn”迁移为显式迁移机制：
      - IDbMigration
      - MigrationRunner
      - 每个迁移文件单独编号，支持幂等执行
  5. 统一错误模型：
      - Result<T> / AppError（不再到处用裸 Exception 文本拼接）
  6. 统一日志上下文：
      - 用例级 OperationId
      - 提取阶段结构化日志字段（stage/itemCount/elapsed）

  ## 目录调整（实施后）

  1. src/MapleItemDB.Application/
  2. src/MapleItemDB.Bootstrap/
  3. src/MapleItemDB.Infrastructure/Repositories/Read/
  4. src/MapleItemDB.Infrastructure/Repositories/Write/
  5. src/MapleItemDB.UI/ViewModels/Search|Detail|Extraction/
  6. src/MapleItemDB.Core/Abstractions|ValueObjects|Errors/

  ## 数据与兼容策略

  1. 数据库表可保持现有 schema（降低数据迁移风险），先做代码架构重组。
  2. 若后续要拆 dynamic_stats JSON，再做第二阶段 schema 正规化。
  3. CLI 命令名可调整（你已选择允许重定义接口），但本次建议保留现有命令以降低使用迁移成本。

  ## 测试与验收

  1. 单元测试：
      - 用例层：查询、提取导入、错误分支、取消令牌
      - 格式化服务：Stats、技能描述解析
      - 迁移执行器：幂等与顺序保证
  2. 集成测试（SQLite 临时库）：
      - Search/Get/Stats 全链路
      - 批量写入事务边界与回滚
  3. CLI 最小闭环验证（强制）：
      - dotnet run --project src/MapleItemDB.Cli -- stats
  4. 验收标准：
      - UI/CLI 不再直接依赖具体仓储实现类
      - MainViewModel 不再承担提取导入编排
      - 仓储接口无“读写全能”单体接口
      - 新增迁移机制可重复执行且无副作用

  ## 实施顺序（单次合并内的执行步骤）

  1. 建立 Application 与 Bootstrap 工程并迁移 DI 入口。
  2. 定义新端口接口和用例接口，替换 UI/CLI 调用面。
  3. 拆分仓储读写实现，迁移 SQL 与批量写入逻辑。
  4. 重构提取流程为“提取器 + 导入用例”两段式。
  5. 拆分 ViewModel，迁移格式化逻辑到服务层。
  6. 引入迁移执行器，替换 SafeAddColumn 风格。
  7. 全量测试 + CLI 验证 + 文档更新（含新架构图和调用链）。

  ## 假设与默认值

  1. 默认继续使用 SQLite 作为唯一持久化实现。
  2. 默认不在本轮做 dynamic_stats/consume_spec 的表结构拆分。
  3. 默认保留现有 WZ 提取业务规则与字段语义，不改业务口径。
  4. 默认以“单次合并”交付，但内部按上述顺序实现，确保可逐段验证。