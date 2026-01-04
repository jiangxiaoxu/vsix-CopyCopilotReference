# CopyCopilotReference / CopyCopilotReference

## 简介 / Overview

这是一个 Visual Studio 扩展，用于把文件路径（以及选中行范围）转换为 Copilot 可用的引用格式，并复制到剪贴板。
This is a Visual Studio extension that converts file paths (and selected line ranges) into Copilot-friendly reference format and copies to the clipboard.

## 功能 / Features

- 右键文档标签页复制引用：`#file:'路径' `
- 右键文档标签页弹窗选择“所有打开的文件”引用（显示全部，窗口一次显示 10 个并可滚动，默认不选中）
- 编辑器中选中文本后，右键复制带行号的引用
- 每个引用末尾都会附带一个空格，便于直接拼接

- Copy reference from document tab context menu: `#file:'path' `
- Pick references for open tabs from a dialog in document tab context menu (shows all, 10 visible with scroll, unchecked by default)
- Copy reference with line range from editor selection context menu
- A trailing space is appended after each reference for easy concatenation

## 使用方式 / Usage

### 文档标签页右键 / Document Tab Context Menu

- `Copy Copilot References`
  - 复制当前上下文中的文件路径引用
  - 如果在解决方案资源管理器中多选文件，会复制多选的文件路径

- `Copy Copilot References (All Open Tabs)`
  - 弹出对话框显示当前打开的所有 tab（从左到右）
  - 窗口一次显示 15 个，可上下滚动；默认不选中
  - 窗口宽度会尽量容纳最长路径
  - 勾选后点击确定才复制；关闭窗口不执行复制

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
