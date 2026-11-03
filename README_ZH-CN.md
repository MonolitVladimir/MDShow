# MD Show 1.0 — 使用指南

[语言索引](README.md) · [English](README_EN.md) · [Русский](README_RU.md) · [Deutsch](README_DE.md) · [العربية](README_AR.md)

## 功能与要求

适用于 **Windows 11 x64** 的轻量 Markdown 阅读器和编辑器。支持阅读、编辑和
双栏实时预览、标签页、大纲、最近文件、格式工具、列表续写、撤销/重做及纯文本
查找替换。支持 GFM 表格、任务列表、删除线、代码和本地图片，可导出 HTML/PDF
及预览打印。不含 Mermaid、LaTeX 或插件；Windows 10 不在支持范围内。

需要 .NET Framework 4.8 和**系统 Microsoft Edge WebView2 Evergreen**。
不捆绑浏览器引擎、Electron 或 Node 运行时，不需要账号，不上传文档。
缺少 WebView2 时请使用 Microsoft 官方安装包。

## 三种安装包

| 文件 | 用途 |
|---|---|
| `MDShow-1.0-Setup-x64.exe` | 为所有用户安装到 Program Files，需要管理员权限；五种界面语言、打开方式、默认应用及可选的传统资源管理器菜单 |
| `MDShow-1.0-portable-x64.zip` | 完整解压后运行 `MDShow.exe`；五种界面语言 |
| `MDShow-1.0-portable-en-x64.zip` | 文档功能相同，仅包含英文界面资源 |

English-only 仍支持中文、俄文、阿拉伯文和混合 Unicode 文档，并保留完整版的
语言设置。系统对话框可能使用 Windows 语言。ZIP 不注册文件关联，也不包含
WebView2 运行时。

## 安装与卸载

Setup 为所有用户安装到 `C:\Program Files\MD Show`，并请求管理员权限。
ZIP 可直接解压到可写文件夹。在 Windows“打开方式”或默认应用中选择 MD Show；
安装程序不强制修改受保护的 `UserChoice`。传统菜单命令可能位于“显示更多选项”。

卸载前请关闭所有 Windows 会话中的 MD Show。安装程序使用文件锁及
Restart Manager 检查，不会终止正在运行的进程。卸载删除程序文件、注册项及
已确认的当前会话帐户的 Installed 配置、缓存和恢复草稿。清理在非管理员权限下
进行。UAC 使用其他帐户时，所有配置会保留并显示提示。安全清理失败时，
卸载在删除程序文件前停止；此时部分缓存可能已经删除。已保存的
Markdown 文档、便携版配置、旧共享文件及其他用户的数据均保留。请先保存重要
草稿。

顶部依次为图标、**文件**、保存、搜索、三种视图和缩放。文件菜单按新建/打开、
保存、导出/打印、关闭分组；设置包含主题、自动换行、语言和目录。
Alt+F 打开菜单，方向键移动焦点，Escape 关闭菜单。

## 配置与隐私

各版本的设置、最近路径、恢复草稿和 WebView2 缓存独立存储：

- 安装版：`%LOCALAPPDATA%\MDShow\Installed`
- 完整便携版：`%LOCALAPPDATA%\MDShow\PortableFull`
- 英文便携版：`%LOCALAPPDATA%\MDShow\PortableEnglish`

三个版本可以同时运行。IPC 按版本隔离；文件锁防止多个会话同时写入同一配置。
便携表示无需安装，并非配置保存在 EXE 旁。程序目录可以是只读目录。


旧的共享 `%LOCALAPPDATA%\MDShow` 文件不会自动导入或删除。如需手动迁移，
请关闭所有版本，备份旧配置及新配置，然后仅将 `settings.json` 复制到所选
的新目录。英文版仍使用英文界面。请先用旧程序将恢复草稿保存为普通 Markdown
文件，再用新程序打开；不要覆盖新的恢复文件。无需迁移缓存。如果启动被阻止，
请等待安装或卸载结束，或关闭另一会话中使用同一配置的实例。安装损坏时可重新
运行 Setup 进行修复。

