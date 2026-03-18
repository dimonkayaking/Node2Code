## Структура проекта (на данный момент)
```plaintext
CustomGameEngineModule/
│
├── 📂 VisualScripting.Core/                  ← Ядро парсера (Разработчик №2)
│   ├── VisualScripting.Core.csproj                Проект .NET (зависимость: Microsoft.CodeAnalysis.CSharp)
│   ├── 📂 Models/
│   │   └── GraphData.cs                           Контракт данных: NodeType, NodeData, GraphData
│   ├── 📂 Parsers/
│   │   └── RoslynCodeParser.cs                    Парсер: C# код → GraphData (через Roslyn AST)
│   └── 📂 Generators/
│       └── SimpleCodeGenerator.cs                 Генератор: GraphData → C# код
│
├── 📂 VisualScripting.Tests/                 ← Юнит-тесты (xUnit, 12 тестов)
│   ├── VisualScripting.Tests.csproj
│   └── ParserCodegenTests.cs                      Все тесты парсера и генератора
│
├── VisualScripting.slnx                      ← Файл решения .NET
├── TEAM_UPDATE.md                            ← Обращение к команде (скоуп MVP, инструкции по ролям)
├── README.md                                 ← Этот файл
└── .gitignore
```

## Доска
https://boardmix.com/app/editor/h_0dEe8kOR01dMc_kUaEPw

## Описание
<img width="1514" height="755" alt="image" src="https://github.com/user-attachments/assets/52eaeb24-0efa-4031-a068-af32ab81e876" />
