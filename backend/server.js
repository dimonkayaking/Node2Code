require('dotenv').config();
const express = require('express');
const sequelize = require('./config/db');
const authRoutes = require('./routes/authRoutes');
const progressRoutes = require('./routes/progressRoutes');

const app = express();
const PORT = process.env.PORT || 5000;

app.use((req, res, next) => {
  res.header('Access-Control-Allow-Origin', process.env.CORS_ORIGIN || '*');
  res.header('Access-Control-Allow-Headers', 'Origin, X-Requested-With, Content-Type, Accept');
  res.header('Access-Control-Allow-Methods', 'GET, POST, PUT, PATCH, DELETE, OPTIONS');

  if (req.method === 'OPTIONS') {
    return res.sendStatus(204);
  }

  next();
});

app.use(express.json());

app.get('/ping', (req, res) => {
  res.json({ message: 'pong', timestamp: new Date() });
});

app.use('/api/auth', authRoutes);
app.use('/api/progress', progressRoutes);

app.use((err, req, res, next) => {
  console.error(err);
  res.status(500).json({ error: 'Internal server error' });
});

const start = async () => {
  try {
    await sequelize.authenticate();
    app.listen(PORT, () => {
      console.log(`Server started on http://localhost:${PORT}`);
    });
  } catch (error) {
    console.error('Failed to start server:', error);
    process.exit(1);
  }
};

start();
