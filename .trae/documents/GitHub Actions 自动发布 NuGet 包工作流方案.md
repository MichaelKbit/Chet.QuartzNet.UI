# GitHub Actions 自动发布 NuGet 包工作流方案

## 任务概述
创建一个 GitHub Actions 工作流，实现自动构建前端项目并发布 NuGet 包，包括主包和数据库存储相关包。

## 工作流设计

### 触发条件
- 当创建格式为 `*.*.*` 的标签时自动构建和发布
- 支持手动触发

### 环境设置
- **.NET 版本**: 8.0
- **Node.js 版本**: 20.x
- **包管理器**: pnpm

### 核心步骤

#### 1. 代码检查与依赖安装
- 检出代码仓库
- 设置 .NET 环境
- 设置 Node.js 环境
- 安装 pnpm

#### 2. 前端项目构建
- 进入 `src/Chet.QuartzNet.Web/apps/web-antd` 目录
- 安装前端依赖
- 执行 `pnpm build`，将构建结果输出到指定目录（vite会自动清空输出目录）

#### 3. NuGet 包版本管理
- 从 GitHub 标签或环境变量获取版本号
- 更新 `Chet.QuartzNet.UI.nuspec` 文件中的版本号

#### 4. 主包打包
- 构建 `src/Chet.QuartzNet.UI` 项目
- 使用 `dotnet pack` 命令打包

#### 5. 数据库存储包打包
- 构建并打包以下项目：
  - `src/Chet.QuartzNet.EFCore.MySql`
  - `src/Chet.QuartzNet.EFCore.PostgreSql`
  - `src/Chet.QuartzNet.EFCore.SQLite`
  - `src/Chet.QuartzNet.EFCore.SqlServer`

#### 6. NuGet 包发布
- 配置 NuGet 源
- 使用 API 密钥发布所有打包好的 NuGet 包

## 工作流文件内容

```yaml
name: Build and Publish NuGet Packages

on:
  push:
    tags:
      - '*.*.*'
  workflow_dispatch:
    inputs:
      version:
        description: 'NuGet Package Version'
        required: true
        default: '1.0.0'

jobs:
  build-and-publish:
    runs-on: ubuntu-latest
    permissions:
      contents: read
    steps:
      # 1. 代码检查与依赖安装
      - name: Checkout code
        uses: actions/checkout@v4

      - name: Setup .NET 8.0
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: 8.0.x

      - name: Setup Node.js 20
        uses: actions/setup-node@v4
        with:
          node-version: 20

      - name: Install pnpm
        uses: pnpm/action-setup@v4
        with:
          version: latest

      - name: Cache pnpm dependencies
        uses: actions/cache@v4
        with:
          path: ~/.pnpm-store
          key: ${{ runner.os }}-pnpm-${{ hashFiles('**/pnpm-lock.yaml') }}
          restore-keys: |
            ${{ runner.os }}-pnpm-

      # 2. 前端项目构建
      - name: Install frontend dependencies (root)
        working-directory: src/Chet.QuartzNet.Web
        run: pnpm install --no-frozen-lockfile

      - name: Generate stubs for workspace packages
        working-directory: src/Chet.QuartzNet.Web
        run: pnpm -r run stub --if-present

      - name: Build frontend project
        working-directory: src/Chet.QuartzNet.Web
        run: pnpm run build:antd

      # 3. NuGet 包版本管理
      - name: Extract version from tag or input
        id: extract-version
        run: |
          # 从标签或输入获取版本号
          if [[ $GITHUB_REF == refs/tags/* ]]; then
            VERSION=${GITHUB_REF#refs/tags/}
          else
            VERSION=${{ github.event.inputs.version }}
          fi
          # 移除版本号中的 'v' 前缀（如果存在）
          VERSION=${VERSION#v}
          echo "VERSION=$VERSION" >> $GITHUB_ENV
          echo "VERSION=$VERSION" >> $GITHUB_OUTPUT

      - name: Update nuspec version
        run: |
          # 更新根目录下的nuspec文件
          sed -i "s/<version>.*<\/version>/<version>$VERSION<\/version>/" ./Chet.QuartzNet.UI.nuspec

      # 4. 主包打包
      - name: Build main package
        run: dotnet build src/Chet.QuartzNet.UI/Chet.QuartzNet.UI.csproj --configuration Release -p:PackageVersion=$VERSION

      - name: Pack main package
        run: dotnet pack src/Chet.QuartzNet.UI/Chet.QuartzNet.UI.csproj --configuration Release --no-build --output ./artifacts -p:PackageVersion=$VERSION

      # 5. 数据库存储包打包
      - name: Build MySQL package
        run: dotnet build src/Chet.QuartzNet.EFCore.MySql/Chet.QuartzNet.EFCore.MySQL.csproj --configuration Release -p:PackageVersion=$VERSION
      - name: Pack MySQL package
        run: dotnet pack src/Chet.QuartzNet.EFCore.MySql/Chet.QuartzNet.EFCore.MySQL.csproj --configuration Release --output ./artifacts -p:PackageVersion=$VERSION
      
      - name: Build PostgreSQL package
        run: dotnet build src/Chet.QuartzNet.EFCore.PostgreSql/Chet.QuartzNet.EFCore.PostgreSQL.csproj --configuration Release -p:PackageVersion=$VERSION
      - name: Pack PostgreSQL package
        run: dotnet pack src/Chet.QuartzNet.EFCore.PostgreSql/Chet.QuartzNet.EFCore.PostgreSQL.csproj --configuration Release --output ./artifacts -p:PackageVersion=$VERSION
      
      - name: Build SQLite package
        run: dotnet build src/Chet.QuartzNet.EFCore.SQLite/Chet.QuartzNet.EFCore.SQLite.csproj --configuration Release -p:PackageVersion=$VERSION
      - name: Pack SQLite package
        run: dotnet pack src/Chet.QuartzNet.EFCore.SQLite/Chet.QuartzNet.EFCore.SQLite.csproj --configuration Release --output ./artifacts -p:PackageVersion=$VERSION
      
      - name: Build SqlServer package
        run: dotnet build src/Chet.QuartzNet.EFCore.SqlServer/Chet.QuartzNet.EFCore.SqlServer.csproj --configuration Release -p:PackageVersion=$VERSION
      - name: Pack SqlServer package
        run: dotnet pack src/Chet.QuartzNet.EFCore.SqlServer/Chet.QuartzNet.EFCore.SqlServer.csproj --configuration Release --output ./artifacts -p:PackageVersion=$VERSION

      # 6. NuGet 包发布 (使用API Key)
      - name: Publish NuGet packages
        run: dotnet nuget push ./artifacts/*.nupkg --source https://api.nuget.org/v3/index.json --api-key ${{ secrets.NUGET_API_KEY }} --skip-duplicate
```

