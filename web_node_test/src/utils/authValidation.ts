export type RegisterFormValues = {
  name: string;
  lastName: string;
  email: string;
  password: string;
  confirmPassword: string;
};

export type LoginFormValues = {
  email: string;
  password: string;
};

export type AuthValidationErrors<T extends string> = Partial<Record<T, string>>;

const NAME_REGEX = /^[A-Za-zА-Яа-яЁё\s'-]+$/;
const EMAIL_REGEX = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;

export const AUTH_LIMITS = {
  nameMin: 2,
  nameMax: 50,
  emailMax: 255,
  passwordMin: 8,
  passwordMax: 72,
} as const;

export const sanitizeName = (value: string) => value.trim().replace(/\s+/g, ' ');
export const sanitizeEmail = (value: string) => value.trim().toLowerCase();

export const validateName = (value: string, label: string) => {
  const sanitizedValue = sanitizeName(value);

  if (!sanitizedValue) {
    return `${label} обязательно для заполнения`;
  }

  if (sanitizedValue.length < AUTH_LIMITS.nameMin || sanitizedValue.length > AUTH_LIMITS.nameMax) {
    return `${label} должно быть от ${AUTH_LIMITS.nameMin} до ${AUTH_LIMITS.nameMax} символов`;
  }

  if (!NAME_REGEX.test(sanitizedValue)) {
    return `${label} может содержать только буквы, пробел, дефис и апостроф`;
  }

  return '';
};

export const validateEmail = (value: string) => {
  const sanitizedValue = sanitizeEmail(value);

  if (!sanitizedValue) {
    return 'Email обязателен для заполнения';
  }

  if (sanitizedValue.length > AUTH_LIMITS.emailMax) {
    return `Email не должен быть длиннее ${AUTH_LIMITS.emailMax} символов`;
  }

  if (!EMAIL_REGEX.test(sanitizedValue)) {
    return 'Введите корректный email';
  }

  return '';
};

export const validatePassword = (value: string) => {
  if (!value) {
    return 'Пароль обязателен для заполнения';
  }

  if (value.length < AUTH_LIMITS.passwordMin || value.length > AUTH_LIMITS.passwordMax) {
    return `Пароль должен быть от ${AUTH_LIMITS.passwordMin} до ${AUTH_LIMITS.passwordMax} символов`;
  }

  if (/\s/.test(value)) {
    return 'Пароль не должен содержать пробелы';
  }

  if (!/[A-Za-zА-Яа-яЁё]/.test(value) || !/\d/.test(value)) {
    return 'Пароль должен содержать хотя бы одну букву и одну цифру';
  }

  return '';
};

export const validateLoginForm = (values: LoginFormValues): AuthValidationErrors<keyof LoginFormValues> => {
  const errors: AuthValidationErrors<keyof LoginFormValues> = {};

  const emailError = validateEmail(values.email);
  if (emailError) {
    errors.email = emailError;
  }

  const passwordError = validatePassword(values.password);
  if (passwordError) {
    errors.password = passwordError;
  }

  return errors;
};

export const validateRegisterForm = (
  values: RegisterFormValues,
): AuthValidationErrors<keyof RegisterFormValues> => {
  const errors: AuthValidationErrors<keyof RegisterFormValues> = {};

  const nameError = validateName(values.name, 'Имя');
  if (nameError) {
    errors.name = nameError;
  }

  const lastNameError = validateName(values.lastName, 'Фамилия');
  if (lastNameError) {
    errors.lastName = lastNameError;
  }

  const emailError = validateEmail(values.email);
  if (emailError) {
    errors.email = emailError;
  }

  const passwordError = validatePassword(values.password);
  if (passwordError) {
    errors.password = passwordError;
  }

  if (!values.confirmPassword) {
    errors.confirmPassword = 'Подтвердите пароль';
  } else if (values.password !== values.confirmPassword) {
    errors.confirmPassword = 'Пароли не совпадают';
  }

  return errors;
};
