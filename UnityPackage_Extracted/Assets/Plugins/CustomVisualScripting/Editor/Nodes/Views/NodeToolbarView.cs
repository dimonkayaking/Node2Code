using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using GraphProcessor;
using CustomVisualScripting.Editor.Nodes.Base;

namespace CustomVisualScripting.Editor.Nodes.Views
{
    public class NodeToolbarView : VisualElement
    {
        private readonly BaseGraphView _graphView;
        private readonly Dictionary<string, List<(string path, Type type)>> _categories;
        private VisualElement _categoriesPanel;
        private VisualElement _nodesPanel;
        private VisualElement _resizer;
        private VisualElement _rootParent;

        private float _panelWidth = 260f;
        private const float MinPanelWidth = 200f;
        private float _maxAllowedWidth = 500f;
        private const string PanelWidthPrefKey = "NodeToolbarView_Width";

        private static readonly Dictionary<string, Color> CategoryColors = new()
        {
            { "Literals", HexColor("#4CAF50") },
            { "Math", HexColor("#2196F3") },
            { "Comparison", HexColor("#FF9800") },
            { "Logic", HexColor("#9C27B0") },
            { "Flow", HexColor("#F44336") },
            { "Conversion", HexColor("#FFC107") },
            { "Debug", HexColor("#FFFFFF") },
            { "Unity", HexColor("#8D6E63") }
        };

        public NodeToolbarView(BaseGraphView graphView)
        {
            _graphView = graphView;
            _categories = GetCategories();

            _panelWidth = EditorPrefs.GetFloat(PanelWidthPrefKey, 260f);
            _maxAllowedWidth = 500f;
            _panelWidth = Mathf.Clamp(_panelWidth, MinPanelWidth, _maxAllowedWidth);

            style.position = Position.Absolute;
            style.right = 0;
            style.top = 0;
            style.bottom = 0;
            style.width = _panelWidth;
            style.backgroundColor = new Color(0.18f, 0.18f, 0.18f);
            style.borderLeftWidth = 1;
            style.borderLeftColor = new Color(0.3f, 0.3f, 0.3f);
            style.marginRight = 0;

            BuildUI();
            CreateResizer();
            RegisterGlobalClickHandler();

            RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
        }

        private void OnAttachToPanel(AttachToPanelEvent evt)
        {
            _rootParent = GetFirstAncestorOfType<VisualElement>();
            if (_rootParent != null)
            {
                _rootParent.RegisterCallback<GeometryChangedEvent>(OnParentGeometryChanged);
                UpdateMaxWidthFromParent();
                if (_panelWidth > _maxAllowedWidth)
                {
                    _panelWidth = _maxAllowedWidth;
                    style.width = _panelWidth;
                    EditorPrefs.SetFloat(PanelWidthPrefKey, _panelWidth);
                }
            }
        }

        private void OnParentGeometryChanged(GeometryChangedEvent evt)
        {
            UpdateMaxWidthFromParent();
            if (_panelWidth > _maxAllowedWidth)
            {
                _panelWidth = _maxAllowedWidth;
                style.width = _panelWidth;
                EditorPrefs.SetFloat(PanelWidthPrefKey, _panelWidth);
                if (_nodesPanel.style.display == DisplayStyle.Flex)
                    _nodesPanel.style.left = -_panelWidth;
            }
        }

        private void UpdateMaxWidthFromParent()
        {
            if (_rootParent != null && _rootParent.resolvedStyle.width > 0)
            {
                _maxAllowedWidth = Mathf.Min(500f, _rootParent.resolvedStyle.width - 20);
                _maxAllowedWidth = Mathf.Max(_maxAllowedWidth, MinPanelWidth);
            }
            else
            {
                _maxAllowedWidth = 500f;
            }
        }

