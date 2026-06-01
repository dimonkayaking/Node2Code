using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GraphProcessor;
using CustomVisualScripting.Editor.Methods;
using CustomVisualScripting.Editor.Nodes.Base;
using CustomVisualScripting.Editor;
using CustomVisualScripting.Editor.Nodes.Methods;
using CustomVisualScripting.Editor.Nodes.Views;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using VisualScripting.Core.Models;

namespace CustomVisualScripting.Editor.Windows
{
    public partial class VisualScriptingWindow
    {
        // ─── Префикс вкладки метода ──────────────────────────────────────────
        private const string MethodTabPrefix = "method:";

        // ─── Рантаймы вкладок методов ────────────────────────────────────────
        private readonly Dictionary<string, MethodTabRuntime> _methodTabRuntimes =
            new(StringComparer.Ordinal);

        private sealed class MethodTabRuntime
        {
            public MethodDefinition Definition;
            public VisualElement     Container;          // TwoPaneSplitView (param-граф | body-граф)

            // Верхний граф — параметры метода (MethodParamNode)
            public MethodParamGraphView ParamGraphView;
            public BaseGraph            ParamInternalGraph;

            // Нижний граф — тело метода
            public FilteredCreateMenuBaseGraphView BodyGraphView;
            public BaseGraph                       BodyInternalGraph;

            public IVisualElementScheduledItem SyncTicker;
        }

        // ─── Сериализация методов ─────────────────────────────────────────────
        [Serializable]
        private class MethodListWrapper
        {
            public List<MethodDefinition> Methods = new();
        }

        // ─── Публичное API ────────────────────────────────────────────────────

        /// <summary>Открывает вкладку редактирования метода. Если уже открыта — активирует её.</summary>
        public void OpenMethodTab(string methodId)
        {
            if (string.IsNullOrWhiteSpace(methodId)) return;
            var def = MethodRegistry.GetById(methodId);
            if (def == null) return;

            var tabId = MethodTabPrefix + methodId;

            var existing = _tabs.FirstOrDefault(t => string.Equals(t.Id, tabId, StringComparison.Ordinal));
            if (existing == null)
            {
                _tabs.Add(new TabDescriptor
                {
                    Id       = tabId,
                    Title    = def.Name,
                    Closable = true
                });
                RenderTabs();
            }

            if (!_methodTabRuntimes.ContainsKey(tabId))
                CreateMethodRuntime(tabId, def);

            ActivateTab(tabId);
        }

        /// <summary>Закрывает вкладку метода.</summary>
        public void CloseMethodTab(string methodId) => CloseTab(MethodTabPrefix + methodId);

        /// <summary>Обновляет заголовок вкладки после переименования метода.</summary>
        public void RefreshMethodTabTitle(string methodId, string newName)
        {
            var tabId = MethodTabPrefix + methodId;
            var tab = _tabs.FirstOrDefault(t => string.Equals(t.Id, tabId, StringComparison.Ordinal));
            if (tab == null) return;
            tab.Title = newName;
            RenderTabs();
        }

        // ─── Сохранение / загрузка ────────────────────────────────────────────

        internal void SyncAllMethodRuntimes()
        {
            foreach (var rt in _methodTabRuntimes.Values)
                SyncMethodRuntime(rt);
        }

        internal void SaveMethodsToPath(string path)
        {
            // Методы сохраняются в .cs файлах через генератор кода
            // JSON-сохранение больше не используется
        }

        internal void LoadMethodsFromPath(string path)
        {
            // Методы загружаются из .cs файлов через парсер
            // JSON-загрузка больше не используется
            MethodRegistry.Clear();
        }

        internal static string GetMethodsFilePath(string csFilePath)
        {
            if (string.IsNullOrWhiteSpace(csFilePath)) return null;
            return Path.Combine(
                Path.GetDirectoryName(csFilePath) ?? "",
                Path.GetFileNameWithoutExtension(csFilePath) + ".methods.json");
        }

        // ─── Создание рантайма метода ─────────────────────────────────────────

