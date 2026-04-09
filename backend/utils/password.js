const crypto = require('crypto');

const SCRYPT_KEYLEN = 64;

const scryptAsync = (value, salt) =>
  new Promise((resolve, reject) => {
    crypto.scrypt(value, salt, SCRYPT_KEYLEN, (error, derivedKey) => {
      if (error) {
        reject(error);
        return;
      }

      resolve(derivedKey);
    });
  });

const hashPassword = async (password) => {
  const salt = crypto.randomBytes(16).toString('hex');
  const derivedKey = await scryptAsync(password, salt);
  return `${salt}:${derivedKey.toString('hex')}`;
};

const verifyPassword = async (password, storedHash) => {
  const [salt, hash] = String(storedHash || '').split(':');

  if (!salt || !hash) {
    return false;
  }

  const derivedKey = await scryptAsync(password, salt);
  const hashBuffer = Buffer.from(hash, 'hex');

  if (hashBuffer.length !== derivedKey.length) {
    return false;
  }

  return crypto.timingSafeEqual(hashBuffer, derivedKey);
};

module.exports = {
  hashPassword,
  verifyPassword,
};