        private void CreateResizer()
        {
            _resizer = new VisualElement();
            _resizer.style.position = Position.Absolute;
            _resizer.style.left = -5;
            _resizer.style.top = 0;
            _resizer.style.bottom = 0;
            _resizer.style.width = 10;
            _resizer.pickingMode = PickingMode.Position;

            _resizer.RegisterCallback<MouseEnterEvent>(_ => EditorUiPointerCursor.TryApply(_resizer, MouseCursor.ResizeHorizontal));
            _resizer.RegisterCallback<MouseLeaveEvent>(_ => EditorUiPointerCursor.Clear(_resizer));

            bool isResizing = false;
            float startWidth = 0f;
            float startMouseX = 0f;

            _resizer.RegisterCallback<MouseDownEvent>(evt =>
            {
                if (evt.button != 0) return;
                isResizing = true;
                startWidth = resolvedStyle.width;
                startMouseX = evt.mousePosition.x;
                _resizer.CaptureMouse();
                evt.StopPropagation();
            });

            _resizer.RegisterCallback<MouseMoveEvent>(evt =>
            {
                if (!isResizing) return;
                float delta = startMouseX - evt.mousePosition.x;
                float newWidth = startWidth + delta;
                newWidth = Mathf.Clamp(newWidth, MinPanelWidth, _maxAllowedWidth);
                if (Mathf.Abs(newWidth - resolvedStyle.width) > 0.1f)
                {
                    style.width = newWidth;
                    _panelWidth = newWidth;
                    EditorPrefs.SetFloat(PanelWidthPrefKey, _panelWidth);
                    if (_nodesPanel.style.display == DisplayStyle.Flex)
                        _nodesPanel.style.left = -_panelWidth;
                }
                evt.StopPropagation();
            });

            _resizer.RegisterCallback<MouseUpEvent>(evt =>
            {
                if (!isResizing) return;
                isResizing = false;
                _resizer.ReleaseMouse();
                evt.StopPropagation();
            });

            Add(_resizer);
        }

        private void RegisterGlobalClickHandler()
        {
            _graphView?.RegisterCallback<MouseDownEvent>(OnGlobalMouseDown, TrickleDown.TrickleDown);
        }

        private void OnGlobalMouseDown(MouseDownEvent evt)
        {
            if (_nodesPanel == null || _nodesPanel.style.display == DisplayStyle.None)
                return;

            var target = evt.target as VisualElement;
            bool clickedInside = false;
            while (target != null)
            {
                if (target == _nodesPanel || target == this)
                {
                    clickedInside = true;
                    break;
                }
                target = target.parent;
            }

            if (!clickedInside)
                HideNodesPanel();
        }

        private void BuildUI()
        {
            var title = new Label("Create Node");
            title.style.fontSize = 14;
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.color = Color.white;
            title.style.paddingTop = 10;
            title.style.paddingBottom = 10;
            title.style.paddingLeft = 12;
            title.style.unityTextAlign = TextAnchor.MiddleCenter;
            title.style.backgroundColor = new Color(0.22f, 0.22f, 0.22f);
            title.style.borderBottomWidth = 1;
            title.style.borderBottomColor = new Color(0.3f, 0.3f, 0.3f);
            Add(title);

            var scrollView = new ScrollView();
            scrollView.style.flexGrow = 1;
            scrollView.style.paddingBottom = 0;
            scrollView.style.marginBottom = 0;

            _categoriesPanel = new VisualElement();
            _categoriesPanel.style.flexDirection = FlexDirection.Column;
            _categoriesPanel.style.paddingBottom = 0;
            _categoriesPanel.style.marginBottom = 0;
            _categoriesPanel.style.paddingLeft = 5;
            _categoriesPanel.style.paddingRight = 5;
            _categoriesPanel.style.overflow = Overflow.Hidden;

            foreach (var category in _categories)
            {
                var categoryButton = CreateCategoryButton(category.Key);
                _categoriesPanel.Add(categoryButton);
            }

            scrollView.Add(_categoriesPanel);
            Add(scrollView);

            _nodesPanel = new VisualElement();
            _nodesPanel.style.position = Position.Absolute;
            _nodesPanel.style.left = -_panelWidth;
            _nodesPanel.style.top = 0;
            _nodesPanel.style.width = _panelWidth;
            _nodesPanel.style.maxHeight = Length.Percent(100);
            _nodesPanel.style.backgroundColor = new Color(0.15f, 0.15f, 0.15f);
            _nodesPanel.style.borderRightWidth = 1;
            _nodesPanel.style.borderRightColor = new Color(0.3f, 0.3f, 0.3f);
            _nodesPanel.style.display = DisplayStyle.None;
            _nodesPanel.style.flexDirection = FlexDirection.Column;
            _nodesPanel.style.paddingBottom = 0;
            _nodesPanel.style.marginBottom = 0;
            Add(_nodesPanel);
        }