        private void CreateMethodRuntime(string tabId, MethodDefinition def)
        {
            var runtime = new MethodTabRuntime { Definition = def };

            // ── Верхний граф: параметры ───────────────────────────────────────
            runtime.ParamInternalGraph = ScriptableObject.CreateInstance<BaseGraph>();
            var paramNodeMap = new Dictionary<string, CustomBaseNode>();

            bool paramGraphHasNodes = def.ParamGraph?.Nodes != null && def.ParamGraph.Nodes.Count > 0;

            if (paramGraphHasNodes)
            {
                // Восстанавливаем MethodParamNode из сохранённых данных
                foreach (var nd in def.ParamGraph.Nodes)
                {
                    var cn = EditorNodeFactory.Create(nd);
                    if (cn == null) continue;
                    cn.NodeId = nd.Id;
                    cn.InitializeFromData(nd);
                    if (cn.GUID != cn.NodeId) cn.SetGUID(cn.NodeId);
                    runtime.ParamInternalGraph.AddNode(cn);
                    paramNodeMap[nd.Id] = cn;
                }
            }
            else if (def.Parameters?.Count > 0)
            {
                // Первый открытый рантайм: создаём ноды параметров из списка MethodDefinition.Parameters
                float xOffset = 40f;
                foreach (var param in def.Parameters)
                {
                    var pn = new MethodParamNode { ParamName = param.Name, ParamType = param.Type };
                    pn.NodeId  = Guid.NewGuid().ToString();
                    pn.SetGUID(pn.NodeId);
                    pn.position = new Rect(xOffset, 30f, 200f, 80f);
                    xOffset += 230f;
                    runtime.ParamInternalGraph.AddNode(pn);
                    paramNodeMap[pn.NodeId] = pn;
                }
            }

            // GraphView параметров — только "Method/" категории в контекстном меню
            runtime.ParamGraphView = new MethodParamGraphView(this);
            runtime.ParamGraphView.NodeViewAdded += OnNodeViewAdded;
            runtime.ParamGraphView.Initialize(runtime.ParamInternalGraph);
            runtime.ParamGraphView.style.flexGrow = 1;
            runtime.ParamGraphView.graphViewChanged += change =>
            {
                SyncMethodRuntime(runtime);
                SyncBodyParamReferences(runtime);
                return change;
            };

            runtime.ParamGraphView.NodeViewAdded += nv =>
            {
                if (nv?.nodeTarget is not MethodParamNode) return;
                SyncMethodRuntime(runtime);
                SyncBodyParamReferences(runtime);
            };

            if (def.ParamGraph?.Edges != null && paramNodeMap.Count > 0)
                GraphViewEdgeRestore.RestoreEdges(runtime.ParamGraphView, def.ParamGraph.Edges,
                    paramNodeMap, validatePortDirections: false);

            if (paramGraphHasNodes)
                GraphDataViewSync.ApplySavedVisualLayout(def.ParamGraph, runtime.ParamGraphView);

            ConfigureNodeViewSizing(runtime.ParamGraphView.nodeViews);
            runtime.ParamGraphView.UpdateViewTransform(Vector3.zero, Vector3.one);
            runtime.ParamGraphView.FrameAll();

            // ── Нижний граф: тело метода ──────────────────────────────────────
            runtime.BodyInternalGraph = ScriptableObject.CreateInstance<BaseGraph>();
            var bodyNodeMap = new Dictionary<string, CustomBaseNode>();

            if (def.BodyGraph?.Nodes != null)
            {
                foreach (var nd in def.BodyGraph.Nodes)
                {
                    var cn = EditorNodeFactory.Create(nd);
                    if (cn == null) continue;
                    cn.NodeId = nd.Id;
                    cn.InitializeFromData(nd);
                    if (cn.GUID != cn.NodeId) cn.SetGUID(cn.NodeId);
                    ApplyLiteralValues(cn, nd);
                    runtime.BodyInternalGraph.AddNode(cn);
                    bodyNodeMap[nd.Id] = cn;
                }
            }

            runtime.BodyGraphView = new MethodBodyGraphView(this);
            runtime.BodyGraphView.NodeViewAdded += OnNodeViewAdded;
            runtime.BodyGraphView.Initialize(runtime.BodyInternalGraph);
            runtime.BodyGraphView.style.flexGrow = 1;
            runtime.BodyGraphView.graphViewChanged += change =>
            {
                SyncMethodRuntime(runtime);
                return change;
            };

            if (def.BodyGraph?.Edges != null && bodyNodeMap.Count > 0)
                GraphViewEdgeRestore.RestoreEdges(runtime.BodyGraphView, def.BodyGraph.Edges,
                    bodyNodeMap, validatePortDirections: false);

            GraphDataViewSync.ApplySavedVisualLayout(def.BodyGraph, runtime.BodyGraphView);
            ConfigureNodeViewSizing(runtime.BodyGraphView.nodeViews);
            runtime.BodyGraphView.UpdateViewTransform(Vector3.zero, Vector3.one);

            // ── Отложенная авто-раскладка тела метода ────────────────────────
            runtime.BodyGraphView.schedule.Execute(() =>
            {
                if (runtime.BodyGraphView?.nodeViews == null || runtime.BodyInternalGraph == null)
                    return;

                ConfigureNodeViewSizing(runtime.BodyGraphView.nodeViews);

                bool hasSavedLayout = GraphViewAutoLayout.HasMeaningfulVisualLayout(
                    runtime.Definition.BodyGraph,
                    runtime.BodyGraphView.nodeViews.Count);

                if (!hasSavedLayout)
                {
                    GraphViewAutoLayout.ApplyIfNeededForNestedGraph(
                        runtime.Definition.BodyGraph,
                        runtime.BodyGraphView.nodeViews,
                        GraphViewAutoLayout.MeasureMainGraphCell);
                }

                runtime.BodyGraphView.FrameAll();
            }).ExecuteLater(1);

            // ── Сборка контейнера ─────────────────────────────────────────────
            var paramArea = new VisualElement();
            paramArea.style.flexGrow      = 1;
            paramArea.style.flexDirection = FlexDirection.Column;
            paramArea.style.overflow      = Overflow.Hidden;

            var paramHeader = BuildParamAreaHeader();
            paramHeader.style.height     = 32f;
            paramHeader.style.flexShrink = 0;
            paramArea.Add(paramHeader);
            paramArea.Add(runtime.ParamGraphView);

            var bodyArea = new VisualElement();
            bodyArea.style.flexGrow      = 1;
            bodyArea.style.flexDirection = FlexDirection.Column;
            bodyArea.style.overflow      = Overflow.Hidden;

            var bodyHeader = BuildBodyAreaHeader();
            bodyHeader.style.height     = 32f;
            bodyHeader.style.flexShrink = 0;
            bodyArea.Add(bodyHeader);
            bodyArea.Add(runtime.BodyGraphView);

            var splitView = new TwoPaneSplitView(0, 220f, TwoPaneSplitViewOrientation.Vertical);
            splitView.style.flexGrow = 1;
            splitView.Add(paramArea);
            splitView.Add(bodyArea);

            runtime.Container = splitView;

            runtime.ParamGraphView.BodyGraphView = runtime.BodyGraphView;

            SyncBodyParamReferences(runtime);
            SyncBodyFieldReferences(runtime);

            runtime.SyncTicker =
                runtime.BodyGraphView.schedule.Execute(() => SyncMethodRuntime(runtime)).Every(300);

            _methodTabRuntimes[tabId] = runtime;
        }

