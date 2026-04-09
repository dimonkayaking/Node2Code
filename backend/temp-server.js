require('dotenv').config();
const express = require('express');
const app = express();

// Middleware для парсинга JSON
app.use(express.json());

// Подключаем ваши роуты прогресса
const progressRoutes = require('./routes/progressRoutes');
app.use('/api/progress', progressRoutes);

// Простой маршрут для проверки, что сервер работает
app.get('/ping', (req, res) => {
  res.json({ message: 'pong', timestamp: new Date() });
});

const PORT = process.env.PORT || 5000;
app.listen(PORT, () => {
  console.log(`✅ Временный сервер запущен на http://localhost:${PORT}`);
  console.log(`   - GET  /api/progress/:userId`);
  console.log(`   - POST /api/progress`);
  console.log(`   - GET  /ping`);
});