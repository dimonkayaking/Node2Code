# Обращение к команде: Модуль Парсера / Транслятора (MVP — Финальная версия)

Привет, команда! 👋

Парсер и кодогенератор **полностью готовы** для стыковки с визуальной системой нод и UI редактора.  
Ниже — полный список реализованных фич, зафиксированный скоуп MVP и конкретные инструкции для каждого члена команды.

---

## 📋 Зафиксированный скоуп MVP (19 типов нод)

Это **финальный** список нод, который мы реализуем для презентации. Всё, что не в этом списке — **не входит в MVP**.

### Литералы (4 ноды)
| NodeType | Пример в коде | Порты |
|---|---|---|
| `VariableInt` | `5` | Выход: значение |
| `VariableFloat` | `5.0f`, `1.5` | Выход: значение |
| `VariableString` | `"hello"` | Выход: значение |
| `VariableBool` | `true`, `false` | Выход: значение |

### Переменные (3 ноды)
| NodeType | Пример в коде | Порты |
|---|---|---|
| `VariableDeclaration` | `int a = 5;` | Вход данных: `value`, Поток: `next` |
| `VariableRead` | `...a + 10...` | Выход: значение переменной |
| `VariableAssignment` | `a = 20;` | Вход данных: `value`, Поток: `next` |

### Математика (4 ноды)
| NodeType | Пример в коде | Порты |
|---|---|---|
| `MathAdd` | `a + b` | Входы: `left`, `right`, Выход: результат |
| `MathSubtract` | `a - b` | Входы: `left`, `right`, Выход: результат |
| `MathMultiply` | `a * b` | Входы: `left`, `right`, Выход: результат |
| `MathDivide` | `a / b` | Входы: `left`, `right`, Выход: результат |

### Сравнение (3 ноды) — НОВОЕ
| NodeType | Пример в коде | Порты |
|---|---|---|
| `CompareGreater` | `a > b` | Входы: `left`, `right`, Выход: bool |
| `CompareLess` | `a < b` | Входы: `left`, `right`, Выход: bool |
| `CompareEqual` | `a == b` | Входы: `left`, `right`, Выход: bool |

### Управление потоком (1 нода)
| NodeType | Пример в коде | Порты |
|---|---|---|
| `IfStatement` | `if (...) { } else { }` | Вход данных: `condition`, Поток: `true`, `false`, `next` |

### Unity-специфичные (3 ноды)
| NodeType | Пример в коде | Порты |
|---|---|---|
| `Vector3Create` | `new Vector3(x, y, z)` | Входы: `x`, `y`, `z`, Выход: Vector3 |
| `TransformPositionRead` | `transform.position` | Выход: Vector3 |
| `TransformPositionSet` | `transform.position = ...` | Вход данных: `value`, Поток: `next` |

### Вывод (1 нода)
| NodeType | Пример в коде | Порты |
|---|---|---|
| `DebugLog` | `Debug.Log(...)` | Вход данных: `message`, Поток: `next` |

---

## 🎯 Целевой демо-скрипт для защиты

Этот скрипт проходит полный цикл парсинга и генерации (покрыт тестом `TestFullDemoScript`):

```csharp
float speed = 5;
float offset = speed * 0.1;
if (speed > 3)
{
    transform.position = new Vector3(offset, 0, 0);
    UnityEngine.Debug.Log(speed);
}
else
{
    UnityEngine.Debug.Log(0);
}
```

---

## 🛑 Что НЕ входит в MVP (не реализуем)

- ❌ Циклы (`for`, `while`, `foreach`)
- ❌ Массивы и коллекции (`List<T>`, `int[]`)
- ❌ Пользовательские методы и классы
- ❌ `GetComponent<T>()` и дженерики
- ❌ `switch/case`, `return`
- ❌ Логические операторы (`&&`, `||`, `!`)
- ❌ Корутины, async/await
- ❌ Обработка событий Unity (OnCollisionEnter и т.д.)
- ❌ Оптимизация (кэширование, асинхронный парсинг)

---

## 🤝 Задачи для каждого члена команды

### 👨‍💻 Разработчику №1 (Система нод)

