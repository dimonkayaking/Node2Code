const { DataTypes } = require('sequelize');
const sequelize = require('../config/db');

const NAME_REGEX = /^[A-Za-zА-Яа-яЁё\s'-]+$/;

const sanitizeName = (value) => value.trim().replace(/\s+/g, ' ');
const sanitizeEmail = (value) => value.trim().toLowerCase();

const User = sequelize.define('User', {
  id: {
    type: DataTypes.INTEGER,
    autoIncrement: true,
    primaryKey: true,
  },
  name: {
    type: DataTypes.STRING(100),
    allowNull: false,
    set(value) {
      this.setDataValue('name', sanitizeName(value));
    },
    validate: {
      notEmpty: { msg: 'Имя обязательно для заполнения' },
      len: {
        args: [2, 50],
        msg: 'Имя должно быть от 2 до 50 символов',
      },
      isValidName(value) {
        if (!NAME_REGEX.test(value)) {
          throw new Error('Имя содержит недопустимые символы');
        }
      },
    },
  },
  last_name: {
    type: DataTypes.STRING(100),
    allowNull: false,
    set(value) {
      this.setDataValue('last_name', sanitizeName(value));
    },
    validate: {
      notEmpty: { msg: 'Фамилия обязательна для заполнения' },
      len: {
        args: [2, 50],
        msg: 'Фамилия должна быть от 2 до 50 символов',
      },
      isValidLastName(value) {
        if (!NAME_REGEX.test(value)) {
          throw new Error('Фамилия содержит недопустимые символы');
        }
      },
    },
  },
  email: {
    type: DataTypes.STRING(255),
    allowNull: false,
    unique: true,
    set(value) {
      this.setDataValue('email', sanitizeEmail(value));
    },
    validate: {
      notEmpty: { msg: 'Email обязателен для заполнения' },
      isEmail: { msg: 'Некорректный формат email' },
      len: {
        args: [5, 255],
        msg: 'Email должен быть не длиннее 255 символов',
      },
    },
  },
  password_hash: {
    type: DataTypes.STRING(255),
    allowNull: false,
    validate: {
      notEmpty: { msg: 'Хэш пароля обязателен для заполнения' },
      len: {
        args: [20, 255],
        msg: 'Некорректное значение хэша пароля',
      },
    },
  },
  created_at: {
    type: DataTypes.DATE,
    defaultValue: DataTypes.NOW,
  },
  updated_at: {
    type: DataTypes.DATE,
    defaultValue: DataTypes.NOW,
  },
}, {
  tableName: 'users',
  timestamps: false,
});

module.exports = User;
