const Progress = require('../models/Progress');
const User = require('../models/User');
const Lesson = require('../models/Lesson');

// Получить прогресс пользователя (список завершённых уроков)
const getProgress = async (req, res) => {
  try {
    const { userId } = req.params;
    const user = await User.findByPk(userId);
    if (!user) {
      return res.status(404).json({ error: 'User not found' });
    }

    const progressRecords = await Progress.findAll({
      where: { user_id: userId, completed: true },
      attributes: ['lesson_id'],
    });

    const completedLessons = progressRecords.map(record => record.lesson_id);
    res.json({ userId: parseInt(userId), completedLessons });
  } catch (error) {
    console.error(error);
    res.status(500).json({ error: 'Internal server error' });
  }
};

// Обновить прогресс (отметить урок пройденным или непройденным)
const updateProgress = async (req, res) => {
  try {
    const { userId, lessonId, completed } = req.body;

    if (!userId || !lessonId || completed === undefined) {
      return res.status(400).json({ error: 'Missing required fields' });
    }

    const user = await User.findByPk(userId);
    const lesson = await Lesson.findByPk(lessonId);
    if (!user || !lesson) {
      return res.status(404).json({ error: 'User or lesson not found' });
    }

    let progress = await Progress.findOne({
      where: { user_id: userId, lesson_id: lessonId },
    });

    if (progress) {
      progress.completed = completed;
      progress.completed_at = completed ? new Date() : null;
      progress.updated_at = new Date();
      await progress.save();
    } else {
      progress = await Progress.create({
        user_id: userId,
        lesson_id: lessonId,
        completed,
        completed_at: completed ? new Date() : null,
        updated_at: new Date(),
      });
    }

    res.json({
      message: 'Progress updated',
      progress: {
        userId: progress.user_id,
        lessonId: progress.lesson_id,
        completed: progress.completed,
        completed_at: progress.completed_at,
      },
    });
  } catch (error) {
    console.error(error);
    res.status(500).json({ error: 'Internal server error' });
  }
};

module.exports = { getProgress, updateProgress };