Твоя задача — реализовать 19 визуальных нод в Unity `GraphView` и написать **`GraphSerializer`** — класс-конвертер между `GraphData` (модель парсера) и визуальным представлением (Unity GraphView).

**Ключевые моменты:**
1. Все порты чётко описаны в таблице выше. У каждой ноды есть входы данных (`InputConnections`) и выходы потока выполнения (`ExecutionFlow`).
2. Ноды делятся на два типа:
   - **Ноды-выражения** (Литералы, VariableRead, Math, Compare, Vector3Create, TransformPositionRead) — у них нет потока выполнения, только данные.
   - **Ноды-инструкции** (VariableDeclaration, VariableAssignment, DebugLog, IfStatement, TransformPositionSet) — у них есть поток выполнения (белые провода `next`, `true`, `false`).
3. Начни с `GraphSerializer.cs` — напиши метод `GraphDataToVisual(GraphData graph)`, который создаёт ноды и провода на экране. Затем `VisualToGraphData()` — обратную конвертацию.

### 👨‍💼 Тимлиду / Архитектору интерфейсов

Вызов парсера и генератора:
```csharp
// Код → Ноды
var parser = new RoslynCodeParser();
ParseResult result = parser.Parse(codeText);
if (result.HasErrors) {
    foreach(var err in result.Errors)
        Debug.LogError(err);   // Покажи ошибки красным
} else {
    graphView.LoadFromGraphData(result.Graph);  // Код Разработчика №1
}

// Ноды → Код
GraphData graph = graphView.SaveToGraphData();  // Код Разработчика №1
var generator = new SimpleCodeGenerator();
string code = generator.GenerateCode(graph);
codeEditor.SetText(code);
```

### 📝 Аналитику / Тех. райтеру

Используй **целевой демо-скрипт** выше как основу для сценария видеоурока. Покажи:
1. Пользователь пишет код в текстовом поле
2. Нажимает тумблер → появляются ноды
3. Меняет значение `speed` в ноде
4. Нажимает обратно → код обновился
5. Кубик на сцене движется

---

## 📊 Текущее покрытие тестами (12 тестов)

| Тест | Что проверяет |
|---|---|
| `TestParseAndGenerate` | Базовый цикл: `Debug.Log(1 + 2)` → граф → код |
| `TestVariableDeclarationAndAssignment` | Переменные + Execution Flow |
| `TestVariableDeclarationWithoutInitializer` | `int b;` без значения |
| `TestUnityTransformAndVector3` | `transform.position = new Vector3(...)` |
| `TestIfStatement` | Базовый if/else |
| `TestIfWithMultipleStatements` | If-блок с несколькими инструкциями |
| `TestDoubleLiteral` | Литерал `1.5` без суффикса `f` |
| `TestCyclicGraphDoesNotCrash` | Защита от циклических графов |
| `TestSyntaxError` | Некорректный код → `HasErrors = true` |
| `TestComparisonOperators` | `if (speed > 3)` с оператором `>` |
| `TestLessThanAndEqual` | Оператор `<` |
| `TestFullDemoScript` | **Полный демо-скрипт для защиты** (интеграционный тест) |

Все **12 тестов проходят**, сборка без ошибок и предупреждений.

---

## 📁 Предлагаемая структура плагина в Unity

```
Assets/Plugins/CustomVisualScripting/
├── Core/                          ← Разработчик №2 (Парсер) — DLL файлы
│   ├── VisualScripting.Core.dll
│   └── Microsoft.CodeAnalysis.*.dll
├── Editor/                        ← Тимлид + Разработчик №1
│   ├── Windows/                       Главное окно EditorWindow, тумблер, панель ошибок
│   ├── Graph/                         GraphView, визуальные ноды, провода, GraphSerializer
│   └── Styles/                        USS-стили
├── Runtime/                       ← Тимлид + Разработчик №1
│   ├── VisualScript.cs                MonoBehaviour на GameObject
│   └── GraphAsset.cs                  ScriptableObject для хранения графа
└── Documentation/                 ← Техрайтер
```

**Опасная точка для merge-конфликтов**: файл `Models/GraphData.cs`. Изменения в нём только по согласованию!
