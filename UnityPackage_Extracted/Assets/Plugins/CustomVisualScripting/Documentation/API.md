## API для разработчиков

### Core API
- `ParserBridge.Parse(string code)` - парсинг кода в граф
- `GeneratorBridge.Generate(GraphData graph)` - генерация кода

### Integration API
- `GraphConverter.LogicToComplete()` - конвертация
- `GraphSaver.SaveToJson()` - сохранение
- `GraphLoader.LoadFromJson()` - загрузка