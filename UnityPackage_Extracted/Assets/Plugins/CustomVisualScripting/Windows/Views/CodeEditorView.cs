using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;
using System.Text;

namespace CustomVisualScripting.Windows.Views
{
    public class CodeEditorView : VisualElement
    {
        private const float GutterWidth = 48f;
        private const float OuterHorizontalPadding = 8f;
        private const float OuterVerticalPadding = 6f;
        private const float CodeGapFromGutter = 10f;
        private const string CodeControlName = "CodeEditorTextArea";

        private readonly IMGUIContainer _imguiEditor;

        // Флаг: Tab был перехвачен в TrickleDown до того, как IMGUI/UI Toolkit увидели событие.
        // Отступ вставляется в следующем IMGUI-проходе, пока фокус не менялся.
        private bool _pendingTabIndent;

        private string _code = string.Empty;
        private string _lineNumbers = "1";
        private int _lineCount = 1;
        private int _maxLineLength = 1;

        private Vector2 _scrollPosition;
        private GUIStyle _codeStyle;
        private GUIStyle _lineNumberStyle;
        private int _lastCursorIndex = -1;
        private int _lastSelectIndex = -1;
        private bool _manualScrollOverride;

        public string Code
        {
            get => _code;
            set
            {
                _code = value ?? string.Empty;
                RebuildLineMetadata();
                _imguiEditor?.MarkDirtyRepaint();
            }
        }

        public CodeEditorView()
        {
            var label = new Label("Код для парсинга:");
            label.style.marginBottom = 5;
            label.style.fontSize = 12;
            label.style.unityFontStyleAndWeight = FontStyle.Bold;
            label.style.color = Color.white;
            Add(label);

            _imguiEditor = new IMGUIContainer(DrawEditor)
            {
                style =
                {
                    flexGrow = 1,
                    minHeight = 0
                }
            };
            _imguiEditor.AddToClassList("code-editor-imgui-container");
            _imguiEditor.focusable = true;
            // TrickleDown: перехватываем Tab ДО того как IMGUIContainer получит событие.
            // Это единственный способ предотвратить и IMGUI-переход фокуса, и UI Toolkit-навигацию.
            _imguiEditor.RegisterCallback<KeyDownEvent>(OnTabTrickleDown, TrickleDown.TrickleDown);
            Add(_imguiEditor);

            style.flexGrow = 1;
            style.minHeight = 0;
            style.marginLeft = 5;
            style.marginRight = 5;

            RegisterCallback<KeyDownEvent>(OnTabTrickleDown, TrickleDown.TrickleDown);
            RegisterCallback<NavigationMoveEvent>(OnNavigationMoveTrickleDown, TrickleDown.TrickleDown);
            RegisterCallback<NavigationSubmitEvent>(OnNavigationSubmitTrickleDown, TrickleDown.TrickleDown);

            RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);

            RebuildLineMetadata();
        }

        private void OnAttachToPanel(AttachToPanelEvent evt)
        {
            var root = panel?.visualTree;
            if (root == null)
                return;

            root.RegisterCallback<KeyDownEvent>(OnRootTabTrickleDown, TrickleDown.TrickleDown);
            root.RegisterCallback<NavigationMoveEvent>(OnRootNavigationMove, TrickleDown.TrickleDown);
        }

        private void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            var root = evt.originPanel?.visualTree;
            if (root == null)
                return;

