# Деплой Node2Code на учебный сервер УрФУ

## Быстрый вариант (одной командой из PowerShell)
Из корня проекта:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\deploy-to-urfu.ps1
```

Скрипт:
- архивирует проект (без `.git`, `node_modules`, `dist`);
- копирует архив на сервер;
- распаковывает в `/opt/node2code/CustomGameEngineModule`;
- запускает `docker compose up -d --build`.

## 1) Передача проекта на сервер
Выполнить с машины, у которой есть доступ во внутреннюю сеть УрФУ:

```bash
scp -r ./CustomGameEngineModule root@10.40.241.59:/opt/node2code
```

## 2) Запуск на сервере
Подключиться по SSH и выполнить:

```bash
cd /opt/node2code/CustomGameEngineModule
docker compose up -d --build
```

## 3) Проверка
На сервере:

```bash
docker compose ps
curl http://localhost/ping
```

Ожидаемый ответ `ping`:
```json
{"message":"pong","timestamp":"..."}
```

## 4) Остановка (при необходимости)
```bash
cd /opt/node2code/CustomGameEngineModule
docker compose down
```