        private static VisualElement BuildParamAreaHeader()
        {
            var header = new VisualElement();
            header.style.flexDirection   = FlexDirection.Row;
            header.style.alignItems      = Align.Center;
            header.style.backgroundColor = new Color(0.12f, 0.12f, 0.12f);
            header.style.paddingLeft     = 8;
            header.style.paddingRight    = 8;
            header.style.paddingTop      = 4;
            header.style.paddingBottom   = 4;

            var label = new Label("Параметры метода");
            label.style.unityFontStyleAndWeight = FontStyle.Bold;
            label.style.fontSize = 11;
            label.style.color    = new Color(0.7f, 0.9f, 1f);
            label.style.flexGrow = 1;
            header.Add(label);

            return header;
        }

        private static VisualElement BuildBodyAreaHeader()
        {
            var header = new VisualElement();
            header.style.flexDirection   = FlexDirection.Row;
            header.style.alignItems      = Align.Center;
            header.style.backgroundColor = new Color(0.12f, 0.12f, 0.12f);
            header.style.paddingLeft     = 8;
            header.style.paddingRight    = 8;
            header.style.paddingTop      = 4;
            header.style.paddingBottom   = 4;

            var label = new Label("Тело метода");
            label.style.unityFontStyleAndWeight = FontStyle.Bold;
            label.style.fontSize = 11;
            label.style.color    = new Color(1f, 0.85f, 0.5f);
            label.style.flexGrow = 1;
            header.Add(label);

            return header;
        }

        // ─── Синхронизация рантайма ──────────────────────────────────────────

