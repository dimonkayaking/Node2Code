import React, { useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { useAppContext } from '../context/AppContext';
import { ApiError } from '../utils/api';
import { sanitizeEmail, validateLoginForm, type LoginFormValues } from '../utils/authValidation';
import './Auth.css';

const Login: React.FC = () => {
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [errors, setErrors] = useState<Partial<Record<keyof LoginFormValues, string>>>({});
  const [submitError, setSubmitError] = useState('');
  const [isSubmitting, setIsSubmitting] = useState(false);
  const navigate = useNavigate();
  const { login } = useAppContext();

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();

    const sanitizedEmail = sanitizeEmail(email);
    const formErrors = validateLoginForm({ email: sanitizedEmail, password });

    if (Object.keys(formErrors).length > 0) {
      setErrors(formErrors);
      setSubmitError('Исправьте ошибки в форме перед входом');
      return;
    }

    setErrors({});
    setSubmitError('');
    setIsSubmitting(true);

    try {
      await login({ email: sanitizedEmail, password });
      navigate('/instructions');
    } catch (error) {
      if (error instanceof ApiError) {
        setErrors((error.fields as Partial<Record<keyof LoginFormValues, string>>) || {});
        if (error.status === 404) {
          setSubmitError('Такого аккаунта нет. Сначала зарегистрируйтесь.');
        } else if (error.status === 401) {
          setSubmitError('Пароль неверный. Введите правильный пароль для вашего аккаунта.');
        } else {
          setSubmitError(error.message);
        }
      } else {
        setSubmitError('Не удалось выполнить вход. Попробуйте ещё раз');
      }
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <div className="auth-container">
      <div className="auth-card">
        <h2>Вход</h2>
        <form onSubmit={handleSubmit} noValidate>
          <div className="form-group">
            <label htmlFor="login-email">Email</label>
            <input
              id="login-email"
              type="email"
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              onBlur={() => {
                const nextEmail = sanitizeEmail(email);
                setEmail(nextEmail);
                setErrors((prev) => ({
                  ...prev,
                  email: validateLoginForm({ email: nextEmail, password }).email,
                }));
              }}
              required
              maxLength={255}
              autoComplete="email"
              aria-invalid={Boolean(errors.email)}
              className={errors.email ? 'input-error' : ''}
              placeholder="Введите ваш email"
            />
            {errors.email ? <p className="field-error">{errors.email}</p> : null}
          </div>
          <div className="form-group">
            <label htmlFor="login-password">Пароль</label>
            <input
              id="login-password"
              type="password"
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              onBlur={() =>
                setErrors((prev) => ({
                  ...prev,
                  password: validateLoginForm({ email: sanitizeEmail(email), password }).password,
                }))
              }
              required
              minLength={8}
              maxLength={72}
              autoComplete="current-password"
              aria-invalid={Boolean(errors.password)}
              className={errors.password ? 'input-error' : ''}
              placeholder="Введите ваш пароль"
            />
            {errors.password ? <p className="field-error">{errors.password}</p> : null}
          </div>
          {submitError ? <p className="form-error">{submitError}</p> : null}
          <button type="submit" className="auth-button" disabled={isSubmitting}>
            {isSubmitting ? 'Входим...' : 'Войти'}
          </button>
        </form>
        <p className="auth-link">
          Нет аккаунта? <Link to="/register">Зарегистрироваться</Link>
        </p>
      </div>
    </div>
  );
};

export default Login;
