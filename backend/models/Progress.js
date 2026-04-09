const { DataTypes } = require('sequelize');
const sequelize = require('../config/db');
const User = require('./User');
const Lesson = require('./Lesson');

const Progress = sequelize.define('Progress', {
  id: {
    type: DataTypes.INTEGER,
    autoIncrement: true,
    primaryKey: true,
  },
  user_id: {
    type: DataTypes.INTEGER,
    allowNull: false,
    references: {
      model: User,
      key: 'id',
    },
  },
  lesson_id: {
    type: DataTypes.INTEGER,
    allowNull: false,
    references: {
      model: Lesson,
      key: 'id',
    },
  },
  completed: {
    type: DataTypes.BOOLEAN,
    defaultValue: false,
  },
  completed_at: {
    type: DataTypes.DATE,
  },
  updated_at: {
    type: DataTypes.DATE,
    defaultValue: DataTypes.NOW,
  },
}, {
  tableName: 'progress',
  timestamps: false,
  indexes: [
    {
      unique: true,
      fields: ['user_id', 'lesson_id'],
    },
  ],
});

// Ассоциации
User.hasMany(Progress, { foreignKey: 'user_id' });
Progress.belongsTo(User, { foreignKey: 'user_id' });
Lesson.hasMany(Progress, { foreignKey: 'lesson_id' });
Progress.belongsTo(Lesson, { foreignKey: 'lesson_id' });

module.exports = Progress;