        private void SyncMethodRuntime(MethodTabRuntime runtime)
        {
            if (runtime?.Definition == null) return;

            if (runtime.ParamGraphView != null && runtime.ParamInternalGraph != null)
            {
                var allParamNodes = runtime.ParamInternalGraph.nodes.OfType<CustomBaseNode>().ToList();
                GraphDataViewSync.SyncGraphDataNodesAndEdgesFromView(
                    runtime.Definition.ParamGraph, allParamNodes, runtime.ParamGraphView);
                GraphDataViewSync.SaveVisualLayoutToGraphData(
                    runtime.Definition.ParamGraph, runtime.ParamInternalGraph, runtime.ParamGraphView);

                runtime.Definition.Parameters.Clear();
                foreach (var pn in runtime.ParamInternalGraph.nodes.OfType<MethodParamNode>())
                {
                    runtime.Definition.Parameters.Add(new ParameterDefinition
                    {
                        Name = pn.ParamName,
                        Type = pn.ParamType
                    });
                }
            }

            if (runtime.BodyGraphView != null && runtime.BodyInternalGraph != null)
            {
                var bodyNodes = runtime.BodyInternalGraph.nodes.OfType<CustomBaseNode>().ToList();
                GraphDataViewSync.SyncGraphDataNodesAndEdgesFromView(
                    runtime.Definition.BodyGraph, bodyNodes, runtime.BodyGraphView);
                GraphDataViewSync.SaveVisualLayoutToGraphData(
                    runtime.Definition.BodyGraph, runtime.BodyInternalGraph, runtime.BodyGraphView);
            }

            _hasUnsavedChanges = true;
        }

        public void ForceSyncMethodRuntime(string methodId)
        {
            var tabId = MethodTabPrefix + methodId;
            if (_methodTabRuntimes.TryGetValue(tabId, out var runtime))
                SyncMethodRuntime(runtime);
        }

        public void SyncParamGraphFromDefinition(string methodId)
        {
            var tabId = MethodTabPrefix + methodId;
            if (!_methodTabRuntimes.TryGetValue(tabId, out var runtime)) return;
            if (runtime.ParamGraphView == null || runtime.ParamInternalGraph == null) return;

            var def           = runtime.Definition;
            var existingNodes = runtime.ParamInternalGraph.nodes.OfType<MethodParamNode>().ToList();

            var savedPositions = existingNodes.Select(n => n.position).ToList();

            foreach (var oldNode in existingNodes)
            {
                try   { runtime.ParamGraphView.RemoveNode(oldNode); }
                catch (Exception ex) { UnityEngine.Debug.LogWarning($"[VS] SyncParamGraph RemoveNode: {ex.Message}"); }
            }

            for (int i = 0; i < def.Parameters.Count; i++)
            {
                var param = def.Parameters[i];
                var pn    = new MethodParamNode { ParamName = param.Name, ParamType = param.Type };
                pn.NodeId = Guid.NewGuid().ToString();
                pn.SetGUID(pn.NodeId);
                pn.position = i < savedPositions.Count
                    ? savedPositions[i]
                    : new Rect(40f + i * 230f, 30f, 200f, 80f);
                try   { runtime.ParamGraphView.AddNode(pn); }
                catch (Exception ex) { UnityEngine.Debug.LogWarning($"[VS] SyncParamGraph AddNode: {ex.Message}"); }
            }

            runtime.ParamGraphView.schedule.Execute(() =>
            {
                SyncMethodRuntime(runtime);
                SyncBodyParamReferences(runtime);
            }).ExecuteLater(150);
        }

        private void SyncBodyParamReferences(MethodTabRuntime runtime)
        {
            if (runtime?.Definition == null) return;
            if (runtime.BodyGraphView == null || runtime.BodyInternalGraph == null) return;

            var def           = runtime.Definition;
            var currentParams = def.Parameters ?? new List<ParameterDefinition>();

            var existingRefs = runtime.BodyInternalGraph.nodes
                .OfType<MethodParamNode>()
                .ToList();

            var existingByName = existingRefs
                .ToDictionary(n => n.ParamName, StringComparer.Ordinal);
            var currentNames = new HashSet<string>(
                currentParams.Select(p => p.Name), StringComparer.Ordinal);

            foreach (var node in existingRefs)
            {
                if (!currentNames.Contains(node.ParamName))
                {
                    try   { runtime.BodyGraphView.RemoveNode(node); }
                    catch (Exception ex)
                    {
                        UnityEngine.Debug.LogWarning($"[VS] SyncBodyParamRefs RemoveNode: {ex.Message}");
                    }
                }
                else
                {
                    var p = currentParams.First(x => x.Name == node.ParamName);
                    node.ParamType = p.Type;
                }
            }

            int existingCount = existingRefs.Count(n => currentNames.Contains(n.ParamName));
            for (int i = 0; i < currentParams.Count; i++)
            {
                var param = currentParams[i];
                if (existingByName.ContainsKey(param.Name)) continue;

                var pn = new MethodParamNode { ParamName = param.Name, ParamType = param.Type };
                pn.NodeId = "_paramref_" + param.Name;
                pn.SetGUID(pn.NodeId);
                pn.position = new Rect(40f, 40f + existingCount * 120f, 200f, 80f);
                existingCount++;

                try   { runtime.BodyGraphView.AddNode(pn); }
                catch (Exception ex)
                {
                    UnityEngine.Debug.LogWarning($"[VS] SyncBodyParamRefs AddNode: {ex.Message}");
                }
            }
        }

