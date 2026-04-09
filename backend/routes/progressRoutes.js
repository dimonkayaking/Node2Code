const express = require('express');
const router = express.Router();
const { getProgress, updateProgress } = require('../controllers/progressController');

// GET /api/progress/:userId
router.get('/:userId', getProgress);

// POST /api/progress
router.post('/', updateProgress);

module.exports = router;