        private VisualElement CreateCategoryButton(string category)
        {
            var button = new Button();
            button.text = category;
            button.style.fontSize = 13;
            button.style.paddingTop = 8;
            button.style.paddingBottom = 8;
            button.style.paddingLeft = 12;
            button.style.paddingRight = 12;
            button.style.backgroundColor = new Color(0.25f, 0.25f, 0.25f);
            button.style.borderTopWidth = 2;
            button.style.borderBottomWidth = 2;
            button.style.borderLeftWidth = 2;
            button.style.borderRightWidth = 2;
            button.style.marginLeft = 0;
            button.style.marginRight = 0;
            button.style.marginTop = 4;
            button.style.marginBottom = 4;
            button.style.whiteSpace = WhiteSpace.Normal;
            button.style.textOverflow = TextOverflow.Clip;
            button.style.alignSelf = Align.Stretch;
            button.style.flexGrow = 1;

            if (CategoryColors.TryGetValue(category, out var color))
            {
                button.style.borderTopColor = color;
                button.style.borderBottomColor = color;
                button.style.borderLeftColor = color;
                button.style.borderRightColor = color;
            }

            button.RegisterCallback<MouseEnterEvent>(_ => button.style.backgroundColor = new Color(0.35f, 0.35f, 0.35f));
            button.RegisterCallback<MouseLeaveEvent>(_ => button.style.backgroundColor = new Color(0.25f, 0.25f, 0.25f));

            button.clicked += () => ShowNodesForCategory(category);
            return button;
        }

        private void ShowNodesForCategory(string category)
        {
            _nodesPanel.Clear();
            _nodesPanel.style.display = DisplayStyle.Flex;
            _nodesPanel.style.left = -_panelWidth;
            _nodesPanel.style.paddingLeft = 5;
            _nodesPanel.style.paddingRight = 5;
            _nodesPanel.style.paddingBottom = 5;

            var backButton = new Button(() => HideNodesPanel());
            backButton.text = "← Back";
            backButton.style.fontSize = 12;
            backButton.style.paddingTop = 6;
            backButton.style.paddingBottom = 6;
            backButton.style.backgroundColor = new Color(0.22f, 0.22f, 0.22f);
            backButton.style.marginBottom = 8;
            backButton.style.alignSelf = Align.Stretch;
            backButton.style.flexGrow = 1;
            backButton.RegisterCallback<MouseEnterEvent>(_ => backButton.style.backgroundColor = new Color(0.32f, 0.32f, 0.32f));
            backButton.RegisterCallback<MouseLeaveEvent>(_ => backButton.style.backgroundColor = new Color(0.22f, 0.22f, 0.22f));
            _nodesPanel.Add(backButton);

            var title = new Label(category);
            title.style.fontSize = 13;
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.color = Color.white;
            title.style.paddingBottom = 8;
            title.style.marginBottom = 8;
            title.style.borderBottomWidth = 1;
            title.style.borderBottomColor = new Color(0.3f, 0.3f, 0.3f);
            title.style.unityTextAlign = TextAnchor.MiddleCenter;
            _nodesPanel.Add(title);

            if (_categories.TryGetValue(category, out var nodes))
            {
                Color catColor = CategoryColors.ContainsKey(category) ? CategoryColors[category] : Color.gray;
                foreach (var node in nodes.OrderBy(n => n.path))
                {
                    var nodeButton = CreateNodeButton(node.type, node.path, catColor);
                    _nodesPanel.Add(nodeButton);
                }
            }
        }

        private VisualElement CreateNodeButton(Type nodeType, string displayName, Color categoryColor)
        {
            var button = new Button();
            var shortName = displayName.Split('/').Last();
            button.text = shortName;
            button.tooltip = displayName;
            button.style.fontSize = 12;
            button.style.paddingTop = 8;
            button.style.paddingBottom = 8;
            button.style.paddingLeft = 12;
            button.style.paddingRight = 12;
            button.style.backgroundColor = new Color(0.28f, 0.28f, 0.28f);
            button.style.alignSelf = Align.Stretch;
            button.style.marginLeft = 0;
            button.style.marginRight = 0;
            button.style.marginTop = 2;
            button.style.marginBottom = 2;
            button.style.unityTextAlign = TextAnchor.MiddleCenter;
            button.style.flexGrow = 1;

            button.style.borderTopWidth = 2;
            button.style.borderBottomWidth = 2;
            button.style.borderLeftWidth = 2;
            button.style.borderRightWidth = 2;
            button.style.borderTopColor = categoryColor;
            button.style.borderBottomColor = categoryColor;
            button.style.borderLeftColor = categoryColor;
            button.style.borderRightColor = categoryColor;

            button.RegisterCallback<MouseEnterEvent>(_ => button.style.backgroundColor = new Color(0.38f, 0.38f, 0.38f));
            button.RegisterCallback<MouseLeaveEvent>(_ => button.style.backgroundColor = new Color(0.28f, 0.28f, 0.28f));

            button.clicked += () => CreateNodeWithoutOverlap(nodeType);
            return button;
        }

