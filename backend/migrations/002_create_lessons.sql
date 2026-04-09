CREATE TABLE IF NOT EXISTS lessons (
    id INTEGER PRIMARY KEY,
    module_id INTEGER NOT NULL,
    order_num INTEGER NOT NULL,
    title VARCHAR(255) NOT NULL,
    summary TEXT,
    duration VARCHAR(50),
    format VARCHAR(20),
    theory JSONB,
    task TEXT,
    success_hint TEXT,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);