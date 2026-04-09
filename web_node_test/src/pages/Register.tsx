import React, { useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { useAppContext } from '../context/AppContext';
import { ApiError } from '../utils/api';
import {
  sanitizeEmail,
  sanitizeName,
  validateRegisterForm,
  type RegisterFormValues,
} from '../utils/authValidation';
import './Auth.css';

const Register: React.FC = () => {
  const [name, setName] = useState('');
  const [lastName, setLastName] = useState('');
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [confirmPassword, setConfirmPassword] = useState('');
  const [errors, setErrors] = useState<Partial<Record<keyof RegisterFormValues, string>>>({});
  const [submitError, setSubmitError] = useState('');
  const [isSubmitting, setIsSubmitting] = useState(false);
  const navigate = useNavigate();
  const { register } = useAppContext();

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();

    const sanitizedName = sanitizeName(name);
    const sanitizedLastName = sanitizeName(lastName);
    const sanitizedEmail = sanitizeEmail(email);
    const formErrors = validateRegisterForm({
      name: sanitizedName,
      lastName: sanitizedLastName,
      email: sanitizedEmail,
      password,
      confirmPassword,
    });

    if (Object.keys(formErrors).length > 0) {
      setErrors(formErrors);
      setSubmitError('Исправьте ошибки в форме перед регистрацией');
      return;
    }

    setErrors({});
    setSubmitError('');
    setIsSubmitting(true);

    try {
      await register({
        name: sanitizedName,
        lastName: sanitizedLastName,
        email: sanitizedEmail,
        password,
      });
      navigate('/course');
    } catch (error) {
      if (error instanceof ApiError) {
        setErrors((error.fields as Partial<Record<keyof RegisterFormValues, string>>) || {});
        if (error.status === 409) {
          setSubmitError('Аккаунт с таким email уже существует. Войдите в него, и ваш сохраненный прогресс загрузится автоматически.');
        } else {
          setSubmitError(error.message);
        }
      } else {
        setSubmitError('Не удалось завершить регистрацию. Попробуйте ещё раз');
      }
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <div className="auth-container">
      <div className="auth-card">
        <h2>Регистрация</h2>
        <form onSubmit={handleSubmit} noValidate>
          <div className="form-group">
            <label htmlFor="register-name">Имя</label>
            <input
              id="register-name"
              type="text"
              value={name}
              onChange={(e) => setName(e.target.value)}
              onBlur={() => {
                const nextName = sanitizeName(name);
                setName(nextName);
                setErrors((prev) => ({
                  ...prev,
                  name: validateRegisterForm({
                    name: nextName,
                    lastName: sanitizeName(lastName),
                    email: sanitizeEmail(email),
                    password,
                    confirmPassword,
                  }).name,
                }));
              }}
              required
              minLength={2}
              maxLength={50}
              autoComplete="given-name"
              aria-invalid={Boolean(errors.name)}
              className={errors.name ? 'input-error' : ''}
              placeholder="Введите ваше имя"
            />
            {errors.name ? <p className="field-error">{errors.name}</p> : null}
          </div>
          <div className="form-group">
            <label htmlFor="register-last-name">Фамилия</label>
            <input
              id="register-last-name"
              type="text"
              value={lastName}
              onChange={(e) => setLastName(e.target.value)}
              onBlur={() => {
                const nextLastName = sanitizeName(lastName);
                setLastName(nextLastName);
                setErrors((prev) => ({
                  ...prev,
                  lastName: validateRegisterForm({
                    name: sanitizeName(name),
                    lastName: nextLastName,
                    email: sanitizeEmail(email),
                    password,
                    confirmPassword,
                  }).lastName,
                }));
              }}
              required
              minLength={2}
              maxLength={50}
              autoComplete="family-name"
              aria-invalid={Boolean(errors.lastName)}
              className={errors.lastName ? 'input-error' : ''}
              placeholder="Введите вашу фамилию"
            />
            {errors.lastName ? <p className="field-error">{errors.lastName}</p> : null}
          </div>
          <div className="form-group">
            <label htmlFor="register-email">Email</label>
            <input
              id="register-email"
              type="email"
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              onBlur={() => {
                const nextEmail = sanitizeEmail(email);
                setEmail(nextEmail);
                setErrors((prev) => ({
                  ...prev,
                  email: validateRegisterForm({
                    name: sanitizeName(name),
                    lastName: sanitizeName(lastName),
                    email: nextEmail,
                    password,
                    confirmPassword,
                  }).email,
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
            <label htmlFor="register-password">Пароль</label>
            <input
              id="register-password"
              type="password"
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              onBlur={() =>
                setErrors((prev) => ({
                  ...prev,
                  password: validateRegisterForm({
                    name: sanitizeName(name),
                    lastName: sanitizeName(lastName),
                    email: sanitizeEmail(email),
                    password,
                    confirmPassword,
                  }).password,
                }))
              }
              required
              minLength={8}
              maxLength={72}
              autoComplete="new-password"
              aria-invalid={Boolean(errors.password)}
              className={errors.password ? 'input-error' : ''}
              placeholder="Введите пароль"
            />
            {errors.password ? <p className="field-error">{errors.password}</p> : null}
          </div>
          <div className="form-group">
            <label htmlFor="register-confirm-password">Подтвердите пароль</label>
            <input
              id="register-confirm-password"
              type="password"
              value={confirmPassword}
              onChange={(e) => setConfirmPassword(e.target.value)}
              onBlur={() =>
                setErrors((prev) => ({
                  ...prev,
                  confirmPassword: validateRegisterForm({
                    name: sanitizeName(name),
                    lastName: sanitizeName(lastName),
                    email: sanitizeEmail(email),
                    password,
                    confirmPassword,
                  }).confirmPassword,
                }))
              }
              required
              minLength={8}
              maxLength={72}
              autoComplete="new-password"
              aria-invalid={Boolean(errors.confirmPassword)}
              className={errors.confirmPassword ? 'input-error' : ''}
              placeholder="Повторите пароль"
            />
            {errors.confirmPassword ? <p className="field-error">{errors.confirmPassword}</p> : null}
          </div>
          {submitError ? <p className="form-error">{submitError}</p> : null}
          <button type="submit" className="auth-button" disabled={isSubmitting}>
            {isSubmitting ? 'Регистрируем...' : 'Зарегистрироваться'}
          </button>
        </form>
        <p className="auth-link">
          Уже есть аккаунт? <Link to="/login">Войти</Link>
        </p>
      </div>
    </div>
  );
};

export default Register;
