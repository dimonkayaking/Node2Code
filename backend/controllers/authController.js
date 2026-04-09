const { ValidationError, UniqueConstraintError } = require('sequelize');
const User = require('../models/User');
const { hashPassword, verifyPassword } = require('../utils/password');

const EMAIL_REGEX = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
const NAME_REGEX = /^[A-Za-zА-Яа-яЁё\s'-]+$/;

const sanitizeName = (value) => String(value || '').trim().replace(/\s+/g, ' ');
const sanitizeEmail = (value) => String(value || '').trim().toLowerCase();

const formatUser = (user) => ({
  id: user.id,
  name: user.name,
  lastName: user.last_name,
  email: user.email,
});

const validateRegisterPayload = ({ name, lastName, email, password }) => {
  const errors = {};

  if (!name || name.length < 2 || name.length > 50 || !NAME_REGEX.test(name)) {
    errors.name = 'Введите корректное имя';
  }

  if (!lastName || lastName.length < 2 || lastName.length > 50 || !NAME_REGEX.test(lastName)) {
    errors.lastName = 'Введите корректную фамилию';
  }

  if (!email || email.length > 255 || !EMAIL_REGEX.test(email)) {
    errors.email = 'Введите корректный email';
  }

  if (!password || password.length < 8 || password.length > 72 || /\s/.test(password)) {
    errors.password = 'Пароль должен быть от 8 до 72 символов без пробелов';
  } else if (!/[A-Za-zА-Яа-яЁё]/.test(password) || !/\d/.test(password)) {
    errors.password = 'Пароль должен содержать хотя бы одну букву и одну цифру';
  }

  return errors;
};

const validateLoginPayload = ({ email, password }) => {
  const errors = {};

  if (!email || email.length > 255 || !EMAIL_REGEX.test(email)) {
    errors.email = 'Введите корректный email';
  }

  if (!password) {
    errors.password = 'Введите пароль';
  }

  return errors;
};

const register = async (req, res) => {
  try {
    const payload = {
      name: sanitizeName(req.body.name),
      lastName: sanitizeName(req.body.lastName),
      email: sanitizeEmail(req.body.email),
      password: String(req.body.password || ''),
    };

    const errors = validateRegisterPayload(payload);
    if (Object.keys(errors).length > 0) {
      return res.status(400).json({ error: 'Validation failed', fields: errors });
    }

    const existingUser = await User.findOne({ where: { email: payload.email } });
    if (existingUser) {
      return res.status(409).json({
        error: 'User already exists',
        fields: { email: 'Пользователь с таким email уже существует' },
      });
    }

    const passwordHash = await hashPassword(payload.password);
    const user = await User.create({
      name: payload.name,
      last_name: payload.lastName,
      email: payload.email,
      password_hash: passwordHash,
    });

    return res.status(201).json({
      message: 'User registered',
      user: formatUser(user),
    });
  } catch (error) {
    if (error instanceof UniqueConstraintError) {
      return res.status(409).json({
        error: 'User already exists',
        fields: { email: 'Пользователь с таким email уже существует' },
      });
    }

    if (error instanceof ValidationError) {
      const fields = {};
      for (const item of error.errors) {
        if (item.path) {
          fields[item.path] = item.message;
        }
      }

      return res.status(400).json({ error: 'Validation failed', fields });
    }

    console.error(error);
    return res.status(500).json({ error: 'Internal server error' });
  }
};

const login = async (req, res) => {
  try {
    const payload = {
      email: sanitizeEmail(req.body.email),
      password: String(req.body.password || ''),
    };

    const errors = validateLoginPayload(payload);
    if (Object.keys(errors).length > 0) {
      return res.status(400).json({ error: 'Validation failed', fields: errors });
    }

    const user = await User.findOne({ where: { email: payload.email } });
    if (!user) {
      return res.status(404).json({
        error: 'User not found',
        fields: { email: 'Аккаунт с таким email не найден' },
      });
    }

    const isPasswordValid = await verifyPassword(payload.password, user.password_hash);
    if (!isPasswordValid) {
      return res.status(401).json({
        error: 'Invalid password',
        fields: { password: 'Неверный пароль' },
      });
    }

    return res.json({
      message: 'Login successful',
      user: formatUser(user),
    });
  } catch (error) {
    console.error(error);
    return res.status(500).json({ error: 'Internal server error' });
  }
};

module.exports = {
  register,
  login,
};
