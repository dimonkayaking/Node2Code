# Backend для платформы UnityNodeBridge

## 📌 Общее описание

Backend-часть проекта **UnityNodeBridge** – образовательной платформы для изучения Unity и визуального программирования через плагин.

**Реализовано:**
- Модели данных (Sequelize ORM) для PostgreSQL: `User`, `Lesson`, `Progress`
- SQL-миграции для создания таблиц
- Seed-скрипт для загрузки начальных данных (урок 1.1)
- API эндпоинты для работы с прогрессом
- Автоматический скрипт настройки базы данных
- Тестовые скрипты для проверки моделей, контроллеров и API

> Аутентификация (регистрация / вход) и основной сервер (`server.js`) разрабатываются другим участником команды.

---

## 🚀 Инструкция по использованию

### 1. Настройка окружения

Создайте файл `.env` в корне `backend/` (не коммитить!):

```env
DB_HOST=localhost
DB_PORT=5432
DB_NAME=unity_bridge
DB_USER=postgres
DB_PASSWORD=ваш_пароль
PORT=5000
```

### 2. Подготовка базы данных

Убедитесь, что PostgreSQL запущен. Выполните автоматическую настройку:

```bash
node scripts/setup-db.js
```

Скрипт создаст базу данных (если её нет), выполнит миграции и загрузит урок 1.1.

### 3. Интеграция с основным сервером

При подключении роутов в `server.js`:

```javascript
const progressRoutes = require('./routes/progressRoutes');
app.use('/api/progress', progressRoutes);
```

Все модели и контроллеры готовы к использованию.

---

## 📡 API эндпоинты (прогресс)

Базовый URL: `http://localhost:5000/api/progress`

### 1. Получить прогресс пользователя

**Запрос:**  
`GET /api/progress/:userId`

**Параметры пути:**  
- `userId` – число, ID пользователя

**Пример:**  
`GET http://localhost:5000/api/progress/3`

**Успешный ответ (200 OK):**
```json
{
  "userId": 3,
  "completedLessons": [1, 5, 7]
}
```

**Ошибки:**
- `404 Not Found` – пользователь не найден
  ```json
  { "error": "User not found" }
  ```
- `500 Internal Server Error`
  ```json
  { "error": "Internal server error" }
  ```

---

### 2. Обновить прогресс (отметить урок пройденным / непройденным)

**Запрос:**  
`POST /api/progress`

**Тело запроса (JSON):**
```json
{
  "userId": 3,
  "lessonId": 1,
  "completed": true
}
```

| Поле       | Тип    | Обязательный | Описание                          |
|------------|--------|--------------|-----------------------------------|
| userId     | число  | Да           | ID пользователя                   |
| lessonId   | число  | Да           | ID урока                          |
| completed  | boolean| Да           | true – пройден, false – не пройден |

**Успешный ответ (200 OK):**
```json
{
  "message": "Progress updated",
  "progress": {
    "userId": 3,
    "lessonId": 1,
    "completed": true,
    "completed_at": "2026-04-05T12:58:38.390Z"
  }
}
```

**Ошибки:**
- `400 Bad Request` – отсутствуют обязательные поля
  ```json
  { "error": "Missing required fields" }
  ```
- `404 Not Found` – пользователь или урок не найдены
  ```json
  { "error": "User or lesson not found" }
  ```
- `500 Internal Server Error`
  ```json
  { "error": "Internal server error" }
  ```

> **Примечание:** `completed_at` возвращается только если `completed = true`, иначе `null`. Даты в формате ISO 8601 (UTC).

---

## 🗄 Структура базы данных

### Таблица `users`
| Колонка        | Тип                     | Ограничения                |
|----------------|-------------------------|----------------------------|
| id             | SERIAL                  | PRIMARY KEY                |
| name           | VARCHAR(100)            | NOT NULL                   |
| last_name      | VARCHAR(100)            | NOT NULL                   |
| email          | VARCHAR(255)            | UNIQUE, NOT NULL           |
| password_hash  | VARCHAR(255)            | NOT NULL                   |
| created_at     | TIMESTAMP               | DEFAULT CURRENT_TIMESTAMP  |
| updated_at     | TIMESTAMP               | DEFAULT CURRENT_TIMESTAMP  |

### Таблица `lessons`
| Колонка       | Тип                     | Ограничения                |
|---------------|-------------------------|----------------------------|
| id            | INTEGER                 | PRIMARY KEY                |
| module_id     | INTEGER                 | NOT NULL                   |
| order_num     | INTEGER                 | NOT NULL                   |
| title         | VARCHAR(255)            | NOT NULL                   |
| summary       | TEXT                    |                            |
| duration      | VARCHAR(50)             |                            |
| format        | VARCHAR(20)             | 'theory' или 'practice'    |
| theory        | JSONB                   | массив строк               |
| task          | TEXT                    |                            |
| success_hint  | TEXT                    |                            |
| created_at    | TIMESTAMP               | DEFAULT CURRENT_TIMESTAMP  |

### Таблица `progress`
| Колонка       | Тип                     | Ограничения                |
|---------------|-------------------------|----------------------------|
| id            | SERIAL                  | PRIMARY KEY                |
| user_id       | INTEGER                 | REFERENCES users(id) ON DELETE CASCADE |
| lesson_id     | INTEGER                 | REFERENCES lessons(id) ON DELETE CASCADE |
| completed     | BOOLEAN                 | DEFAULT FALSE              |
| completed_at  | TIMESTAMP               |                            |
| updated_at    | TIMESTAMP               | DEFAULT CURRENT_TIMESTAMP  |

**Уникальное ограничение:** `UNIQUE(user_id, lesson_id)`

---

## 🧪 Тестирование (без основного сервера)

### 1. Проверка моделей
```bash
node test-models.js
```

### 2. Проверка контроллеров прогресса
```bash
node test-controller.js
```

### 3. Проверка API через временный Express-сервер
```bash
node temp-server.js
```
Затем отправляйте запросы через curl или REST Client (например, расширение для VS Code).

---

## 📁 Структура проекта

```
backend/
├── config/
│   └── db.js
├── models/
│   ├── User.js
│   ├── Lesson.js
│   └── Progress.js
├── controllers/
│   └── progressController.js
├── routes/
│   └── progressRoutes.js
├── migrations/
│   ├── 001_create_users.sql
│   ├── 002_create_lessons.sql
│   └── 003_create_progress.sql
├── seeders/
│   └── 001_initial_lessons.sql
├── scripts/
│   └── setup-db.js
├── test-models.js
├── test-controller.js
├── temp-server.js
├── .env.example
└── README.md
```

---

## ⚙️ Развёртывание на сервере

1. Установите Node.js и PostgreSQL.
2. Создайте файл `.env` с реальными параметрами подключения.
3. Запустите настройку БД: `node scripts/setup-db.js`.
4. Запустите основной сервер (разрабатывается отдельно).