            root.UnregisterCallback<KeyDownEvent>(OnRootTabTrickleDown, TrickleDown.TrickleDown);
            root.UnregisterCallback<NavigationMoveEvent>(OnRootNavigationMove, TrickleDown.TrickleDown);
        }

        private bool IsCodeEditorFocused()
        {
            if (panel == null)
                return false;
            var focused = panel.focusController?.focusedElement as VisualElement;
            if (focused == null)
                return false;
            // Фокус принадлежит нам, если сам IMGUI-контейнер или его предок — этот вью.
            return focused == _imguiEditor || focused == this ||
                   (focused is VisualElement ve && ve.FindCommonAncestor(_imguiEditor) == _imguiEditor);
        }

        private void OnRootTabTrickleDown(KeyDownEvent evt)
        {
            if (!IsCodeEditorFocused())
                return;
            SwallowTabAndScheduleIndent(evt);
        }

        private void OnRootNavigationMove(NavigationMoveEvent evt)
        {
            if (!IsCodeEditorFocused())
                return;
            evt.StopImmediatePropagation();
            evt.StopPropagation();
        }

        // TrickleDown: перехватываем Tab до IMGUIContainer — фокус не сдвигается ни в IMGUI, ни в UI Toolkit.
        // Отступ применяется флагом в следующем IMGUI-кадре (внутри DrawEditor), пока фокус сохранён.
        private void OnTabTrickleDown(KeyDownEvent evt)
        {
            if (!IsCodeEditorFocused())
                return;
            SwallowTabAndScheduleIndent(evt);
        }

        private void SwallowTabAndScheduleIndent(KeyDownEvent evt)
        {
            if (evt.keyCode != KeyCode.Tab && evt.character != '\t')
                return;
            if (evt.shiftKey)
                return;

            evt.StopImmediatePropagation();
            evt.StopPropagation();

            _pendingTabIndent = true;
            _imguiEditor.MarkDirtyRepaint();
        }

        private void OnNavigationMoveTrickleDown(NavigationMoveEvent evt)
        {
            evt.StopImmediatePropagation();
            evt.StopPropagation();
        }

        private void OnNavigationSubmitTrickleDown(NavigationSubmitEvent evt)
        {
            evt.StopImmediatePropagation();
            evt.StopPropagation();
        }


        public new void Clear()
        {
            Code = string.Empty;
            _scrollPosition = Vector2.zero;
            _lastCursorIndex = -1;
            _lastSelectIndex = -1;
            _manualScrollOverride = false;
        }

        private void DrawEditor()
        {
            EnsureStyles();

            // Если Tab был перехвачен в TrickleDown — восстанавливаем IMGUI-фокус,
            // чтобы TextEditor знал, в каком контроле сейчас каретка.
            if (_pendingTabIndent)
                GUI.FocusControl(CodeControlName);

            var viewportRect = GUILayoutUtility.GetRect(
                0f,
                100000f,
                0f,
                100000f,
                GUILayout.ExpandWidth(true),
                GUILayout.ExpandHeight(true));

            DrawEditorBackground(viewportRect);
            DrawEditorContents(viewportRect);
        }

        private void DrawEditorBackground(Rect rect)
        {
            EditorLikeDrawRect(rect, new Color(0.12f, 0.12f, 0.12f));
            EditorLikeDrawRect(new Rect(rect.x, rect.y, 1f, rect.height), new Color(0.24f, 0.24f, 0.24f));
            EditorLikeDrawRect(new Rect(rect.xMax - 1f, rect.y, 1f, rect.height), new Color(0.24f, 0.24f, 0.24f));
            EditorLikeDrawRect(new Rect(rect.x, rect.y, rect.width, 1f), new Color(0.24f, 0.24f, 0.24f));
            EditorLikeDrawRect(new Rect(rect.x, rect.yMax - 1f, rect.width, 1f), new Color(0.24f, 0.24f, 0.24f));
        }

        private void DrawEditorContents(Rect viewportRect)
        {
            const float scrollBarAllowance = 24f;
            float lineHeight = _codeStyle.lineHeight > 0f ? _codeStyle.lineHeight : 16f;
            float charWidth = Mathf.Max(6f, _codeStyle.CalcSize(new GUIContent("M")).x);

            float estimatedTextWidth = _maxLineLength * charWidth + _codeStyle.padding.left + _codeStyle.padding.right;
            float contentWidth = Mathf.Max(
                viewportRect.width - scrollBarAllowance,
                GutterWidth + OuterHorizontalPadding * 2f + CodeGapFromGutter + estimatedTextWidth);
            float contentHeight = Mathf.Max(
                viewportRect.height - scrollBarAllowance,
                _lineCount * lineHeight + OuterVerticalPadding * 2f + _codeStyle.padding.top + _codeStyle.padding.bottom);

            var contentRect = new Rect(0f, 0f, contentWidth, contentHeight);
            var evt = Event.current;
            if (evt != null && evt.type == EventType.ScrollWheel && viewportRect.Contains(evt.mousePosition))
                _manualScrollOverride = true;

            _scrollPosition = GUI.BeginScrollView(viewportRect, _scrollPosition, contentRect, true, true);
            DrawScrollContent(contentRect, viewportRect, lineHeight);
            GUI.EndScrollView();
        }

        private void DrawScrollContent(Rect contentRect, Rect viewportRect, float lineHeight)
        {
            var gutterRect = new Rect(contentRect.x, contentRect.y, GutterWidth, contentRect.height);
            EditorLikeDrawRect(gutterRect, new Color(0.10f, 0.10f, 0.10f));
            EditorLikeDrawRect(new Rect(gutterRect.xMax - 1f, gutterRect.y, 1f, gutterRect.height), new Color(0.20f, 0.20f, 0.20f));

            var lineRect = new Rect(
                gutterRect.x + 4f,
                gutterRect.y + OuterVerticalPadding,
                GutterWidth - 10f,
                Mathf.Max(lineHeight, contentRect.height - OuterVerticalPadding * 2f));
            GUI.Label(lineRect, _lineNumbers, _lineNumberStyle);

            float codeX = gutterRect.xMax + CodeGapFromGutter;
            var codeRect = new Rect(
                codeX,
                contentRect.y + OuterVerticalPadding,
                Mathf.Max(1f, contentRect.width - codeX - OuterHorizontalPadding),
                Mathf.Max(lineHeight, contentRect.height - OuterVerticalPadding * 2f));

            EditorGUIUtility.AddCursorRect(codeRect, MouseCursor.Text);

            // Применяем отложенный Tab-отступ до вызова TextArea,
            // чтобы TextArea получил уже обновлённую строку и позицию каретки.
            if (_pendingTabIndent)
                ApplyPendingTabIndent();

            GUI.SetNextControlName(CodeControlName);
            string next = GUI.TextArea(codeRect, _code, _codeStyle);
            bool textChangedThisFrame = false;
            if (!string.Equals(next, _code))
            {
                _code = next;
                RebuildLineMetadata();
                textChangedThisFrame = true;
            }

            EnsureCaretVisible(codeRect, viewportRect, lineHeight, textChangedThisFrame);
        }

        private void ApplyPendingTabIndent()
        {
            _pendingTabIndent = false;

            // keyboardControl хранит ID контрола с фокусом — берём TextEditor из state-кеша IMGUI.
            var editor = GUIUtility.GetStateObject(typeof(TextEditor), GUIUtility.keyboardControl) as TextEditor;
            if (editor == null)
                return;

            _code ??= string.Empty;
            int start = Mathf.Clamp(Mathf.Min(editor.cursorIndex, editor.selectIndex), 0, _code.Length);
            int end   = Mathf.Clamp(Mathf.Max(editor.cursorIndex, editor.selectIndex), 0, _code.Length);
            const string indent = "    ";

            _code = end > start
                ? _code.Remove(start, end - start).Insert(start, indent)
                : _code.Insert(start, indent);

            int newCursor = start + indent.Length;

            // Синхронизируем editor.text ДО вызова GUI.TextArea.
            // Если этого не сделать, TextArea обнаружит расхождение _code vs editor.text
            // и выделит вставленную часть как «внешнее изменение» — отсюда 4 выделенных пробела.
            editor.text = _code;
            editor.cursorIndex = newCursor;
            editor.selectIndex = newCursor;   // нет выделения — только каретка

            RebuildLineMetadata();
            GUI.changed = true;
        }

        private void EnsureStyles()
        {
            if (_codeStyle == null)
            {
                _codeStyle = new GUIStyle(GUI.skin.textArea)
                {
                    wordWrap = false,
                    richText = false
                };
                _codeStyle.font = Font.CreateDynamicFontFromOSFont("Consolas", 13) ??
                                  Font.CreateDynamicFontFromOSFont("Courier New", 13);
                _codeStyle.fontSize = 13;
                _codeStyle.padding = new RectOffset(6, 6, 4, 4);
                _codeStyle.margin = new RectOffset(0, 0, 0, 0);
            }

            if (_lineNumberStyle == null)
            {
                _lineNumberStyle = new GUIStyle(_codeStyle)
                {
                    alignment = TextAnchor.UpperRight,
                    normal = { textColor = new Color(0.45f, 0.45f, 0.45f) },
                    active = { textColor = new Color(0.45f, 0.45f, 0.45f) },
                    focused = { textColor = new Color(0.45f, 0.45f, 0.45f) },
                    hover = { textColor = new Color(0.45f, 0.45f, 0.45f) }
                };
                _lineNumberStyle.padding = new RectOffset(0, 2, 4, 4);
            }
        }

        private void EnsureCaretVisible(Rect codeRect, Rect viewportRect, float lineHeight, bool textChangedThisFrame)
        {
            if (!string.Equals(GUI.GetNameOfFocusedControl(), CodeControlName))
                return;

            var evt = Event.current;
            var textEditor = GUIUtility.GetStateObject(typeof(TextEditor), GUIUtility.keyboardControl) as TextEditor;
            if (textEditor == null)
                return;

            int cursorIndex = Mathf.Clamp(textEditor.cursorIndex, 0, _code.Length);
            int selectIndex = Mathf.Clamp(textEditor.selectIndex, 0, _code.Length);
            bool caretMoved = cursorIndex != _lastCursorIndex || selectIndex != _lastSelectIndex;
            bool navigationEvent = IsCaretNavigationEvent(evt);

            if (caretMoved)
                _manualScrollOverride = false;

            // If user manually scrolled away, don't snap back until caret actually moves.
            if (_manualScrollOverride && !caretMoved && !textChangedThisFrame && !navigationEvent)
                return;

            GetLineAndColumn(cursorIndex, out int line, out int column);

            float caretY = codeRect.y + _codeStyle.padding.top + line * lineHeight;
            const float margin = 10f;
            float visibleTop = _scrollPosition.y + viewportRect.yMin;
            float visibleBottom = _scrollPosition.y + viewportRect.yMax - 18f;

            if (caretY + lineHeight + margin > visibleBottom)
                _scrollPosition.y = caretY + lineHeight + margin - viewportRect.height;
            else if (caretY - margin < visibleTop)
                _scrollPosition.y = Mathf.Max(0f, caretY - margin);

            _lastCursorIndex = cursorIndex;
            _lastSelectIndex = selectIndex;
        }

        private static bool IsCaretNavigationEvent(Event evt)
        {
            if (evt == null || evt.type != EventType.KeyDown)
                return false;

            return evt.keyCode == KeyCode.LeftArrow ||
                   evt.keyCode == KeyCode.RightArrow ||
                   evt.keyCode == KeyCode.UpArrow ||
                   evt.keyCode == KeyCode.DownArrow ||
                   evt.keyCode == KeyCode.Home ||
                   evt.keyCode == KeyCode.End ||
                   evt.keyCode == KeyCode.PageUp ||
                   evt.keyCode == KeyCode.PageDown;
        }

        private void GetLineAndColumn(int index, out int line, out int column)
        {
            line = 0;
            column = 0;

            for (int i = 0; i < index; i++)
            {
                char c = _code[i];
                if (c == '\n')
                {
                    line++;
                    column = 0;
                }
                else if (c != '\r')
                {
                    column++;
                }
            }
        }

        private void RebuildLineMetadata()
        {
            int lines = 1;
            int currentLength = 0;
            int maxLength = 0;

            for (int i = 0; i < _code.Length; i++)
            {
                char c = _code[i];
                if (c == '\n')
                {
                    lines++;
                    if (currentLength > maxLength)
                        maxLength = currentLength;
                    currentLength = 0;
                    continue;
                }

                if (c != '\r')
                    currentLength++;
            }

            if (currentLength > maxLength)
                maxLength = currentLength;

            _lineCount = Mathf.Max(1, lines);
            _maxLineLength = Mathf.Max(1, maxLength);

            var sb = new StringBuilder(_lineCount * 4);
            for (int i = 1; i <= _lineCount; i++)
            {
                sb.Append(i);
                if (i < _lineCount)
                    sb.Append('\n');
            }

            _lineNumbers = sb.ToString();
        }

        private static void EditorLikeDrawRect(Rect rect, Color color)
        {
            var oldColor = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = oldColor;
        }
    }
}