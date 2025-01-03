const { logger } = require("firebase-functions");
const { getFirebaseAdminStorage } = require("../firebaseInit");
const os = require("os");
const path = require("path");
const fs = require("fs").promises;

const getVerificationKey = async (name) => {
	try {
		const bucket = getFirebaseAdminStorage().bucket();
		const tempFilePath = path.join(
			os.tmpdir(),
			`${name}_verification_key.json`
		);

		// Download the verification key from Firebase Storage
		await bucket
			.file(`verification/${name}_verification_key.json`)
			.download({ destination: tempFilePath });

		// Read and parse the verification key JSON
		const verificationKeyData = await fs.readFile(tempFilePath, "utf-8");

		return JSON.parse(verificationKeyData);
	} catch (error) {
		logger.error("Error fetching verification key:", error);
		throw new Error("Failed to retrieve verification key");
	}
};

module.exports = {
	getVerificationKey,
};