草稿约每十秒写入**未加密 JSON**，由 Windows 用户账户权限保护。
崩溃后可选择恢复；它不能代替独立备份。文件被其他程序修改时会提示冲突，
取消可保留草稿和外部文件。原子写入可防止替换失败，但不能与其他写入程序
形成共同事务。

## 外观、语言与编码

完整版界面语言：英语、俄语、德语、阿拉伯语、简体中文。自动模式跟随 Windows，
不支持时回退到英语；手动选择会保存。阿拉伯语界面从右向左，文档方向独立。
代码、URL、路径保持从左向右，中文使用系统后备字体。
支持系统、浅色、深色主题，文档缩放 **50–300%**，通过工具栏或 Ctrl+滚轮操作。
双栏宽度会保存。真实中文 IME 输入及其他 Windows DPI 设置仍需按发布说明手动验证。

支持带/不带 BOM 的 UTF-8、带 BOM 的 UTF-16/32 和 Windows-1251。
保存保留原编码、BOM 和 LF/CRLF/CR 换行。无法用原编码表示的字符，需要明确
同意转换为 UTF-8。

| 快捷键 | 操作 |
|---|---|
| Ctrl+O / N；Ctrl+S / Ctrl+Shift+S | 打开 / 新建；保存 / 另存为 |
| Ctrl+W / Tab / Shift+Tab | 关闭 / 下一个 / 上一个标签页 |
| Ctrl+1 / 2 / 3 | 编辑 / 双栏 / 阅读 |
| Ctrl+F / H；Ctrl+B / I / K | 查找 / 替换；加粗 / 斜体 / 链接 |
| Ctrl+Z / Y；Ctrl+P；F1 | 撤销 / 重做；打印；帮助 |
| Ctrl++ / − / 0；Ctrl+滚轮 | 放大 / 缩小 / 重置 |

## 限制与安全

包含大量短行的大型文档可能导致 WebView2 编辑器和辅助功能处理耗时较长。请等待加载完成。这是首版已接受的限制，参见 RELEASE-NOTES-v1.0.md。

文件上限 **16 MiB**。预览输入上限 **1 Mi 字符**，HTML 上限 **2 Mi 字符**，
另有限制复杂度和约 **1.5 秒**的工作线程超时。复杂预览可能被拒绝，编辑器仍可用。
阻止外部图片。本地图片和 Markdown 链接须位于文档目录或其子目录；拒绝已有的
子目录 junction/符号链接和不安全协议。明确点击的 http/https/mail 链接由外部程序打开。

其他本地进程仍可能在检查与打开之间替换路径（TOCTOU），或在冲突检查与保存
之间写入文件。这些限制没有被宣称消除。请更新 Windows/WebView2，并阅读
[SECURITY.md](SECURITY.md)。请通过 [GitHub 私密漏洞报告渠道](https://github.com/MonolitVladimir/MDShow/security) 提交安全问题；
不要在公开 issue 中发布私人文档或漏洞利用详情。

EXE/Setup **未签名**，可能触发 SmartScreen 提示；不要绕过保护。
将 SHA-256 与单独提供的校验文件比较：

```powershell
Get-FileHash .\MDShow-1.0-portable-x64.zip -Algorithm SHA256
```

## 从源代码构建

需要 Windows 11 x64、.NET Framework 4.8 编译器和 PowerShell 7。
Bootstrap 下载并验证 WebView2 SDK 和 Inno Setup。

```powershell
pwsh -File scripts/bootstrap.ps1 -Installer
pwsh -File scripts/build.ps1 -Installer
```

输出文件位于 `dist/`；不保证构建结果逐位一致。
图标可选使用 `scripts/make-icon.ps1` 配合 Python/Pillow 12.2.0 重新生成。

## 许可与开发者

原创代码和文档采用 [MIT](LICENSE)。
依赖库保留[各自许可](THIRD-PARTY-NOTICES.txt)。标志使用
[独立授权](BRANDING.md)；MIT 不授予品牌权利。
Copyright (c) 2026 **ООО «Агентство «МОНОЛИТ»**。
**MONOLIT Agency LLC** 是英文显示译名，并非已核实的另一个注册法律名称。
参见[权属说明](COPYRIGHT.md)。

当前要求和已知限制参见[发布说明](RELEASE-NOTES-v1.0.md)。
