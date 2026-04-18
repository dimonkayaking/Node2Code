using System;
using System.Collections.Generic;
using GraphProcessor;
using UnityEditor;

namespace CustomVisualScripting.Editor.Nodes.Views
{
    /// <summary>
    /// Скрывает категории Utils и Unity из меню создания нод (типы остаются в сборке).
    /// </summary>
    public sealed class FilteredCreateMenuBaseGraphView : BaseGraphView
    {
        bool _pendingPostGraphChromeFix;

        public FilteredCreateMenuBaseGraphView(EditorWindow window)
            : base(window)
        {
            // GraphProcessor после Initialize вызывает RefreshPorts() → Unity снова создаёт collapse-button.
            // Оборачиваем колбэк GraphProcessor и после каждого изменения графа подтягиваем вид нод на следующий кадр.
            var previousGraphViewChanged = graphViewChanged;
            graphViewChanged = change =>
            {
                change = previousGraphViewChanged != null ? previousGraphViewChanged(change) : change;
                SchedulePostGraphChromeFixForAllNodes();
                return change;
            };
        }

        void SchedulePostGraphChromeFixForAllNodes()
        {
            if (_pendingPostGraphChromeFix)
                return;
            _pendingPostGraphChromeFix = true;
            schedule.Execute(() =>
            {
                _pendingPostGraphChromeFix = false;
                if (nodeViews == null)
                    return;
                foreach (var nv in nodeViews)
                {
                    if (nv != null)
                        NodeViewBoundsUtils.PerformFullNodeAppearanceFix(nv);
                }
            }).ExecuteLater(1);
        }

        public override IEnumerable<(string path, Type type)> FilterCreateNodeMenuEntries()
        {
            foreach (var entry in NodeProvider.GetNodeMenuEntries(graph))
            {
                if (ShouldHideMenuPath(entry.path))
                    continue;
                yield return entry;
            }
        }

        private static bool ShouldHideMenuPath(string path)
        {
            if (string.IsNullOrEmpty(path))
                return false;

            return IsUnderCategory(path, "Utils") || IsUnderCategory(path, "Unity");
        }

        private static bool IsUnderCategory(string path, string category)
        {
            return path.StartsWith(category + "/", StringComparison.OrdinalIgnoreCase)
                   || string.Equals(path.Trim(), category, StringComparison.OrdinalIgnoreCase);
        }
    }
}
