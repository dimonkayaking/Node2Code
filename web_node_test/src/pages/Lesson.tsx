import React, { useEffect, useMemo, useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { useAppContext } from '../context/AppContext';
import { allLessonIds, courseModules, getLessonById } from '../data/courseData';
import './Lesson.css';

type VideoSource =
  | { kind: 'youtube'; url: string }
  | { kind: 'embed'; url: string }
  | { kind: 'direct'; url: string }
  | { kind: 'external'; url: string }
  | { kind: 'none'; url: null };

const getVideoSource = (url?: string): VideoSource => {
  if (!url) return { kind: 'none', url: null };

  const normalizedUrl = url.trim();
  if (!normalizedUrl) return { kind: 'none', url: null };

  // Support local static assets like /videos/lesson.mp4
  if (normalizedUrl.startsWith('/')) {
    const path = normalizedUrl.toLowerCase();
    return path.endsWith('.mp4')
      ? { kind: 'direct', url: normalizedUrl }
      : { kind: 'none', url: null };
  }

  try {
    const parsed = new URL(normalizedUrl);
    const hostname = parsed.hostname.toLowerCase();
    const pathname = parsed.pathname.toLowerCase();
    const filename = (parsed.searchParams.get('filename') || '').toLowerCase();
    const contentType = (parsed.searchParams.get('content_type') || '').toLowerCase();

    if (hostname.includes('youtu.be') || hostname.includes('youtube.com')) {
      const videoId = hostname.includes('youtu.be')
        ? parsed.pathname.slice(1)
        : parsed.searchParams.get('v');

      return videoId
        ? { kind: 'youtube', url: `https://www.youtube.com/embed/${videoId}` }
        : { kind: 'none', url: null };
    }

    if (hostname.includes('rutube.ru') && pathname.includes('/play/embed/')) {
      return { kind: 'embed', url: normalizedUrl };
    }

    const isDirectVideoLink =
      pathname.endsWith('.mp4') ||
      filename.endsWith('.mp4') ||
      contentType.startsWith('video/');

    if (isDirectVideoLink) {
      return { kind: 'direct', url: normalizedUrl };
    }

    return { kind: 'external', url: normalizedUrl };
  } catch {
    return { kind: 'none', url: null };
  }
};

const QUIZ_STORAGE_KEY = 'node2code_lesson_quiz_answers';

const getQuizStorageKey = (userId?: number) =>
  userId ? `${QUIZ_STORAGE_KEY}_${userId}` : `${QUIZ_STORAGE_KEY}_guest`;

const Lesson: React.FC = () => {
  const { id } = useParams();
  const navigate = useNavigate();
  const { user, markLessonCompleted } = useAppContext();
  const lessonId = id ? Number(id) : 1;
  const lessonEntry = getLessonById(lessonId) || getLessonById(1);

  const [code, setCode] = useState('// Опишите решение или вставьте свой фрагмент логики здесь');
  const [taskStatus, setTaskStatus] = useState('');
  const [taskSolved, setTaskSolved] = useState(false);
  const [isSavingProgress, setIsSavingProgress] = useState(false);
  const [selectedQuizAnswers, setSelectedQuizAnswers] = useState<Record<string, string>>({});

  const fallbackModule = courseModules[0];
  const fallbackLesson = fallbackModule.lessons[0];
  const module = lessonEntry?.module || fallbackModule;
  const lesson = lessonEntry?.lesson || fallbackLesson;
  const currentIndex = allLessonIds.indexOf(lesson.id);
  const nextLessonId = currentIndex >= 0 && currentIndex < allLessonIds.length - 1 ? allLessonIds[currentIndex + 1] : null;
  const selectedQuiz = lesson.quiz;
  const quizQuestions = useMemo(() => {
    if (!selectedQuiz) {
      return [];
    }

    if (selectedQuiz.questions && selectedQuiz.questions.length > 0) {
      return selectedQuiz.questions;
    }

    if (selectedQuiz.question && selectedQuiz.options) {
      return [{ id: `lesson-${lesson.id}`, question: selectedQuiz.question, options: selectedQuiz.options }];
    }

    return [];
  }, [selectedQuiz, lesson.id]);
  const hasSidePanel = Boolean(lesson.task) || quizQuestions.length > 0;
  const isTheoryOnlyLesson = !hasSidePanel;
  const videoSource = getVideoSource(lesson.videoUrl);
  const hasEmbeddedVideo = videoSource.kind === 'youtube' || videoSource.kind === 'direct' || videoSource.kind === 'embed';
  const quizStorageKey = getQuizStorageKey(user?.id);

  useEffect(() => {
    const sanitizeAnswers = (answers: Record<string, string>) =>
      quizQuestions.reduce<Record<string, string>>((acc, question) => {
        const answerId = answers[question.id];
        if (!answerId) {
          return acc;
        }

        const isValidOption = question.options.some((option) => option.id === answerId);
        if (isValidOption) {
          acc[question.id] = answerId;
        }
        return acc;
      }, {});

    try {
      const rawAnswers = window.localStorage.getItem(quizStorageKey);
      if (!rawAnswers) {
        setSelectedQuizAnswers({});
        return;
      }

      const savedAnswers = JSON.parse(rawAnswers) as Record<string, unknown>;
      const lessonSavedAnswer = savedAnswers[String(lesson.id)];

      if (lessonSavedAnswer && typeof lessonSavedAnswer === 'object') {
        setSelectedQuizAnswers(sanitizeAnswers(lessonSavedAnswer as Record<string, string>));
        return;
      }

      if (typeof lessonSavedAnswer === 'string' && quizQuestions[0]) {
        setSelectedQuizAnswers(sanitizeAnswers({ [quizQuestions[0].id]: lessonSavedAnswer }));
        return;
      }

      setSelectedQuizAnswers({});
    } catch {
      setSelectedQuizAnswers({});
    }
  }, [lesson.id, quizQuestions, quizStorageKey]);

  const handleQuizAnswerChange = (questionId: string, optionId: string) => {
    const question = quizQuestions.find((item) => item.id === questionId);
    const selectedOptionId = selectedQuizAnswers[questionId];
    const hasValidSavedAnswer = question?.options.some((option) => option.id === selectedOptionId);

    if (hasValidSavedAnswer) {
      return;
    }

    const nextAnswers = { ...selectedQuizAnswers, [questionId]: optionId };
    setSelectedQuizAnswers(nextAnswers);

    try {
      const rawAnswers = window.localStorage.getItem(quizStorageKey);
      const savedAnswers = rawAnswers ? (JSON.parse(rawAnswers) as Record<string, unknown>) : {};
      savedAnswers[String(lesson.id)] = nextAnswers;
      window.localStorage.setItem(quizStorageKey, JSON.stringify(savedAnswers));
    } catch {
      window.localStorage.setItem(quizStorageKey, JSON.stringify({ [lesson.id]: nextAnswers }));
    }
  };

  const checkTask = () => {
    const trimmed = code.trim();

    if (trimmed.length < 20) {
      setTaskStatus('Ответ слишком короткий. Добавьте более конкретное описание логики или код.');
      setTaskSolved(false);
      return;
    }

    if (lesson.successHint && !trimmed.toLowerCase().includes(trimmed.split(' ')[0].toLowerCase())) {
      setTaskStatus(`Ответ сохранён. Ориентир для самопроверки: ${lesson.successHint}`);
      setTaskSolved(true);
      return;
    }

    setTaskStatus(`Задание принято. Ориентир для самопроверки: ${lesson.successHint || 'сверьтесь с теорией урока и практикой модуля.'}`);
    setTaskSolved(true);
  };

  const goToNextLesson = async () => {
    setIsSavingProgress(true);
    try {
      await markLessonCompleted(lesson.id);

      if (nextLessonId) {
        navigate(`/lesson/${nextLessonId}`);
        return;
      }

      navigate('/instructions');
    } finally {
      setIsSavingProgress(false);
    }
  };

  const goToModule = () => {
    if (module?.id) {
      navigate(`/topic/${module.id}`);
      return;
    }

    navigate('/instructions');
  };

  return (
    <div className="lesson-page">
      <div className={`lesson-container ${isTheoryOnlyLesson ? 'lesson-container--theory-only' : ''}`}>
        <div className={`lesson-content ${isTheoryOnlyLesson ? 'lesson-content--centered' : ''}`}>
          <div className="lesson-header">
            <div>
              <button type="button" className="back-link" onClick={goToModule}>← Назад к модулю {module.order}</button>
              <h1>{lesson.title}</h1>
              <p className="lesson-subtitle">{module.title}</p>
            </div>
            <div className="lesson-header__meta">
              <span className="theme-tag">{lesson.duration}</span>
              <span className={`format-tag format-tag--${lesson.format}`}>
                {lesson.format === 'practice' ? 'Видео + задание' : 'Видео + теория'}
              </span>
            </div>
          </div>

          <div className="lesson-video">
            <div className={`video-player ${hasEmbeddedVideo ? 'video-player--embedded' : ''}`}>
              {videoSource.kind === 'youtube' && (
                <iframe
                  className="video-embed"
                  src={videoSource.url}
                  title={lesson.title}
                  allow="accelerometer; autoplay; clipboard-write; encrypted-media; gyroscope; picture-in-picture; web-share"
                  referrerPolicy="strict-origin-when-cross-origin"
                  allowFullScreen
                />
              )}
              {videoSource.kind === 'embed' && (
                <iframe
                  className="video-embed"
                  src={videoSource.url}
                  title={lesson.title}
                  allow="clipboard-write; autoplay"
                  allowFullScreen
                />
              )}
              {videoSource.kind === 'direct' && (
                <video className="video-embed" controls preload="metadata" src={videoSource.url}>
                  Ваш браузер не поддерживает воспроизведение видео.
                </video>
              )}
              {videoSource.kind === 'none' && (
                <div className="video-placeholder">
                  <div className="play-button">▶</div>
                  <p>Здесь будет встроенное видео урока</p>
                </div>
              )}
              {videoSource.kind === 'external' && (
                <div className="video-placeholder">
                  <div className="play-button">↗</div>
                  <p>Видео доступно по внешней ссылке</p>
                  <a href={videoSource.url} target="_blank" rel="noreferrer">
                    Открыть видео урока
                  </a>
                </div>
              )}
            </div>
          </div>

          <div className="lesson-text">
            <h2>О чем урок</h2>
            <p>{lesson.summary}</p>

            <h2>Теория</h2>
            <ul className="compact-points">
              {lesson.theory.map((paragraph) => (
                <li key={paragraph}>{paragraph}</li>
              ))}
            </ul>

            <div className="lesson-note">
              <strong>Цель модуля:</strong> {module.goal}
            </div>

            {lesson.cheatSheet && lesson.cheatSheet.length > 0 && (
              <div className="lesson-note">
                <strong>{lesson.cheatSheetTitle || 'Шпаргалка'}</strong>
                <ol className="compact-points compact-points--ordered">
                  {lesson.cheatSheet.map((item) => (
                    <li key={item}>{item}</li>
                  ))}
                </ol>
              </div>
            )}
          </div>

          {lesson.format === 'theory' && (
            <button className="next-lesson-btn" onClick={() => void goToNextLesson()} disabled={isSavingProgress}>
              {isSavingProgress ? 'Сохраняем...' : nextLessonId ? 'Следующий урок' : 'Вернуться к инструкции'}
            </button>
          )}
        </div>

        {hasSidePanel && (
          <aside className="lesson-task-area">
            <div className="task-box">
              {quizQuestions.length > 0 && selectedQuiz && (
                <div className="lesson-side-quiz">
                  <h2>Тест</h2>
                  {quizQuestions.map((quizQuestion, index) => {
                    const selectedOptionId = selectedQuizAnswers[quizQuestion.id] || null;
                    const selectedOption = quizQuestion.options.find((option) => option.id === selectedOptionId) || null;
                    const correctOption = quizQuestion.options.find((option) => option.isCorrect) || null;
                    const isQuestionLocked = Boolean(selectedOption);

                    return (
                      <div key={quizQuestion.id} className="lesson-note lesson-note--quiz">
                        <p className="lesson-quiz__question">
                          <strong>Вопрос {index + 1}.</strong> {quizQuestion.question}
                        </p>
                        <div className="lesson-quiz__options">
                          {quizQuestion.options.map((option) => (
                            <label
                              key={option.id}
                              className={`lesson-quiz__option ${selectedOptionId === option.id ? 'lesson-quiz__option--selected' : ''}`}
                            >
                              <input
                                type="radio"
                                name={`lesson-quiz-${lesson.id}-${quizQuestion.id}`}
                                value={option.id}
                                checked={selectedOptionId === option.id}
                                onChange={() => handleQuizAnswerChange(quizQuestion.id, option.id)}
                                disabled={isQuestionLocked}
                              />
                              <span>{option.text}</span>
                            </label>
                          ))}
                        </div>

                        {selectedOption && (
                          <>
                            <p
                              className={`lesson-quiz__result ${
                                selectedOption.isCorrect ? 'lesson-quiz__result--correct' : 'lesson-quiz__result--incorrect'
                              }`}
                            >
                              {selectedOption.isCorrect ? selectedQuiz.correctMessage : selectedQuiz.incorrectMessage}
                            </p>

                            {!selectedOption.isCorrect && correctOption && (
                              <p className="lesson-quiz__answer">
                                <strong>Правильный ответ:</strong> {correctOption.text}
                              </p>
                            )}
                          </>
                        )}
                      </div>
                    );
                  })}
                </div>
              )}

              {lesson.task && (
                <>
                <h2>Задание</h2>
                <p>{lesson.task || 'Зафиксируйте, как вы будете применять идеи урока на практике.'}</p>

                <div className="code-editor">
                  <textarea value={code} onChange={(e) => setCode(e.target.value)} className="code-input" spellCheck={false} />
                </div>

                <div className="code-controls">
                  <button onClick={checkTask} className="check-button">Проверить</button>
                </div>

                {taskStatus && <p className="task-status">{taskStatus}</p>}

                {taskSolved && (
                  <button className="next-lesson-btn" onClick={() => void goToNextLesson()} disabled={isSavingProgress}>
                    {isSavingProgress ? 'Сохраняем...' : nextLessonId ? 'Следующий урок' : 'Вернуться к инструкции'}
                  </button>
                )}
                </>
              )}
            </div>
          </aside>
        )}
      </div>
    </div>
  );
};

export default Lesson;
