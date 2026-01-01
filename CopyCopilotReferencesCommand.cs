using EnvDTE;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CopyCopilotReference
{
    internal sealed class CopyCopilotReferencesCommand
    {
        public const int CommandId = 0x0100;
        public const int AllOpenTabsCommandId = 0x0101;
        public static readonly Guid CommandSet = new Guid("e5a7ccbd-56db-4f30-9c9f-01f7c52c1f6a");

        private readonly AsyncPackage package;

        private CopyCopilotReferencesCommand(AsyncPackage package, OleMenuCommandService commandService)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            this.package = package ?? throw new ArgumentNullException(nameof(package));

            AddMenuCommand(commandService, CommandId, ExecuteSelected);
            AddMenuCommand(commandService, AllOpenTabsCommandId, ExecuteAllOpenTabs);
        }

        private void AddMenuCommand(OleMenuCommandService commandService, int commandId, EventHandler handler)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var menuCommandId = new CommandID(CommandSet, commandId);
            var menuItem = new OleMenuCommand(handler, menuCommandId);
            menuItem.BeforeQueryStatus += BeforeQueryStatus;
            commandService.AddCommand(menuItem);
        }

        public static async Task InitializeAsync(AsyncPackage package)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            var commandService = await package.GetServiceAsync(typeof(IMenuCommandService)).ConfigureAwait(true) as OleMenuCommandService;
            if (commandService == null)
            {
                return;
            }

            new CopyCopilotReferencesCommand(package, commandService);
        }

        private void BeforeQueryStatus(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var cmd = sender as OleMenuCommand;
            if (cmd == null)
            {
                return;
            }

            List<string> paths;
            if (cmd.CommandID.ID == AllOpenTabsCommandId)
            {
                paths = GetAllOpenDocumentPaths();
                cmd.Text = "Copy Copilot References (All Open Tabs)";
            }
            else
            {
                paths = GetSelectedDocumentPaths();
                cmd.Text = paths.Count <= 1
                    ? "Copy Copilot Reference"
                    : string.Format(CultureInfo.CurrentCulture, "Copy Copilot References ({0})", paths.Count);
            }

            cmd.Visible = true;
            cmd.Enabled = paths.Count > 0;
        }

        private void ExecuteSelected(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var paths = GetSelectedDocumentPaths();
            if (paths.Count == 0)
            {
                return;
            }

            var text = string.Concat(paths.Select(p => "#file:'" + p + "' "));
            Clipboard.SetText(text);
        }

        private void ExecuteAllOpenTabs(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var paths = GetAllOpenDocumentPaths();
            if (paths.Count == 0)
            {
                return;
            }

            var text = string.Concat(paths.Select(p => "#file:'" + p + "' "));
            Clipboard.SetText(text);
        }

        private List<string> GetSelectedDocumentPaths()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var paths = new List<string>();

            TryAddSelectionContextPaths(paths);

            // Fallback: active document frame (tab under the selection service).
            if (paths.Count == 0)
            {
                TryAddActiveDocumentFramePath(paths);
            }

            // Fallback: active document.
            if (paths.Count == 0)
            {
                try
                {
                    var dte = Package.GetGlobalService(typeof(DTE)) as DTE;
                    var fullName = dte != null && dte.ActiveDocument != null ? dte.ActiveDocument.FullName : null;
                    if (!string.IsNullOrWhiteSpace(fullName))
                    {
                        paths.Add(fullName);
                    }
                }
                catch
                {
                }
            }

            return paths
                .Where(p => !string.IsNullOrWhiteSpace(p))
                .Where(Path.IsPathRooted)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private List<string> GetAllOpenDocumentPaths()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var paths = new List<string>();
            foreach (var frame in GetDocumentWindowFrames())
            {
                string moniker;
                if (TryGetFrameMoniker(frame, out moniker))
                {
                    paths.Add(moniker);
                }
            }

            return paths
                .Where(p => !string.IsNullOrWhiteSpace(p))
                .Where(Path.IsPathRooted)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static void TryAddSelectionContextPaths(List<string> paths)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            try
            {
                var monitorSelection = Package.GetGlobalService(typeof(SVsShellMonitorSelection)) as IVsMonitorSelection;
                if (monitorSelection == null)
                {
                    return;
                }

                IntPtr ppHier;
                uint itemid;
                IVsMultiItemSelect ppMIS;
                IntPtr ppSC;

                int hr = monitorSelection.GetCurrentSelection(out ppHier, out itemid, out ppMIS, out ppSC);
                if (!ErrorHandler.Succeeded(hr))
                {
                    return;
                }

                try
                {
                    if (ppMIS != null)
                    {
                        TryAddMultiSelectionPaths(ppMIS, paths);
                    }

                    if (paths.Count > 0)
                    {
                        return;
                    }

                    if (itemid == VSConstants.VSITEMID_NIL || itemid == VSConstants.VSITEMID_SELECTION)
                    {
                        return;
                    }

                    var hierObj = ppHier != IntPtr.Zero ? Marshal.GetObjectForIUnknown(ppHier) as IVsHierarchy : null;
                    if (hierObj == null)
                    {
                        return;
                    }

                    TryAddHierarchyItemPath(hierObj, itemid, paths);
                }
                finally
                {
                    if (ppHier != IntPtr.Zero)
                    {
                        Marshal.Release(ppHier);
                    }
                    if (ppSC != IntPtr.Zero)
                    {
                        Marshal.Release(ppSC);
                    }
                }
            }
            catch
            {
            }
        }

        private static List<IVsWindowFrame> GetDocumentWindowFrames()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var frames = new List<IVsWindowFrame>();
            IEnumWindowFrames ppenum;
            var uiShell = Package.GetGlobalService(typeof(SVsUIShell)) as IVsUIShell;
            if (uiShell == null || !ErrorHandler.Succeeded(uiShell.GetDocumentWindowEnum(out ppenum)) || ppenum == null)
            {
                return frames;
            }

            var frameArray = new IVsWindowFrame[1];
            uint fetched;
            while (ppenum.Next(1, frameArray, out fetched) == VSConstants.S_OK && fetched == 1)
            {
                if (frameArray[0] != null)
                {
                    frames.Add(frameArray[0]);
                }
            }

            return frames;
        }

        private static void TryAddActiveDocumentFramePath(List<string> paths)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            try
            {
                var monitorSelection = Package.GetGlobalService(typeof(SVsShellMonitorSelection)) as IVsMonitorSelection;
                if (monitorSelection == null)
                {
                    return;
                }

                object frameObj;
                if (!ErrorHandler.Succeeded(monitorSelection.GetCurrentElementValue((uint)VSConstants.VSSELELEMID.SEID_DocumentFrame, out frameObj)))
                {
                    return;
                }

                var frame = frameObj as IVsWindowFrame;
                if (frame == null)
                {
                    return;
                }

                string moniker;
                if (TryGetFrameMoniker(frame, out moniker))
                {
                    paths.Add(moniker);
                }
            }
            catch
            {
            }
        }

        private static void TryAddMultiSelectionPaths(IVsMultiItemSelect multiItemSelect, List<string> paths)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            try
            {
                uint itemCount;
                int singleHierarchy;
                if (!ErrorHandler.Succeeded(multiItemSelect.GetSelectionInfo(out itemCount, out singleHierarchy)) || itemCount == 0)
                {
                    return;
                }

                var items = new VSITEMSELECTION[itemCount];
                if (!ErrorHandler.Succeeded(multiItemSelect.GetSelectedItems(0, itemCount, items)))
                {
                    return;
                }

                foreach (var item in items)
                {
                    if (item.pHier == null)
                    {
                        continue;
                    }

                    TryAddHierarchyItemPath(item.pHier, item.itemid, paths);
                }
            }
            catch
            {
            }
        }

        private static void TryAddHierarchyItemPath(IVsHierarchy hierarchy, uint itemid, List<string> paths)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (hierarchy == null)
            {
                return;
            }

            string fullPath;
            if (TryGetHierarchyItemFullPath(hierarchy, itemid, out fullPath))
            {
                paths.Add(fullPath);
            }
        }

        private static bool TryGetHierarchyItemFullPath(IVsHierarchy hierarchy, uint itemid, out string fullPath)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            fullPath = null;

            if (hierarchy == null)
            {
                return false;
            }

            string canonicalName;
            if (ErrorHandler.Succeeded(hierarchy.GetCanonicalName(itemid, out canonicalName)) && !string.IsNullOrWhiteSpace(canonicalName))
            {
                fullPath = canonicalName;
                return true;
            }

            object value;
            if (ErrorHandler.Succeeded(hierarchy.GetProperty(itemid, (int)__VSHPROPID.VSHPROPID_SaveName, out value)))
            {
                var fullPathValue = value as string;
                if (!string.IsNullOrWhiteSpace(fullPathValue))
                {
                    fullPath = fullPathValue;
                    return true;
                }
            }

            if (ErrorHandler.Succeeded(hierarchy.GetProperty(itemid, (int)__VSHPROPID.VSHPROPID_ExtObject, out value)))
            {
                var projectItem = value as ProjectItem;
                var projectItemPath = TryGetProjectItemFullPath(projectItem);
                if (!string.IsNullOrWhiteSpace(projectItemPath))
                {
                    fullPath = projectItemPath;
                    return true;
                }
            }

            return false;
        }

        private static bool TryGetFrameMoniker(IVsWindowFrame frame, out string moniker)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            moniker = null;
            if (frame == null)
            {
                return false;
            }

            object monikerObj;
            if (!ErrorHandler.Succeeded(frame.GetProperty((int)__VSFPROPID.VSFPROPID_pszMkDocument, out monikerObj)))
            {
                return false;
            }

            moniker = monikerObj as string;
            return !string.IsNullOrWhiteSpace(moniker);
        }

        private static string TryGetProjectItemFullPath(ProjectItem projectItem)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (projectItem == null)
            {
                return null;
            }

            try
            {
                var prop = projectItem.Properties != null ? projectItem.Properties.Item("FullPath") : null;
                return prop != null ? prop.Value as string : null;
            }
            catch
            {
                return null;
            }
        }
    }
}
