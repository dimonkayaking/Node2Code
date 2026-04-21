# Ветка версии плагина за 1 итерацию 
## Структура проекта
```plaintext
VisualScripting/ (КОРЕНЬ РЕПОЗИТОРИЯ)
│
├── Core/                                     ← РОЛЬ №1 (ОТДЕЛЬНЫЙ .NET ПРОЕКТ)
│   ├── VisualScripting.Core.csproj
│   ├── VisualScripting.Core.sln
│   ├── Directory.Build.props
│   ├── global.json
│   │
│   ├── Models/
│   │   ├── NodeType.cs
│   │   ├── NodeData.cs
│   │   └── GraphData.cs
│   │
│   ├── Parsers/
│   │   ├── ICodeParser.cs
│   │   ├── ParseResult.cs
│   │   ├── RoslynCodeParser.cs
│   │   └── GraphBuilderWalker.cs
│   │
│   ├── Generators/
│   │   ├── ICodeGenerator.cs
│   │   └── SimpleCodeGenerator.cs
│   │
│   └── Tests/
│       ├── ParserTests.cs
│       ├── GeneratorTests.cs
│       ├── GraphDataTests.cs
│       └── TestHelpers.cs
│
├── UnityProject/                              ← UNITY ПРОЕКТ (2026.1+)
│   ├── Assets/
│   │   └── Plugins/
│   │       └── VisualScripting/
│   │           │
│   │           ├── Core/                      ← DLL ОТ РОЛИ №1 (копируется сюда)
│   │           │   ├── VisualScripting.Core.dll
│   │           │   ├── VisualScripting.Core.xml
│   │           │   ├── Microsoft.CodeAnalysis.dll
│   │           │   ├── Microsoft.CodeAnalysis.CSharp.dll
│   │           │   ├── System.Collections.Immutable.dll
│   │           │   └── link.xml
│   │           │
│   │           ├── NodeGraph/                  ← ПОДКЛЮЧЕННАЯ БИБЛИОТЕКА
│   │           │   ├── Editor/
│   │           │   ├── Runtime/
│   │           │   └── package.json
│   │           │
│   │           ├── Editor/                     ← РОЛЬ №2 (Визуальные ноды)
│   │           │   ├── VisualScripting.Editor.asmdef
│   │           │   │
│   │           │   ├── Nodes/
│   │           │   │   ├── Base/
│   │           │   │   │   ├── BaseNode.cs
│   │           │   │   │   ├── BaseValueNode.cs
│   │           │   │   │   └── BaseExecutionNode.cs
│   │           │   │   │
│   │           │   │   ├── Literals/
│   │           │   │   │   ├── IntNode.cs
│   │           │   │   │   ├── FloatNode.cs
│   │           │   │   │   ├── StringNode.cs
│   │           │   │   │   └── BoolNode.cs
│   │           │   │   │
│   │           │   │   ├── Math/
│   │           │   │   │   ├── AddNode.cs
│   │           │   │   │   ├── SubtractNode.cs
│   │           │   │   │   ├── MultiplyNode.cs
│   │           │   │   │   └── DivideNode.cs
│   │           │   │   │
│   │           │   │   ├── Comparison/
│   │           │   │   │   ├── GreaterNode.cs
│   │           │   │   │   ├── LessNode.cs
│   │           │   │   │   └── EqualNode.cs
│   │           │   │   │
│   │           │   │   ├── Variables/
│   │           │   │   │   ├── VariableDeclarationNode.cs
│   │           │   │   │   ├── GetVariableNode.cs
│   │           │   │   │   └── SetVariableNode.cs
│   │           │   │   │
│   │           │   │   ├── Flow/
│   │           │   │   │   └── IfNode.cs
│   │           │   │   │
│   │           │   │   ├── Unity/
│   │           │   │   │   ├── Vector3CreateNode.cs
│   │           │   │   │   ├── GetPositionNode.cs
│   │           │   │   │   └── SetPositionNode.cs
│   │           │   │   │
│   │           │   │   ├── Debug/
│   │           │   │   │   └── DebugLogNode.cs
│   │           │   │   │
│   │           │   │   └── GraphSerializer.cs
│   │           │   │
│   │           │   └── (другие Editor папки)
│   │           │
│   │           ├── Runtime/                     ← РОЛЬ №2 (Runtime)
│   │           │   ├── VisualScripting.Runtime.asmdef
│   │           │   │
│   │           │   ├── Assets/
│   │           │   │   └── GraphAsset.cs
│   │           │   │
│   │           │   ├── Components/
│   │           │   │   └── VisualScriptBehaviour.cs
│   │           │   │
│   │           │   └── Execution/
│   │           │       ├── GraphRunner.cs
│   │           │       └── NodeExecutor.cs
│   │           │
│   │           ├── Integration/                  ← FULLSTACK (Мосты)
│   │           │   ├── ParserBridge.cs
│   │           │   ├── GeneratorBridge.cs
│   │           │   ├── GraphConverter.cs
│   │           │   ├── GraphSaver.cs
│   │           │   ├── GraphLoader.cs
│   │           │   └── Models/
│   │           │       ├── VisualNodeData.cs
│   │           │       └── CompleteGraphData.cs
│   │           │
│   │           ├── Windows/                      ← FULLSTACK (UI)
│   │           │   ├── VisualScriptingWindow.cs
│   │           │   ├── Views/
│   │           │   │   ├── CodeEditorView.cs
│   │           │   │   ├── ToolbarView.cs
│   │           │   │   └── ErrorPanel.cs
│   │           │   └── Styles/
│   │           │       └── WindowStyles.uss
│   │           │
│   │           ├── Textures/                     ← FULLSTACK
│   │           │   ├── grid_dark.png
│   │           │   └── grid_light.png
│   │           │
│   │           ├── Samples/                      ← FULLSTACK (Примеры)
│   │           │   ├── SimpleMath/
│   │           │   │   ├── SimpleMath.asset
│   │           │   │   └── SimpleMath.unity
│   │           │   └── PlayerController/
│   │           │       ├── PlayerController.asset
│   │           │       └── PlayerController.unity
│   │           │
│   │           └── Documentation/                ← FULLSTACK
│   │               ├── README.md
│   │               ├── INSTALL.md
│   │               ├── API.md
│   │               └── Tutorials/
│   │                   ├── 01_GettingStarted.md
│   │                   ├── 02_FirstGraph.md
│   │                   └── 03_Variables.md
│   │
│   ├── Packages/
│   │   └── manifest.json
│   │
│   └── ProjectSettings/
│       ├── ProjectVersion.txt
│       └── GraphicsSettings.asset
│
├── package.json                                   ← ДЛЯ UPM ПУБЛИКАЦИИ
├── README.md                                      ← ОБЩИЙ README
├── LICENSE                                        ← ЛИЦЕНЗИЯ
├── .gitignore                                     ← ИГНОРИРУЕМЫЕ ФАЙЛЫ
└── .gitattributes                                 ← АТРИБУТЫ GIT
```

## Доска
https://boardmix.com/app/editor/h_0dEe8kOR01dMc_kUaEPw

## Описание
<img width="1514" height="755" alt="image" src="https://github.com/user-attachments/assets/52eaeb24-0efa-4031-a068-af32ab81e876" />
