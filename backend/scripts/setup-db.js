const { Client } = require('pg');
const fs = require('fs');
const path = require('path');
require('dotenv').config();

const config = {
  host: process.env.DB_HOST,
  port: process.env.DB_PORT,
  user: process.env.DB_USER,
  password: process.env.DB_PASSWORD,
  database: process.env.DB_NAME,
};

const executeSqlFile = async (client, filePath) => {
  const sql = fs.readFileSync(filePath, 'utf8');
  await client.query(sql);
  console.log(`✓ ${path.basename(filePath)}`);
};

const setup = async () => {
  // Подключаемся к postgres (без указания БД) для создания БД
  const adminClient = new Client({ ...config, database: 'postgres' });
  await adminClient.connect();

  // Проверяем существование БД
  const res = await adminClient.query(`SELECT 1 FROM pg_database WHERE datname = $1`, [config.database]);
  if (res.rowCount === 0) {
    await adminClient.query(`CREATE DATABASE ${config.database}`);
    console.log(`✓ База данных ${config.database} создана`);
  } else {
    console.log(`✓ База данных ${config.database} уже существует`);
  }
  await adminClient.end();

  // Подключаемся к целевой БД
  const client = new Client(config);
  await client.connect();

  // Миграции
  const migrations = ['001_create_users.sql', '002_create_lessons.sql', '003_create_progress.sql'];
  for (const file of migrations) {
    await executeSqlFile(client, path.join(__dirname, '..', 'migrations', file));
  }

  // Сиды
  await executeSqlFile(client, path.join(__dirname, '..', 'seeders', '001_initial_lessons.sql'));

  await client.end();
  console.log('=== Настройка базы данных завершена ===');
};

setup().catch(err => {
  console.error('Ошибка:', err);
  process.exit(1);
});