        internal void ImportDiscoveredMethods(IEnumerable<MethodInfo> discovered)
        {
            if (discovered == null) return;

            int count = 0;
            foreach (var mi in discovered)
            {
                if (string.IsNullOrWhiteSpace(mi.Id) || string.IsNullOrWhiteSpace(mi.Name)) continue;

                var existing = MethodRegistry.GetById(mi.Id);
                if (existing != null)
                {
                    existing.Name       = mi.Name;
                    existing.ReturnType = mi.ReturnType ?? "void";
                    existing.Parameters = BuildParamDefs(mi);
                    if (!string.IsNullOrEmpty(mi.ClassId))
                        existing.ClassId = mi.ClassId;
                    if (mi.BodyGraph?.Nodes?.Count > 0)
                        existing.BodyGraph = mi.BodyGraph;
                    MethodRegistry.Update(existing);
                }
                else
                {
                    var def = new MethodDefinition
                    {
                        Id         = mi.Id,
                        Name       = mi.Name,
                        ReturnType = mi.ReturnType ?? "void",
                        ClassId    = mi.ClassId ?? "",
                        Parameters = BuildParamDefs(mi),
                        BodyGraph  = mi.BodyGraph ?? new GraphData(),
                        ParamGraph = new GraphData()
                    };
                    MethodRegistry.Add(def);
                }
                count++;
            }

            if (count > 0)
                UnityEngine.Debug.Log($"[VS] Импортировано inline-методов: {count}");
        }

        private void SyncBodyFieldReferences(MethodTabRuntime runtime)
        {
            if (runtime?.Definition == null) return;
            if (runtime.BodyGraphView == null || runtime.BodyInternalGraph == null) return;

            var classId = runtime.Definition.ClassId;
            if (string.IsNullOrEmpty(classId)) return;

            var classDef = Classes.ClassRegistry.GetById(classId);
            var classFields = classDef?.Fields ?? new System.Collections.Generic.List<Classes.FieldDefinition>();

            var existingRefs = runtime.BodyInternalGraph.nodes
                .OfType<Nodes.Methods.FieldRefNode>()
                .ToList();

            var existingById = new Dictionary<string, Nodes.Methods.FieldRefNode>(StringComparer.Ordinal);
            foreach (var n in existingRefs)
                existingById[n.FieldId ?? ""] = n;

            var currentIds = new HashSet<string>(classFields.Select(f => f.Id), StringComparer.Ordinal);

            const string FieldRefPrefix = "_fieldref_";
            foreach (var node in existingRefs)
            {
                if (!string.IsNullOrEmpty(node.FieldId)) continue;

                var match = classFields.FirstOrDefault(f => f.Name == node.FieldName);

                if (match == null && node.NodeId != null && node.NodeId.StartsWith(FieldRefPrefix, StringComparison.Ordinal))
                {
                    var nameFromId = node.NodeId.Substring(FieldRefPrefix.Length);
                    match = classFields.FirstOrDefault(f =>
                        string.Equals(f.Name, nameFromId, StringComparison.Ordinal));
                }

                if (match != null)
                {
                    node.FieldId   = match.Id;
                    node.FieldType = match.Type;
                    node.FieldName = match.Name;
                    existingById[match.Id] = node;
                }
            }

            foreach (var node in existingRefs)
            {
                if (!currentIds.Contains(node.FieldId))
                {
                    bool hasConnections = runtime.BodyInternalGraph.edges
                        .Any(e => e.inputNode == node || e.outputNode == node);

                    if (hasConnections)
                    {
                        var rescue = classFields.FirstOrDefault(f =>
                            string.Equals(f.Name, node.FieldName, StringComparison.Ordinal));
                        if (rescue != null)
                        {
                            node.FieldId   = rescue.Id;
                            node.FieldType = rescue.Type;
                            existingById[rescue.Id] = node;
                        }
                        continue;
                    }

                    try   { runtime.BodyGraphView.RemoveNode(node); }
                    catch (Exception ex)
                    {
                        UnityEngine.Debug.LogWarning($"[VS] SyncBodyFieldRefs RemoveNode: {ex.Message}");
                    }
                }
                else
                {
                    var f = classFields.First(x => x.Id == node.FieldId);
                    node.FieldType = f.Type;
                    node.FieldName = f.Name;
                }
            }

            int count = existingRefs.Count(n => currentIds.Contains(n.FieldId));
            for (int i = 0; i < classFields.Count; i++)
            {
                var field = classFields[i];
                if (existingById.ContainsKey(field.Id)) continue;

                var fn = new Nodes.Methods.FieldRefNode
                {
                    FieldId   = field.Id,
                    FieldName = field.Name,
                    FieldType = field.Type
                };
                fn.NodeId = "_fieldref_" + field.Id;
                fn.SetGUID(fn.NodeId);
                fn.position = new UnityEngine.Rect(260f, 40f + count * 120f, 200f, 80f);
                count++;

                try   { runtime.BodyGraphView.AddNode(fn); }
                catch (Exception ex)
                {
                    UnityEngine.Debug.LogWarning($"[VS] SyncBodyFieldRefs AddNode: {ex.Message}");
                }
            }
        }