## 配置说明

### 工作流触发
- **自动触发**: 当创建格式为 `*.*.*` 或 `v*.*.*` 的标签时
- **手动触发**: 支持通过 GitHub Actions 界面手动触发，并指定版本号

### 版本号管理
- 优先从 GitHub 标签中提取版本号（如 `v1.2.3` 提取为 `1.2.3`）
- 手动触发时可自定义版本号
- 自动更新 `Chet.QuartzNet.UI.nuspec` 文件中的版本号

### 如何给 main 分支打标签

#### 方法一：使用 GitHub 网页界面

1. **进入仓库页面**
   - 登录 GitHub，进入项目仓库
   - 点击顶部导航栏中的 `Releases` 标签

2. **创建新版本**
   - 点击 `Create a new release` 按钮
   - 在 `Choose a tag` 下拉菜单中，输入新标签名称（格式：`v*.*.*`，如 `v1.2.3`）
   - 确保 `Target` 选择为 `main` 分支

3. **填写发布信息**
   - 在 `Release title` 中填写版本标题（建议与标签名一致或更具描述性）
   - 可选：在 `Describe this release` 中填写版本说明，包括新增功能、修复的bug等
   - 可选：勾选 `Set as the latest release` 标记为最新版本

4. **发布版本**
   - 点击 `Publish release` 按钮
   - 发布成功后，GitHub Actions 工作流会自动触发

#### 方法二：使用 Git 命令行

1. **克隆仓库（如果未克隆）**
   ```bash
   git clone https://github.com/qiect/Chet.QuartzNet.UI.git
   cd Chet.QuartzNet.UI
   ```

2. **切换到 main 分支**
   ```bash
   git checkout main
   git pull origin main
   ```

3. **创建标签**
   ```bash
   git tag v1.2.3
   ```

4. **推送标签到远程仓库**
   ```bash
   git push origin v1.2.3
   ```
   推送成功后，GitHub Actions 工作流会自动触发

