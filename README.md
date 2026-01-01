# CopyCopilotReference / CopyCopilotReference

## 简介 / Overview

这是一个 Visual Studio 扩展，用于把文件路径（以及选中行范围）转换为 Copilot 可用的引用格式，并复制到剪贴板。
This is a Visual Studio extension that converts file paths (and selected line ranges) into Copilot-friendly reference format and copies to the clipboard.

## 功能 / Features

- 右键文档标签页复制引用：`#file:'路径' `
- 右键文档标签页复制“所有打开的文件”引用
- 编辑器中选中文本后，右键复制带行号的引用
- 每个引用末尾都会附带一个空格，便于直接拼接

- Copy reference from document tab context menu: `#file:'path' `
- Copy references for all open tabs from document tab context menu
- Copy reference with line range from editor selection context menu
- A trailing space is appended after each reference for easy concatenation

## 使用方式 / Usage

### 文档标签页右键 / Document Tab Context Menu

- `Copy Copilot References`
  - 复制当前上下文中的文件路径引用
  - 如果在解决方案资源管理器中多选文件，会复制多选的文件路径

- `Copy Copilot References (All Open Tabs)`
  - 复制当前所有已打开文档的路径

### 编辑器右键 / Editor Context Menu

- `Copy Copilot Reference (Selected Lines)`
  - 仅当**有选中行**时显示
  - 单行：`#file:'路径':2 `
  - 多行：`#file:'路径':2-8 `

## 示例 / Examples

```
#file:'G:\source\CopyCopilotReference\CopyCopilotReferencePackage.cs' 
#file:'G:\source\CopyCopilotReference\CopyCopilotReferencePackage.cs':2-8 
```

## 构建与打包 / Build & Package

- VS 2022 (17.x) 或更高版本
- 在 Visual Studio 中切换到 Release 并 Build
- 生成的 VSIX 在：`bin\Release\CopyCopilotReference.vsix`

命令行构建（可选）：
```
msbuild CopyCopilotReference.csproj /t:Rebuild /p:Configuration=Release
```

## 兼容性 / Compatibility

- Visual Studio 2022+ (17.x / 18.x)
- 支持 Community / Pro / Enterprise（amd64）

## 注意事项 / Notes

- VS 不会公开“多选文档标签页”的完整集合，所以该场景无法精确获取；可以使用“所有打开的文件”或在解决方案资源管理器多选文件。
- `Copy Copilot Reference (Selected Lines)` 只有在编辑器有选中时才显示。

---

如需反馈或建议，欢迎提 Issue。
Feel free to open an issue for feedback or suggestions.
