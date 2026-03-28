# Обращение к команде: парсер, граф и плагин (MVP)

Привет, команда!

Ниже — **актуальное состояние** после перехода парсера на **Roslyn** и согласования контракта графа с тимлидом (строго C#, без циклов и степени на этом этапе). Старые формулировки про «19 нод с VariableInt / Debug.Log» и 12 тестов **заменены** этим текстом.

---

## Обновление от 28.03.2026 — что сделано (Backend 1 + интеграция)

| Область | Изменение |
|--------|-----------|
| **Парсер** | `RoslynCodeParser` переписан на **Microsoft.CodeAnalysis** (обход AST, без Regex и без Shunting Yard). |
| **Модель** | В `NodeData` добавлено поле **`VariableName`**. Парсер **не заполняет** `ExecutionFlow` / `InputConnections` — только **`Edges`** с именами портов по контракту. |
| **`NodeType`** | Добавлены: `MathModulo`, `CompareNotEqual`, `CompareGreaterOrEqual`, `CompareLessOrEqual`, `LogicalAnd`, `LogicalOr`, `LogicalNot`, `FlowElse`. Enum в Core и в `UnityPackage_Extracted` **синхронизирован**. |
| **Рёбра** | Единая схема портов: математика `inputA` / `inputB` / `output`; сравнения и логика (`&&`, ИЛИ, `!`) — `left` / `right` / `result` или `input` / `result` для `!`; `If` — `condition`, поток `true` / `false`; выполнение — `execIn` / `execOut`. |
| **Присваивание** | Результат — **один узел операции** с `VariableName` (без отдельного узла объявления для `z` в `int z = x + y`). Ссылки на переменные — **прямые рёбра** к узлу-источнику, **без VariableGet**. |
| **if / else if / else** | Цепочка **`FlowIf`** по ветке `false`, финальный **`FlowElse`**; тела — рёбра потока между инструкциями. |
| **Editor-ноды** | `CustomBaseNode` наследует **`BaseExecutionNode`** (есть `execIn` / `execOut`). Обновлены порты у математики и литералов. Добавлены **7 новых нод** + **`ElseNode`**. **`IfNode`** переведён на потоковую модель (`condition` + выходы `true` / `false`). |
| **Интеграция** | `GraphConverter` / `GraphSerializer` — цвета и имена для новых типов. `ParserBridge` логирует число нод и рёбер после парса. |
| **Генератор** | `SimpleCodeGenerator.GenerateCode` — **MVP-подмножество**: литералы с именем + бинарная математика по `Edges` (для простого roundtrip в тестах). |
| **Тесты** | Удалён устаревший `ParserCodegenTests` (ожидал другой парсер и Unity-скрипты). Добавлен **`RoslynParserMvpTests`** (5 тестов), `dotnet test` зелёный. |
| **Runtime** | `NodeExecutor` переведён на **`switch` по `NodeType`**, чтение входов из **`Edges`** (плюс legacy `InputConnections`). `GraphRunner` передаёт граф в executor. |

**Файлы «источник правды» в репозитории:** `VisualScripting.Core/` (парсер, модели, генератор, тесты); зеркало под Unity — `UnityPackage_Extracted/Assets/Plugins/CustomVisualScripting/`.

---

## Контракт портов (кратко, для Backend 2 и Fullstack)

| Тип узла | Входы | Выходы |
|----------|--------|--------|
| Литералы | — | `output` |
| Math (Add, Subtract, Multiply, Divide, Modulo) | `inputA`, `inputB` | `output` |
| Compare (Equal, Greater, Less, NotEqual, >=, <=) | `left`, `right` | `result` |
| And / Or | `left`, `right` | `result` |
| Not | `input` | `result` |
| If | `execIn`, `condition` | `true`, `false`, `execOut` (база) |
| Else | `execIn` | `execOut` |
| Прочие (Unity, переменные) | см. существующие ноды | — |

`VariableName`: непустой только у литерала с объявлением и у **корневого** узла результата присваивания; у промежуточных операций — пустая строка.

---

## Скоуп MVP парсера (что разбирается сейчас)

**Поддерживается:**

- Объявления `int x = …`, `float`, `string`, `bool` **с инициализатором**.
- Простое присваивание `z = …` (правая часть — выражение MVP).
- Арифметика: `+ - * / %`, скобки, приоритеты из Roslyn.
- Сравнения: `== != > < >= <=`.
- Логика: `&&`, `||`, `!`.
- `if` / `else if` / `else` с блоками или одной инструкцией.
- Вложенные `if`.

**Пока не поддерживается парсером (ошибка или «неподдерживаемая конструкция»):**

- Циклы, вызовы методов (`Debug.Log`, `Math.Pow`, …), Unity-специфика (`transform`, `Vector3`, …).
- Объявление без инициализатора, составные присваивания (`+=`), и т.д.

Оператор степени в стиле Python и циклы в ТЗ MVP v3 **не входят**.

---

## Что НЕ входит в текущий MVP парсера (напоминание)

- Циклы (`for`, `while`)
- Массивы, коллекции, методы, дженерики
- `switch`, `return`, корутины, async
- Генерация полного демо-скрипта с `transform` / `Debug.Log` из старого описания — до появления поддержки в парсере или отдельной задачи

---

## Пример кода для ручной проверки парсера в редакторе

```csharp
int x = 10;
int y = 20;
int z = x + y * 2 % 3;
bool flag = true;
if (x >= y && z != 0 || !flag)
{
    z = x + y;
}
else
{
    z = x - y;
}
```

Ожидается: ноды литералов, `Multiply` → `Modulo` → `Add`, цепочка логики к `FlowIf`, ветка `FlowElse`, рёбра с портами из таблицы выше.

---

## Задачи по ролям (обновлено)

### Backend 1 (парсер / мост) — статус

Основной объём по плану MVP v3 в репозитории **влит**. Дальше — доработки по согласованию (например, расширение генератора, новые конструкции).

### Backend 2 (генератор / рантайм)

- Ориентироваться на **`Edges`** и **`VariableName`**, а не на `ExecutionFlow` от парсера.
- Расширять `SimpleCodeGenerator` под `if`, сравнения и логику по мере необходимости; согласовать порядок эмиссии инструкций с топологией графа.

### Fullstack / визуальный граф (GraphProcessor)

- Подключить отображение новых типов нод и проводов с **именами портов из контракта**.
- Доработать **`GraphSerializer`**: сериализация/десериализация **`Edges`** (сейчас в коде помечено TODO).

### Тимлид / сценарий демо

- Для записи демо использовать **фрагменты из раздела «Пример кода»** или согласовать новый целевой скрипт после расширения парсера под Unity.

---

## Вызов API (без изменений по смыслу)

```csharp
// Код → граф
var result = ParserBridge.Parse(codeText);
if (result.HasErrors) {
    foreach (var err in result.Errors)
        Debug.LogError(err);
} else {
    // загрузка result.Graph в UI / GraphView
}

// Граф → код (MVP генератора — ограниченный подмножество)
var generator = new SimpleCodeGenerator();
string code = generator.GenerateCode(graph);
```

---

## Тесты (актуально)

| Файл / класс | Количество | Назначение |
|----------------|------------|------------|
| `RoslynParserMvpTests.cs` | 5 | Арифметика с `%`, if + логика, вложенный if, синтаксическая ошибка, roundtrip `int x,y,z` через генератор. |

Запуск: из корня репозитория `dotnet test VisualScripting.Tests/VisualScripting.Tests.csproj`.

---

## Структура в репозитории

```
VisualScripting.Core/           ← парсер, модели, SimpleCodeGenerator, тесты (ссылка из .Tests)
UnityPackage_Extracted/Assets/Plugins/CustomVisualScripting/
├── Core/                      ← зеркало моделей и RoslynCodeParser (исходники; в Unity нужны ещё DLL Roslyn)
├── Editor/Nodes/              ← визуальные ноды (GraphProcessor)
├── Integration/               ← ParserBridge, GraphConverter
└── Runtime/Execution/         ← NodeExecutor, GraphRunner
```

**Точка внимания при merge:** `NodeType.cs`, `NodeData.cs`, `GraphData.cs`, `RoslynCodeParser.cs` — правки только по согласованному контракту.

---

Если нужна короткая выжимка для чата команды — можно переслать только раздел **«Обновление от 28.03.2026»** и таблицу **«Контракт портов»**.