        private void CreateNodeWithoutOverlap(Type nodeType)
        {
            if (_graphView == null) return;

            Rect graphRect = _graphView.layout;
            float visibleWidth = graphRect.width - _panelWidth;
            float visibleCenterX = graphRect.x + visibleWidth / 2f;
            float visibleCenterY = graphRect.y + graphRect.height / 2f;
            Vector2 screenCenter = new Vector2(visibleCenterX, visibleCenterY);
#pragma warning disable 0618
            Vector2 pan = (Vector2)_graphView.viewTransform.position;
            float scale = _graphView.scale;
#pragma warning restore 0618
            Vector2 baseCenter = (screenCenter - pan) / scale;

            const float gap = 25f;
            Vector2 finalPos = FindFreePosition(baseCenter, 200, 100, gap);

            var node = (BaseNode)Activator.CreateInstance(nodeType);
            if (node == null) return;
            if (string.IsNullOrEmpty(node.GUID))
                node.GUID = Guid.NewGuid().ToString();

            node.position = new Rect(finalPos.x, finalPos.y, 200, 100);
            _graphView.AddNode(node);
            HideNodesPanel();
        }

        private Vector2 FindFreePosition(Vector2 desiredCenter, float nodeWidth, float nodeHeight, float gap)
        {
            if (_graphView == null) return desiredCenter;

            var existingNodes = _graphView.graph.nodes
                .Where(n => n != null)
                .Select(n => n.position)
                .ToList();

            bool OverlapsWithGap(Rect rect, Rect other)
            {
                Rect expanded = new Rect(rect.x - gap, rect.y - gap, rect.width + gap * 2, rect.height + gap * 2);
                return expanded.Overlaps(other);
            }

            Rect proposedRect = new Rect(desiredCenter.x - nodeWidth/2, desiredCenter.y - nodeHeight/2, nodeWidth, nodeHeight);
            if (!existingNodes.Any(r => OverlapsWithGap(proposedRect, r)))
                return desiredCenter;

            float radiusStep = 30f;
            int maxAttempts = 80;
            for (int attempt = 1; attempt <= maxAttempts; attempt++)
            {
                float radius = radiusStep * attempt;
                for (float angle = 0; angle < 360f; angle += 25f)
                {
                    float rad = angle * Mathf.Deg2Rad;
                    Vector2 offset = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)) * radius;
                    Vector2 candidate = desiredCenter + offset;
                    Rect candidateRect = new Rect(candidate.x - nodeWidth/2, candidate.y - nodeHeight/2, nodeWidth, nodeHeight);
                    if (!existingNodes.Any(rect => OverlapsWithGap(candidateRect, rect)))
                        return candidate;
                }
            }

            return desiredCenter + new Vector2(UnityEngine.Random.Range(-80f, 80f), UnityEngine.Random.Range(-80f, 80f));
        }

        private void HideNodesPanel()
        {
            _nodesPanel.style.display = DisplayStyle.None;
            _nodesPanel.Clear();
        }

        private Dictionary<string, List<(string path, Type type)>> GetCategories()
        {
            var categories = new Dictionary<string, List<(string, Type)>>();
            var menuEntries = NodeProvider.GetNodeMenuEntries(null);
            foreach (var entry in menuEntries)
            {
                if (ShouldHideMenuPath(entry.path)) continue;
                var category = entry.path.Split('/')[0];
                if (!categories.ContainsKey(category))
                    categories[category] = new List<(string, Type)>();
                categories[category].Add((entry.path, entry.type));
            }
            return categories;
        }

        private static bool ShouldHideMenuPath(string path)
        {
            if (string.IsNullOrEmpty(path)) return false;
            return path.StartsWith("Utils/") || path.StartsWith("Utils") ||
                   path.StartsWith("Unity/") || path.StartsWith("Unity");
        }

        private static Color HexColor(string hex)
        {
            ColorUtility.TryParseHtmlString(hex, out var color);
            return color;
        }
    }
}