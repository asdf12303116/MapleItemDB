# MapleItemDB 重构执行进度记录（Refactoring_plan_ing）

更新时间：2026-02-26

## 依据来源
- 文档：`doc/Refactoring_plan.md`
- 文档：`doc/PROJECT_STRUCTURE_AND_DATAFLOW.md`
- 代码：本地工作区改动（`src/MapleItemDB.Application`、`src/MapleItemDB.Bootstrap`、`src/MapleItemDB.Cli`、`src/MapleItemDB.UI`、`src/MapleItemDB.Infrastructure`）
- 运行验证：`dotnet build` 与 `mapleidb` 命令执行结果

## 总体状态
- 当前阶段：计划步骤 1、2、3、4、6 已完成；步骤 5 格式化下沉已完成、ViewModel 拆分评估后延后；步骤 7 部分完成。
- 核心结论：架构分层目标已基本达成。UI/CLI 仅依赖 Application 用例；Infrastructure 仓储已读写分离为独立类；迁移机制已从 SafeAddColumn 升级为编号迁移（IDbMigration + MigrationRunner）。
- 剩余项：ViewModel 物理拆分（评估后延后）、自动化测试、UI 人工验证。

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
  - Application 补齐并注册全部用例（7 个）
  - `SearchRequest` 扩展到覆盖 CLI 搜索参数
  - CLI 子命令全部切换为调用 Application 用例
  - UI `MainViewModel` 完全切换为 Application 用例调用
  - 移除了 MainViewModel 中全部格式化逻辑（~300+ 行）

### 3. 拆分仓储读写实现，迁移 SQL 与批量写入逻辑
- 状态：**已完成**
- 已完成：
  - Core 接口层读写拆分（6 个独立接口）
  - Infrastructure 仓储实现类拆分为 6 个独立 Read/Write 类
  - 共用代码提取到 `Repositories/Shared/`（RepositoryHelper、ItemRow、SkillRow）
  - DI 注册直接指向独立实现类
  - 删除旧单体 `ItemRepository.cs` 与聚合接口 `IItemRepository.cs`

### 4. 重构提取流程为"提取器 + 导入用例"两段式
- 状态：**已完成**（CLI + UI）

### 5. 拆分 ViewModel，迁移格式化逻辑到服务层
- 状态：**格式化下沉已完成，ViewModel 物理拆分评估后延后**
- 已完成：
  - `ItemDetailFormattingHelper`（Application）已承接全部格式化逻辑
  - MainViewModel 不再包含任何格式化业务逻辑（~500 行纯 UI 绑定 + 命令）
- 延后原因：
  - MainViewModel 经过格式化下沉后仅剩 ~500 行，职责已明确为"搜索/详情/提取"的 UI 状态管理
  - XAML 有 40+ 直接绑定 + BindingProxy 代理绑定 + code-behind 交互
  - 拆分为子 ViewModel 需修改全部 XAML 绑定路径，改动量大、UI 回归风险高
  - 当前单文件体量可控，建议后续视功能扩展需求再拆分

### 6. 引入迁移执行器，替换 SafeAddColumn 风格
- 状态：**已完成**
- 已完成：
  - 新增 `IDbMigration` 接口（Version + Description + ExecuteAsync）
  - 新增 `MigrationRunner`（管理 `_migrations` 表，按编号顺序执行未执行的迁移）
  - 将原有 7 次 `SafeAddColumnAsync` 转化为 6 个编号迁移类（Migration001 ~ Migration006）
  - `DatabaseBootstrapper` 重构为：创建基础 Schema → 调用 `MigrationRunner.RunAsync()`
  - DI 注册所有迁移类 + MigrationRunner
  - 删除 `SafeAddColumnAsync` 方法
  - 新增 `_migrations` 表自动记录已执行的迁移版本

### 7. 全量测试 + CLI 验证 + 文档更新
- 状态：部分完成
- 已完成：
  - `dotnet build MapleItemDB.sln`：通过（0 error, 0 warning）
  - CLI 验证已执行：`stats` / `search` / `skill` / `get`
  - 本文件已更新
