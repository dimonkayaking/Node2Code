const { DataTypes } = require('sequelize');
const sequelize = require('../config/db');

const Lesson = sequelize.define('Lesson', {
  id: {
    type: DataTypes.INTEGER,
    primaryKey: true,
  },
  module_id: {
    type: DataTypes.INTEGER,
    allowNull: false,
  },
  order_num: {
    type: DataTypes.INTEGER,
    allowNull: false,
  },
  title: {
    type: DataTypes.STRING(255),
    allowNull: false,
  },
  summary: {
    type: DataTypes.TEXT,
  },
  duration: {
    type: DataTypes.STRING(50),
  },
  format: {
    type: DataTypes.STRING(20),
  },
  theory: {
    type: DataTypes.JSONB,
  },
  task: {
    type: DataTypes.TEXT,
  },
  success_hint: {
    type: DataTypes.TEXT,
  },
  created_at: {
    type: DataTypes.DATE,
    defaultValue: DataTypes.NOW,
  },
}, {
  tableName: 'lessons',
  timestamps: false,
});

module.exports = Lesson;