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
        private BaseGraphView _graphView;
        private readonly Dictionary<string, List<(string path, Type type)>> _categories;
        private VisualElement _contentContainer;

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

            style.backgroundColor = new Color(0.18f, 0.18f, 0.18f);
            style.borderLeftWidth = 1;
            style.borderLeftColor = new Color(0.3f, 0.3f, 0.3f);
            style.flexDirection = FlexDirection.Column;
            style.paddingBottom = 0;
            style.marginBottom = 0;

            BuildUI();
            ShowCategories();
        }

        public void UpdateGraphView(BaseGraphView newGraphView)
        {
            _graphView = newGraphView;
            ShowCategories();
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
            title.style.whiteSpace = WhiteSpace.NoWrap;          // запрет переноса
            title.style.textOverflow = TextOverflow.Ellipsis;    // многоточие
            title.style.overflow = Overflow.Hidden;
            Add(title);

            var scrollView = new ScrollView();
            scrollView.style.flexGrow = 1;
            scrollView.style.paddingBottom = 0;
            scrollView.style.marginBottom = 0;

            _contentContainer = new VisualElement();
            _contentContainer.style.flexDirection = FlexDirection.Column;
            _contentContainer.style.paddingBottom = 0;
            _contentContainer.style.marginBottom = 0;
            _contentContainer.style.paddingLeft = 5;
            _contentContainer.style.paddingRight = 5;
            _contentContainer.style.width = Length.Percent(100);

            scrollView.Add(_contentContainer);
            Add(scrollView);
        }

        private void ShowCategories()
        {
            _contentContainer.Clear();
            foreach (var category in _categories)
            {
                var button = CreateCategoryButton(category.Key);
                _contentContainer.Add(button);
            }
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
            button.style.whiteSpace = WhiteSpace.NoWrap;
            button.style.textOverflow = TextOverflow.Ellipsis;
            button.style.overflow = Overflow.Hidden;
            button.style.alignSelf = Align.Stretch;
            button.style.flexGrow = 1;
            button.style.width = Length.Percent(100);

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
            _contentContainer.Clear();

            var backButton = new Button(ShowCategories);
            backButton.text = "← Back";
            backButton.style.fontSize = 12;
            backButton.style.paddingTop = 6;
            backButton.style.paddingBottom = 6;
            backButton.style.backgroundColor = new Color(0.22f, 0.22f, 0.22f);
            backButton.style.marginBottom = 8;
            backButton.style.alignSelf = Align.Stretch;
            backButton.style.flexGrow = 1;
            backButton.style.width = Length.Percent(100);
            backButton.style.whiteSpace = WhiteSpace.NoWrap;
            backButton.style.textOverflow = TextOverflow.Ellipsis;
            backButton.RegisterCallback<MouseEnterEvent>(_ => backButton.style.backgroundColor = new Color(0.32f, 0.32f, 0.32f));
            backButton.RegisterCallback<MouseLeaveEvent>(_ => backButton.style.backgroundColor = new Color(0.22f, 0.22f, 0.22f));
            _contentContainer.Add(backButton);

            var title = new Label(category);
            title.style.fontSize = 13;
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.color = Color.white;
            title.style.paddingBottom = 8;
            title.style.marginBottom = 8;
            title.style.borderBottomWidth = 1;
            title.style.borderBottomColor = new Color(0.3f, 0.3f, 0.3f);
            title.style.unityTextAlign = TextAnchor.MiddleCenter;
            title.style.whiteSpace = WhiteSpace.NoWrap;
            title.style.textOverflow = TextOverflow.Ellipsis;
            title.style.overflow = Overflow.Hidden;
            _contentContainer.Add(title);

            if (_categories.TryGetValue(category, out var nodes))
            {
                Color catColor = CategoryColors.ContainsKey(category) ? CategoryColors[category] : Color.gray;
                foreach (var node in nodes.OrderBy(n => n.path))
                {
                    var nodeButton = CreateNodeButton(node.type, node.path, catColor);
                    _contentContainer.Add(nodeButton);
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
            button.style.width = Length.Percent(100);
            button.style.borderTopWidth = 2;
            button.style.borderBottomWidth = 2;
            button.style.borderLeftWidth = 2;
            button.style.borderRightWidth = 2;
            button.style.borderTopColor = categoryColor;
            button.style.borderBottomColor = categoryColor;
            button.style.borderLeftColor = categoryColor;
            button.style.borderRightColor = categoryColor;
            button.style.whiteSpace = WhiteSpace.NoWrap;
            button.style.textOverflow = TextOverflow.Ellipsis;
            button.style.overflow = Overflow.Hidden;

            button.RegisterCallback<MouseEnterEvent>(_ => button.style.backgroundColor = new Color(0.38f, 0.38f, 0.38f));
            button.RegisterCallback<MouseLeaveEvent>(_ => button.style.backgroundColor = new Color(0.28f, 0.28f, 0.28f));

            button.clicked += () => CreateNodeAtCenter(nodeType);
            return button;
        }

        private void CreateNodeAtCenter(Type nodeType)
        {
            if (_graphView == null || _graphView.graph == null)
            {
                UnityEngine.Debug.LogError("[NodeToolbarView] Graph is not initialized.");
                return;
            }

            Rect graphRect = _graphView.layout;
            Vector2 screenCenter = new Vector2(graphRect.width / 2f, graphRect.height / 2f);
#pragma warning disable 0618
            Vector2 pan = (Vector2)_graphView.viewTransform.position;
            float scale = _graphView.scale;
#pragma warning restore 0618
            Vector2 graphCenter = (screenCenter - pan) / scale;

            const float gap = 25f;
            Vector2 finalPos = FindFreePosition(graphCenter, 200, 100, gap);

            var node = (BaseNode)Activator.CreateInstance(nodeType);
            if (node == null) return;
            if (string.IsNullOrEmpty(node.GUID))
                node.GUID = Guid.NewGuid().ToString();

            node.position = new Rect(finalPos.x, finalPos.y, 200, 100);

            try
            {
                _graphView.AddNode(node);
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError($"[NodeToolbarView] Failed to add node: {e.Message}");
                return;
            }

            ShowCategories();
        }

        private Vector2 FindFreePosition(Vector2 desiredCenter, float nodeWidth, float nodeHeight, float gap)
        {
            if (_graphView == null || _graphView.graph == null) return desiredCenter;

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