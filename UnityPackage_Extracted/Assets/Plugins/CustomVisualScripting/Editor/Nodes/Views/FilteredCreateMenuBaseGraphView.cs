using System;
using System.Collections.Generic;
using GraphProcessor;
using UnityEditor;
using UnityEngine.UIElements;

namespace CustomVisualScripting.Editor.Nodes.Views
{
    /// <summary>
    /// Скрывает категории Utils и Unity из меню создания нод (типы остаются в сборке).
    /// </summary>
    public sealed class FilteredCreateMenuBaseGraphView : BaseGraphView
    {
        bool _pendingPostGraphChromeFix;

        /// <summary>
        /// <para>
        /// Срабатывает для каждой новой <see cref="BaseNodeView"/>, добавленной в граф.
        /// </para>
        /// <para>
        /// Unity <c>GraphView.AddElement</c> не виртуальный, а <c>GraphView.graphViewChanged</c> не вызывается
        /// при программном <c>AddElement</c> (именно так GraphProcessor добавляет ноду из меню/загрузки).
        /// Поэтому отслеживаем рост <see cref="BaseGraphView.nodeViews"/> планировщиком каждый кадр.
        /// </para>
        /// </summary>
        public event Action<BaseNodeView> NodeViewAdded;

        readonly HashSet<BaseNodeView> _trackedNodeViews = new();

        public FilteredCreateMenuBaseGraphView(EditorWindow window)
            : base(window)
        {
            var previousGraphViewChanged = graphViewChanged;
            graphViewChanged = change =>
            {
                change = previousGraphViewChanged != null ? previousGraphViewChanged(change) : change;
                SchedulePostGraphChromeFixForAllNodes();
                return change;
            };

            schedule.Execute(PollForNewNodeViews).Every(16);

            // Перехватываем любой клик на GraphView (включая клик по пустому пространству,
            // который снимает выделение) и обновляем цвет рамок всех нод после того,
            // как Unity успеет изменить свойство selected.
            RegisterCallback<PointerDownEvent>(_ => ScheduleOutlineRefreshForAllNodes(), TrickleDown.TrickleDown);
        }

        bool _pendingOutlineRefresh;

        void ScheduleOutlineRefreshForAllNodes()
        {
            if (_pendingOutlineRefresh)
                return;
            _pendingOutlineRefresh = true;
            schedule.Execute(() =>
            {
                _pendingOutlineRefresh = false;
                if (nodeViews == null)
                    return;
                foreach (var nv in nodeViews)
                {
                    if (nv != null)
                        NodeViewBoundsUtils.RefreshNodeOutlineColor(nv);
                }
            // 80 мс — достаточно, чтобы Unity обновил selected до нашего чтения.
            }).ExecuteLater(80);
        }

        void PollForNewNodeViews()
        {
            if (nodeViews == null || nodeViews.Count == 0)
                return;

            for (int i = 0; i < nodeViews.Count; i++)
            {
                var nv = nodeViews[i];
                if (nv == null)
                    continue;
                if (!_trackedNodeViews.Add(nv))
                    continue;

                NodeViewBoundsUtils.PerformFullNodeAppearanceFix(nv);
                NodeViewAdded?.Invoke(nv);

                var captured = nv;
                captured.schedule.Execute(() =>
                    NodeViewBoundsUtils.PerformFullNodeAppearanceFix(captured)).ExecuteLater(0);
                captured.schedule.Execute(() =>
                    NodeViewBoundsUtils.PerformFullNodeAppearanceFix(captured)).ExecuteLater(2);
            }

            _trackedNodeViews.RemoveWhere(nv => nv == null || nv.panel == null);
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