- 未完成：
  - 尚未新增自动化测试用例（仅执行命令行验证）
  - UI 功能需人工验证

## 当前编译状态（最近一次）
- 命令：`dotnet build MapleItemDB.sln`
- 结果：成功
- 摘要：0 错误，0 警告

## 本轮改动摘要（2026-02-26 #3 — 迁移机制重构）

### 迁移机制要点
1. **新增 3 个文件**：
   - `Database/Migrations/IDbMigration.cs` — 迁移接口（Version/Description/ExecuteAsync）
   - `Database/Migrations/MigrationRunner.cs` — 执行器（_migrations 表管理 + 按序执行）
   - `Database/Migrations/AllMigrations.cs` — 6 个迁移实现类
2. **重构 1 个文件**：
   - `Database/DatabaseBootstrapper.cs` — 移除 `SafeAddColumnAsync`，改为注入 `MigrationRunner` 并调用 `RunAsync()`
3. **DI 变更**：
   - 注册 6 个 `IDbMigration` 实现 + `MigrationRunner`
4. **迁移清单**：
   | 编号 | 描述 |
   |------|------|
   | 001 | dim_items 新增 setitem_id 列 |
   | 002 | dim_items 新增 icon_data/preview_data BLOB 列 |
   | 003 | dim_skills 新增 skill_h/common_props 列 |
   | 004 | dim_items 新增 sn 列 |
   | 005 | dim_items 新增 time_limited 列 |
   | 006 | dim_items 新增 req_job 列 |

## 本轮 CLI 验证记录（迁移机制重构后）

### 1) 数据库统计
- 命令：`dotnet run --project src/MapleItemDB.Cli -- stats`
- 关键输出：`totalItems: 83967, totalSetItems: 767, totalSkills: 3917`
- 结论：通过

## 历史改动摘要

### 2026-02-26 #2 — 仓储实现拆分
- 新增 9 个文件（6 仓储 + 3 共享）
- 删除 2 个文件（ItemRepository.cs + IItemRepository.cs）
- DI 直接绑定独立实现类

### 2026-02-26 #1 — UI/CLI 切换用例
- MainViewModel 构造函数改为注入 5 个用例接口
- 删除 ~300 行格式化代码
- CLI 全部切换为 Application 用例

## 验收标准对照

| 标准 | 状态 |
|------|------|
| UI/CLI 不再直接依赖具体仓储实现类 | **达成** |
| MainViewModel 不再承担提取导入编排 | **达成** |
| 仓储接口无"读写全能"单体接口 | **达成**（IItemRepository 已删除） |
| 新增迁移机制可重复执行且无副作用 | **达成**（_migrations 表幂等记录） |

## 当前目录结构

```
src/MapleItemDB.Infrastructure/
├── Repositories/
│   ├── Read/
│   │   ├── ItemReadRepository.cs
│   │   ├── SetItemReadRepository.cs
│   │   └── SkillReadRepository.cs
│   ├── Write/
│   │   ├── ItemWriteRepository.cs
│   │   ├── SetItemWriteRepository.cs
│   │   └── SkillWriteRepository.cs
│   └── Shared/
│       ├── RepositoryHelper.cs
│       ├── ItemRow.cs
│       └── SkillRow.cs
├── Database/
│   ├── SqliteConnectionFactory.cs
│   ├── DatabaseBootstrapper.cs
│   └── Migrations/
│       ├── IDbMigration.cs
│       ├── MigrationRunner.cs
│       └── AllMigrations.cs
└── Cache/
    └── InMemorySearchIndex.cs
```

## 下一步执行清单（建议顺序）
1. 补充自动化测试（用例层单元测试 + 仓储集成测试）。
2. UI 人工验证。
3. （可选）MainViewModel 物理拆分为子 ViewModel — 视后续功能扩展需求。

## 交接提示
- 本文件用于"断点续做"。
- 重构核心步骤（1-4、6）均已完成。
- 恢复执行时，建议从"下一步执行清单"第 1 条（自动化测试）继续。
