require('dotenv').config();
const sequelize = require('./config/db');
const User = require('./models/User');
const Lesson = require('./models/Lesson');
const Progress = require('./models/Progress');

(async () => {
  try {
    await sequelize.authenticate();
    console.log('✅ Подключение к БД успешно');

    // Синхронизация моделей (создаст таблицы, если их нет)
    await sequelize.sync({ alter: true });
    console.log('✅ Модели синхронизированы');

    // Создадим тестового пользователя
    const user = await User.create({
      name: 'Тест',
      last_name: 'Тестовый',
      email: 'test@example.com',
      password_hash: 'fakehash-value-long-enough-for-validation'
    });
    console.log('✅ Пользователь создан:', user.toJSON());

    // Найдём урок 1
    const lesson = await Lesson.findByPk(1);
    if (lesson) {
      console.log('✅ Урок 1 найден:', lesson.title);
    } else {
      console.log('⚠️ Урок 1 не найден – запустите seed');
    }

    // Создадим запись прогресса
    const progress = await Progress.create({
      user_id: user.id,
      lesson_id: 1,
      completed: true,
      completed_at: new Date()
    });
    console.log('✅ Прогресс создан:', progress.toJSON());

    // Проверим получение прогресса через контроллер (импортируем функцию)
    const { getProgress } = require('./controllers/progressController');
    // Но getProgress ожидает req/res – проще вызвать напрямую через модель
    const records = await Progress.findAll({ where: { user_id: user.id, completed: true } });
    console.log('✅ Прогресс пользователя:', records.map(r => r.lesson_id));

    // Очистим тестовые данные
    await Progress.destroy({ where: { user_id: user.id } });
    await User.destroy({ where: { id: user.id } });
    console.log('✅ Тестовые данные удалены');

    await sequelize.close();
  } catch (err) {
    console.error('❌ Ошибка:', err);
  }
})();
