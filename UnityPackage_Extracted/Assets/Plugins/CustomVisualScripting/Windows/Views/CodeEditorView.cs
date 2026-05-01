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
        private const float HeaderHeight = 38f;
        private const float LineHeightPx = 16f;
        private const float RightExtraSpace = 8f;

        private readonly IMGUIContainer _imguiEditor;
        private bool _pendingTabIndent;

        private string _code = string.Empty;
        private string _lineNumbers = "1";
        private int _lineCount = 1;
        private int _maxLineLength = 1;

        private Vector2 _scrollPosition;
        private GUIStyle _codeStyle;
        private GUIStyle _lineNumberStyle;

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
            // Заголовок
            var headerContainer = new VisualElement();
            headerContainer.style.height = HeaderHeight;
            headerContainer.style.flexDirection = FlexDirection.Row;
            headerContainer.style.alignItems = Align.Center;
            headerContainer.style.justifyContent = Justify.Center;
            headerContainer.style.marginTop = 0;
            headerContainer.style.marginBottom = 0;
            headerContainer.style.paddingTop = 0;
            headerContainer.style.paddingBottom = 0;
            headerContainer.style.backgroundColor = new Color(0.22f, 0.22f, 0.22f);
            headerContainer.style.borderBottomWidth = 1;
            headerContainer.style.borderBottomColor = new Color(0.3f, 0.3f, 0.3f);
            headerContainer.style.borderTopWidth = 0;
            headerContainer.style.borderLeftWidth = 0;
            headerContainer.style.borderRightWidth = 0;

            var title = new Label("Код");
            title.style.fontSize = 14;
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.color = Color.white;
            title.style.unityTextAlign = TextAnchor.MiddleCenter;
            title.style.marginTop = 0;
            title.style.marginBottom = 0;
            title.style.paddingTop = 0;
            title.style.paddingBottom = 0;

            headerContainer.Add(title);
            Add(headerContainer);

            _imguiEditor = new IMGUIContainer(DrawEditor)
            {
                style =
                {
                    flexGrow = 1,
                    minHeight = 0,
                    marginTop = 0,
                    marginBottom = 0,
                    paddingTop = 0,
                    paddingBottom = 0,
                    borderTopWidth = 0,
                    borderBottomWidth = 0,
                    borderLeftWidth = 0,
                    borderRightWidth = 0,
                    backgroundColor = new Color(0.12f, 0.12f, 0.12f)
                }
            };
            _imguiEditor.AddToClassList("code-editor-imgui-container");
            _imguiEditor.focusable = true;
            _imguiEditor.RegisterCallback<KeyDownEvent>(OnTabTrickleDown, TrickleDown.TrickleDown);
            Add(_imguiEditor);

            style.flexGrow = 1;
            style.minHeight = 0;
            style.marginLeft = 0;
            style.marginRight = 0;
            style.marginTop = 0;
            style.marginBottom = 0;
            style.paddingTop = 0;
            style.paddingBottom = 0;
            style.borderTopWidth = 0;
            style.borderBottomWidth = 0;
            style.borderLeftWidth = 0;
            style.borderRightWidth = 0;

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
            if (root == null) return;
            root.RegisterCallback<KeyDownEvent>(OnRootTabTrickleDown, TrickleDown.TrickleDown);
            root.RegisterCallback<NavigationMoveEvent>(OnRootNavigationMove, TrickleDown.TrickleDown);
        }

        private void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            var root = evt.originPanel?.visualTree;
            if (root == null) return;
            root.UnregisterCallback<KeyDownEvent>(OnRootTabTrickleDown, TrickleDown.TrickleDown);
            root.UnregisterCallback<NavigationMoveEvent>(OnRootNavigationMove, TrickleDown.TrickleDown);
        }

        private bool IsCodeEditorFocused()
        {
            if (panel == null) return false;
            var focused = panel.focusController?.focusedElement as VisualElement;
            if (focused == null) return false;
            return focused == _imguiEditor || focused == this ||
                   (focused is VisualElement ve && ve.FindCommonAncestor(_imguiEditor) == _imguiEditor);
        }

        private void OnRootTabTrickleDown(KeyDownEvent evt)
        {
            if (!IsCodeEditorFocused()) return;
            SwallowTabAndScheduleIndent(evt);
        }

        private void OnRootNavigationMove(NavigationMoveEvent evt)
        {
            if (!IsCodeEditorFocused()) return;
            evt.StopImmediatePropagation();
            evt.StopPropagation();
        }

        private void OnTabTrickleDown(KeyDownEvent evt)
        {
            if (!IsCodeEditorFocused()) return;
            SwallowTabAndScheduleIndent(evt);
        }

        private void SwallowTabAndScheduleIndent(KeyDownEvent evt)
        {
            if (evt.keyCode != KeyCode.Tab && evt.character != '\t') return;
            if (evt.shiftKey) return;
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
        }

        private void DrawEditor()
        {
            EnsureStyles();
            if (_pendingTabIndent)
                GUI.FocusControl(CodeControlName);

            var viewportRect = GUILayoutUtility.GetRect(0f, 100000f, 0f, 100000f, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
            EditorLikeDrawRect(viewportRect, new Color(0.12f, 0.12f, 0.12f));
            DrawEditorContents(viewportRect);
        }

        private void DrawEditorContents(Rect viewportRect)
        {
            const float scrollBarAllowance = 24f;
            float charWidth = Mathf.Max(6f, _codeStyle.CalcSize(new GUIContent("M")).x);

            // Ширина контента: минимум – видимая область, максимум – необходимая ширина по тексту
            float requiredWidth = GutterWidth + OuterHorizontalPadding * 2f + CodeGapFromGutter + (_maxLineLength * charWidth) + RightExtraSpace;
            float contentWidth = Mathf.Max(viewportRect.width - scrollBarAllowance, requiredWidth);
            // Высота контента: минимум – видимая область, максимум – необходимая высота по строкам
            float requiredHeight = _lineCount * LineHeightPx + OuterVerticalPadding * 2f;
            float contentHeight = Mathf.Max(viewportRect.height - scrollBarAllowance, requiredHeight);

            var contentRect = new Rect(0f, 0f, contentWidth, contentHeight);

            _scrollPosition = GUI.BeginScrollView(viewportRect, _scrollPosition, contentRect, true, true);
            bool textChanged = DrawScrollContent(contentRect, LineHeightPx);
            GUI.EndScrollView();

            if (textChanged && IsCodeEditorFocused())
            {
                AdjustScrollToCaret(viewportRect);
            }
        }

        private bool DrawScrollContent(Rect contentRect, float lineHeight)
        {
            bool textChanged = false;
            var gutterRect = new Rect(contentRect.x, contentRect.y, GutterWidth, contentRect.height);
            EditorLikeDrawRect(gutterRect, new Color(0.10f, 0.10f, 0.10f));
            EditorLikeDrawRect(new Rect(gutterRect.xMax - 1f, gutterRect.y, 1f, gutterRect.height), new Color(0.22f, 0.22f, 0.22f));

            var lineRect = new Rect(gutterRect.x + 4f, gutterRect.y + OuterVerticalPadding, GutterWidth - 10f,
                Mathf.Max(lineHeight, contentRect.height - OuterVerticalPadding * 2f));
            GUI.Label(lineRect, _lineNumbers, _lineNumberStyle);

            float codeX = gutterRect.xMax + CodeGapFromGutter;
            var codeRect = new Rect(codeX, contentRect.y + OuterVerticalPadding,
                Mathf.Max(1f, contentRect.width - codeX - OuterHorizontalPadding),
                Mathf.Max(lineHeight, contentRect.height - OuterVerticalPadding * 2f));

            EditorGUIUtility.AddCursorRect(codeRect, MouseCursor.Text);

            if (_pendingTabIndent)
                ApplyPendingTabIndent();

            GUI.SetNextControlName(CodeControlName);
            string next = GUI.TextArea(codeRect, _code, _codeStyle);
            if (!string.Equals(next, _code))
            {
                _code = next;
                RebuildLineMetadata();
                textChanged = true;
            }
            return textChanged;
        }

        private void ApplyPendingTabIndent()
        {
            _pendingTabIndent = false;
            var editor = GUIUtility.GetStateObject(typeof(TextEditor), GUIUtility.keyboardControl) as TextEditor;
            if (editor == null) return;
            _code ??= string.Empty;
            int start = Mathf.Clamp(Mathf.Min(editor.cursorIndex, editor.selectIndex), 0, _code.Length);
            int end = Mathf.Clamp(Mathf.Max(editor.cursorIndex, editor.selectIndex), 0, _code.Length);
            const string indent = "    ";
            _code = end > start
                ? _code.Remove(start, end - start).Insert(start, indent)
                : _code.Insert(start, indent);
            int newCursor = start + indent.Length;
            editor.text = _code;
            editor.cursorIndex = newCursor;
            editor.selectIndex = newCursor;
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
                    richText = false,
                    border = new RectOffset(0, 0, 0, 0)
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
                    border = new RectOffset(0, 0, 0, 0)
                };
                _lineNumberStyle.padding = new RectOffset(0, 2, 4, 4);
            }
        }

        private void AdjustScrollToCaret(Rect viewportRect)
        {
            if (!string.Equals(GUI.GetNameOfFocusedControl(), CodeControlName)) return;

            var textEditor = GUIUtility.GetStateObject(typeof(TextEditor), GUIUtility.keyboardControl) as TextEditor;
            if (textEditor == null) return;

            int cursorIndex = Mathf.Clamp(textEditor.cursorIndex, 0, _code.Length);
            GetLineAndColumn(cursorIndex, out int line, out int column);

            // Вертикальная позиция
            float caretY = _codeStyle.padding.top + line * LineHeightPx + OuterVerticalPadding;
            float visibleTop = _scrollPosition.y;
            float visibleBottom = visibleTop + viewportRect.height - 40f;
            const float verticalMargin = 10f;

            if (caretY + LineHeightPx + verticalMargin > visibleBottom)
                _scrollPosition.y = caretY + LineHeightPx + verticalMargin - (viewportRect.height - 40f);
            else if (caretY - verticalMargin < visibleTop)
                _scrollPosition.y = Mathf.Max(0f, caretY - verticalMargin);

            // Горизонтальная позиция
            string currentLine = GetLineText(line);
            float caretX = column < currentLine.Length
                ? _codeStyle.CalcSize(new GUIContent(currentLine.Substring(0, column))).x
                : _codeStyle.CalcSize(new GUIContent(currentLine)).x;
            float totalLeftOffset = GutterWidth + CodeGapFromGutter + OuterHorizontalPadding;
            float caretScreenX = totalLeftOffset + caretX;

            float visibleLeft = _scrollPosition.x;
            float visibleRight = visibleLeft + viewportRect.width - 30f;
            const float horizontalMargin = 20f;

            if (caretScreenX < visibleLeft + horizontalMargin)
                _scrollPosition.x = Mathf.Max(0f, caretScreenX - horizontalMargin);
            else if (caretScreenX > visibleRight - horizontalMargin)
                _scrollPosition.x = caretScreenX - (viewportRect.width - 30f) + horizontalMargin;

            _scrollPosition.x = Mathf.Max(0f, _scrollPosition.x);
            _scrollPosition.y = Mathf.Max(0f, _scrollPosition.y);
        }

        private string GetLineText(int lineNumber)
        {
            if (_code == null) return "";
            int currentLine = 0;
            int start = 0;
            for (int i = 0; i < _code.Length; i++)
            {
                if (_code[i] == '\n')
                {
                    if (currentLine == lineNumber)
                        return _code.Substring(start, i - start);
                    start = i + 1;
                    currentLine++;
                }
            }
            if (currentLine == lineNumber)
                return _code.Substring(start);
            return "";
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
                    column++;
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
                    if (currentLength > maxLength) maxLength = currentLength;
                    currentLength = 0;
                    continue;
                }
                if (c != '\r')
                    currentLength++;
            }
            if (currentLength > maxLength) maxLength = currentLength;
            _lineCount = Mathf.Max(1, lines);
            _maxLineLength = Mathf.Max(1, maxLength);
            var sb = new StringBuilder(_lineCount * 4);
            for (int i = 1; i <= _lineCount; i++)
            {
                sb.Append(i);
                if (i < _lineCount) sb.Append('\n');
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