        private static List<ParameterDefinition> BuildParamDefs(MethodInfo mi)
        {
            var result = new List<ParameterDefinition>();
            var names = mi.ParamNames ?? new List<string>();
            for (int i = 0; i < names.Count; i++)
            {
                result.Add(new ParameterDefinition
                {
                    Name = names[i],
                    Type = (mi.ParamTypes != null && i < mi.ParamTypes.Count)
                        ? mi.ParamTypes[i] : "int"
                });
            }
            return result;
        }

        internal void DisposeMethodRuntime(string tabId)
        {
            if (!_methodTabRuntimes.TryGetValue(tabId, out var runtime)) return;
            SyncMethodRuntime(runtime);
            TearDownMethodRuntimeGraph(runtime);
            _methodTabRuntimes.Remove(tabId);
        }

        internal void DisposeAllMethodRuntimes()
        {
            foreach (var key in _methodTabRuntimes.Keys.ToList())
                DisposeMethodRuntime(key);
            _methodTabRuntimes.Clear();
        }

        private void TearDownMethodRuntimeGraph(MethodTabRuntime runtime)
        {
            if (runtime == null) return;

            runtime.SyncTicker?.Pause();
            runtime.SyncTicker = null;

            if (runtime.ParamGraphView != null)
            {
                runtime.ParamGraphView.NodeViewAdded -= OnNodeViewAdded;
                runtime.ParamGraphView.Dispose();
                runtime.ParamGraphView = null;
            }
            if (runtime.ParamInternalGraph != null)
            {
                DestroyImmediate(runtime.ParamInternalGraph);
                runtime.ParamInternalGraph = null;
            }

            if (runtime.BodyGraphView != null)
            {
                runtime.BodyGraphView.NodeViewAdded -= OnNodeViewAdded;
                runtime.BodyGraphView.Dispose();
                runtime.BodyGraphView = null;
            }
            if (runtime.BodyInternalGraph != null)
            {
                DestroyImmediate(runtime.BodyInternalGraph);
                runtime.BodyInternalGraph = null;
            }

            runtime.Container = null;
        }

        private static void ApplyLiteralValues(CustomBaseNode node, NodeData nodeData)
        {
            if (node is Nodes.Literals.IntNode intNode && int.TryParse(nodeData.Value, out var intVal))
                intNode.intValue = intVal;
            else if (node is Nodes.Literals.FloatNode floatNode &&
                     float.TryParse(nodeData.Value, System.Globalization.NumberStyles.Float,
                         System.Globalization.CultureInfo.InvariantCulture, out var floatVal))
                floatNode.floatValue = floatVal;
            else if (node is Nodes.Literals.BoolNode boolNode && bool.TryParse(nodeData.Value, out var boolVal))
                boolNode.boolValue = boolVal;
            else if (node is Nodes.Literals.StringNode stringNode)
                stringNode.stringValue = nodeData.Value;
        }
    }
}