import React, { useMemo, useState } from 'react';
import { Link } from 'react-router-dom';
import { useAppContext } from '../context/AppContext';
import { allLessonIds, courseModules } from '../data/courseData';
import './Dashboard.css';

const Dashboard: React.FC = () => {
  const { user, completedLessons, isProgressLoading } = useAppContext();
  const [activeTab, setActiveTab] = useState<'lessons' | 'progress' | 'keys'>('lessons');

  const lessons = useMemo(
    () =>
      courseModules.flatMap((module) =>
        module.lessons.map((lesson) => ({
          id: lesson.id,
          title: lesson.title,
          description: lesson.summary,
          duration: lesson.duration,
          completed: completedLessons.includes(lesson.id),
        })),
      ),
    [completedLessons],
  );

  const [trialKeys] = useState([
    { id: 1, key: 'CODE123-456-789', status: 'Активен', expires: '30.12.2026' },
    { id: 2, key: 'CODE456-789-123', status: 'Использован', expires: '15.12.2026' },
  ]);

  const totalLessons = allLessonIds.length;
  const completedCount = completedLessons.length;
  const progress = totalLessons > 0 ? Math.round((completedCount / totalLessons) * 100) : 0;

  const generateKey = () => {
    const newKey = `CODE${Math.random().toString(36).slice(2, 11).toUpperCase()}`;
    alert(`Новый ключ: ${newKey}`);
  };

  return (
    <div className="dashboard">
      <nav className="dashboard-nav">
        <div className="nav-container">
          <div className="logo">UnityNodeBridge</div>
          <div className="user-info">
            <span>Привет, {user ? `${user.name} ${user.lastName}` : 'гость'}!</span>
            <Link to="/" className="logout-btn">Выйти</Link>
          </div>
        </div>
      </nav>

      <div className="dashboard-content">
        <aside className="sidebar">
          <div className="user-profile">
            <div className="avatar">{user?.name?.[0]?.toUpperCase() || 'U'}</div>
            <h3>{user ? `${user.name} ${user.lastName}` : 'Пользователь'}</h3>
            <p>{isProgressLoading ? 'Загружаем прогресс...' : `Пройдено ${completedCount} из ${totalLessons}`}</p>
          </div>

          <nav className="sidebar-nav">
            <button className={activeTab === 'lessons' ? 'active' : ''} onClick={() => setActiveTab('lessons')}>
              Уроки
            </button>
            <button className={activeTab === 'progress' ? 'active' : ''} onClick={() => setActiveTab('progress')}>
              Прогресс
            </button>
            <button className={activeTab === 'keys' ? 'active' : ''} onClick={() => setActiveTab('keys')}>
              Мои ключи
            </button>
          </nav>
        </aside>

        <main className="main-content">
          {activeTab === 'lessons' && (
            <div className="lessons-tab">
              <h2>Доступные уроки</h2>
              <div className="progress-bar">
                <div className="progress" style={{ width: `${progress}%` }}></div>
              </div>
              <p className="progress-text">
                {isProgressLoading ? 'Загружаем прогресс...' : `Прогресс: ${progress}%`}
              </p>

              <div className="lessons-grid">
                {lessons.map((lesson) => (
                  <Link to={`/lesson/${lesson.id}`} key={lesson.id} className="lesson-card">
                    <div className="lesson-status">{lesson.completed ? '✓' : '○'}</div>
                    <h3>{lesson.title}</h3>
                    <p>{lesson.description}</p>
                    <div className="lesson-meta">
                      <span>{lesson.duration}</span>
                      <span className="start-lesson">{lesson.completed ? 'Повторить →' : 'Начать →'}</span>
                    </div>
                  </Link>
                ))}
              </div>
            </div>
          )}

          {activeTab === 'progress' && (
            <div className="progress-tab">
              <h2>Мой прогресс</h2>

              <div className="stats-grid">
                <div className="stat-card">
                  <div className="stat-value">{totalLessons}</div>
                  <div className="stat-label">Всего уроков</div>
                </div>
                <div className="stat-card">
                  <div className="stat-value">{completedCount}</div>
                  <div className="stat-label">Пройдено</div>
                </div>
                <div className="stat-card">
                  <div className="stat-value">{progress}%</div>
                  <div className="stat-label">Завершено</div>
                </div>
              </div>

              <h3>Детальный прогресс по урокам</h3>
              <div className="lesson-progress-list">
                {lessons.map((lesson) => (
                  <div key={lesson.id} className="lesson-progress-item">
                    <div className="lesson-info">
                      <h4>{lesson.title}</h4>
                      <span className={lesson.completed ? 'completed' : 'not-completed'}>
                        {lesson.completed ? 'Пройдено' : 'В процессе'}
                      </span>
                    </div>
                    <div className="progress-bar small">
                      <div className="progress" style={{ width: lesson.completed ? '100%' : '0%' }}></div>
                    </div>
                  </div>
                ))}
              </div>
            </div>
          )}

          {activeTab === 'keys' && (
            <div className="keys-tab">
              <h2>Мои триальные ключи</h2>

              <button onClick={generateKey} className="generate-key-btn">
                Сгенерировать новый ключ
              </button>

              <div className="keys-table">
                <table>
                  <thead>
                    <tr>
                      <th>Ключ</th>
                      <th>Статус</th>
                      <th>Истекает</th>
                    </tr>
                  </thead>
                  <tbody>
                    {trialKeys.map((key) => (
                      <tr key={key.id}>
                        <td><code>{key.key}</code></td>
                        <td>
                          <span className={`status-badge ${key.status === 'Активен' ? 'active' : 'used'}`}>
                            {key.status}
                          </span>
                        </td>
                        <td>{key.expires}</td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            </div>
          )}
        </main>
      </div>
    </div>
  );
};

export default Dashboard;
