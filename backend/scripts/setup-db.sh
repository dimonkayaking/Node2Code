#!/bin/bash

# Цветной вывод
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

echo -e "${GREEN}=== UnityNodeBridge - Автоматическая настройка базы данных ===${NC}"

# Загрузка переменных окружения из .env, если файл существует
if [ -f .env ]; then
    export $(grep -v '^#' .env | xargs)
    echo -e "${GREEN}✓ Загружены переменные из .env${NC}"
else
    echo -e "${RED}✗ Файл .env не найден. Создайте его по образцу .env.example${NC}"
    exit 1
fi

# Проверка обязательных переменных
if [ -z "$DB_HOST" ] || [ -z "$DB_PORT" ] || [ -z "$DB_NAME" ] || [ -z "$DB_USER" ] || [ -z "$DB_PASSWORD" ]; then
    echo -e "${RED}✗ Одна или несколько переменных окружения не заданы (DB_HOST, DB_PORT, DB_NAME, DB_USER, DB_PASSWORD)${NC}"
    exit 1
fi

# Проверка наличия psql
if ! command -v psql &> /dev/null; then
    echo -e "${RED}✗ psql не найден. Установите PostgreSQL клиент.${NC}"
    exit 1
fi

# Экспорт пароля для psql
export PGPASSWORD=$DB_PASSWORD

# Функция выполнения SQL-файла
execute_sql_file() {
    local file=$1
    echo -e "${YELLOW}→ Выполняется $file...${NC}"
    psql -h $DB_HOST -p $DB_PORT -U $DB_USER -d $DB_NAME -f $file
    if [ $? -eq 0 ]; then
        echo -e "${GREEN}  ✓ Успешно${NC}"
    else
        echo -e "${RED}  ✗ Ошибка в $file${NC}"
        exit 1
    fi
}

# Проверка существования базы данных
DB_EXISTS=$(psql -h $DB_HOST -p $DB_PORT -U $DB_USER -lqt | cut -d \| -f 1 | grep -w "$DB_NAME" | wc -l)

if [ "$DB_EXISTS" -eq 0 ]; then
    echo -e "${YELLOW}→ База данных $DB_NAME не существует. Создаём...${NC}"
    createdb -h $DB_HOST -p $DB_PORT -U $DB_USER $DB_NAME
    if [ $? -eq 0 ]; then
        echo -e "${GREEN}  ✓ База данных создана${NC}"
    else
        echo -e "${RED}  ✗ Не удалось создать базу данных${NC}"
        exit 1
    fi
else
    echo -e "${GREEN}✓ База данных $DB_NAME уже существует${NC}"
fi

# Выполнение миграций (порядок важен)
echo -e "\n${YELLOW}=== Выполнение миграций ===${NC}"
execute_sql_file "migrations/001_create_users.sql"
execute_sql_file "migrations/002_create_lessons.sql"
execute_sql_file "migrations/003_create_progress.sql"

# Заполнение начальными данными (seeders)
echo -e "\n${YELLOW}=== Заполнение начальными данными ===${NC}"
execute_sql_file "seeders/001_initial_lessons.sql"

echo -e "\n${GREEN}=== Настройка базы данных завершена успешно! ===${NC}"