#### 标签命名规范
- 标签格式：`v*.*.*`（如 `v1.0.0`、`v1.2.3`、`v2.0.0-beta.1`）
- 建议遵循 [语义化版本规范](https://semver.org/)
- 主要版本号：不兼容的 API 变更
- 次要版本号：向下兼容的功能性新增
- 修订号：向下兼容的问题修正

#### 注意事项
- 标签一旦创建并推送，会自动触发 GitHub Actions 工作流
- 建议在创建标签前，确保 main 分支包含所有需要发布的更改
- 标签创建后不可修改，如需修改需删除原标签并重新创建
- 每个标签对应一个特定版本的代码快照，便于版本回溯和管理

### NuGet API Key 配置

#### 优势
- **配置简单**: 无需复杂的 OIDC 配置
- **兼容性好**: 支持所有 NuGet 服务器版本
- **使用广泛**: 大多数开发者熟悉 API Key 认证方式

#### 配置步骤

##### 1. 获取 NuGet API Key
1. 登录 [NuGet.org](https://www.nuget.org/)
2. 点击右上角头像，选择 `Account Settings`
3. 在左侧导航栏中选择 `API Keys`
4. 点击 `Create` 按钮
5. 配置 API Key：
   - **Name**: 输入一个易于识别的名称（如 `Chet.QuartzNet.UI GitHub Actions`）
   - **Select Scopes**: 选择 `Push` 权限
   - **Select Packages**: 选择要授权的包（或选择 `*` 表示所有包）
   - **Expires In**: 可选设置过期时间（建议定期更换 API Key）
6. 点击 `Create` 按钮
7. **复制生成的 API Key**（此 Key 只会显示一次，务必保存好）

##### 2. 在 GitHub 上配置 API Key
1. 进入 GitHub 仓库
2. 点击 `Settings` 标签页
3. 在左侧导航栏中选择 `Secrets and variables` > `Actions`
4. 点击 `New repository secret` 按钮
5. 配置 Secret：
   - **Name**: 输入 `NUGET_API_KEY`（必须与工作流文件中的变量名一致）
   - **Secret**: 粘贴刚才复制的 NuGet API Key
6. 点击 `Add secret` 按钮

##### 3. 工作流配置说明
- 工作流文件中已包含 API Key 引用：`${{ secrets.NUGET_API_KEY }}`
- 发布命令格式：`dotnet nuget push ./artifacts/*.nupkg --source https://api.nuget.org/v3/index.json --api-key ${{ secrets.NUGET_API_KEY }} --skip-duplicate`
- 无需额外权限配置，`contents: read` 权限已足够

#### 安全最佳实践
- **定期更换 API Key**: 建议每 3-6 个月更换一次 API Key
- **最小权限原则**: 只授予必要的 `Push` 权限，不授予 `Manage Packages` 等高级权限
- **避免共享 API Key**: 每个项目或用途使用独立的 API Key
- **定期审查**: 定期检查 NuGet.org 上的 API Key 列表，删除不再使用的 Key
- **使用 GitHub Environments**: 对于多环境部署，建议使用 GitHub Environments 管理不同环境的 API Key

### 注意事项
- 确保 API Key 具有发布目标包的权限
- 首次发布新包时，API Key 需要有 `Push` 和 `Unlist` 权限
- API Key 是敏感信息，务必妥善保管，不要泄露到代码或日志中
- 如果 API Key 泄露，立即在 NuGet.org 上删除并重新生成
- 发布命令中的 `--skip-duplicate` 参数用于跳过已存在的版本，避免发布失败

## 预期效果

1. **前端构建**: 成功将前端项目构建到指定目录
2. **包打包**: 生成所有必要的 NuGet 包
3. **自动发布**: 成功将 NuGet 包发布到 NuGet.org
4. **版本一致**: 所有包使用相同的版本号
5. **支持手动触发**: 允许手动指定版本号进行发布

## 后续扩展建议

1. **添加测试步骤**: 在打包前运行单元测试，确保代码质量
2. **多环境支持**: 区分预览版和正式版发布
3. **发布前验证**: 添加 NuGet 包验证步骤
4. **通知机制**: 发布成功后发送通知
5. **分支管理**: 支持不同分支发布不同版本

## 注意事项

1. 前端项目的构建输出目录需与主项目的静态资源目录匹配
2. 版本号格式需符合语义化版本规范
3. 首次运行时确保已在 GitHub Secrets 中配置了有效的 `NUGET_API_KEY`
4. 建议先在测试环境中验证工作流的正确性
5. 确保 NuGet API Key 具有发布目标包的权限
6. 工作流文件必须位于 `.github/workflows/` 目录下
7. 定期更换 NuGet API Key 以提高安全性

这个工作流方案完全符合用户的需求，能够自动化完成从前端构建到 NuGet 包发布的整个流程，提高开发效率并确保发布的一致性。