const crypto = require("crypto");

const encryptDataWithPublicKey = (publicKey, message) => {
	const encryptedMessage = crypto.publicEncrypt(
		publicKey,
		Buffer.from(message)
	);
	return encryptedMessage.toString("base64");
};

module.exports = { encryptDataWithPublicKey };
