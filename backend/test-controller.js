require('dotenv').config();
const sequelize = require('./config/db');
const User = require('./models/User');
const Lesson = require('./models/Lesson');
const Progress = require('./models/Progress'); // добавлен импорт
const { getProgress, updateProgress } = require('./controllers/progressController');

// Мокаем объекты req, res
const mockReq = (params = {}, body = {}) => ({ params, body });
const mockRes = () => {
  const res = {};
  res.status = (code) => { res.statusCode = code; return res; };
  res.json = (data) => { res.data = data; return res; };
  return res;
};

(async () => {
  try {
    await sequelize.authenticate();
    console.log('✅ Подключение к БД');

    // Удаляем пользователя с тестовым email, если он существует (чтобы избежать дубликата)
    const existingUser = await User.findOne({ where: { email: 'controller@test.com' } });
    if (existingUser) {
      // Удаляем связанные записи прогресса
      await Progress.destroy({ where: { user_id: existingUser.id } });
      await existingUser.destroy();
      console.log('🗑️ Удалён существующий тестовый пользователь');
    }

    // Создаём тестового пользователя
    const user = await User.create({
      name: 'Контроллер',
      last_name: 'Тест',
      email: 'controller@test.com',
      password_hash: 'controller-hash-value-long-enough'
    });
    console.log('✅ Создан пользователь:', user.id);

    // Находим урок 1 (или создаём временный, если нет)
    let lesson = await Lesson.findByPk(1);
    if (!lesson) {
      lesson = await Lesson.create({
        id: 999,
        module_id: 1,
        order_num: 99,
        title: 'Тестовый урок',
        summary: 'Для проверки контроллера',
        duration: '5 мин',
        format: 'theory',
        theory: []
      });
      console.log('📚 Создан временный урок (id=999)');
    } else {
      console.log('📚 Найден урок id=1');
    }

    // Тест getProgress
    const reqGet = mockReq({ userId: user.id });
    const resGet = mockRes();
    await getProgress(reqGet, resGet);
    console.log('GET прогресс:', resGet.data);

    // Тест updateProgress
    const reqPost = mockReq({}, { userId: user.id, lessonId: lesson.id, completed: true });
    const resPost = mockRes();
    await updateProgress(reqPost, resPost);
    console.log('POST результат:', resPost.data);

    // Повторный GET
    const reqGet2 = mockReq({ userId: user.id });
    const resGet2 = mockRes();
    await getProgress(reqGet2, resGet2);
    console.log('GET после обновления:', resGet2.data);

    // Очистка
    await Progress.destroy({ where: { user_id: user.id } });
    await User.destroy({ where: { id: user.id } });
    if (lesson.id === 999) {
      await Lesson.destroy({ where: { id: 999 } });
    }
    console.log('🧹 Тестовые данные удалены');
  } catch (err) {
    console.error('❌ Ошибка:', err);
  } finally {
    await sequelize.close();
  }
})();
