const { logger } = require("firebase-functions");
const snarkjs = require("snarkjs");
const os = require("os");
const path = require("path");
const { getFirebaseAdminStorage } = require("../firebaseInit");
const fs = require("fs").promises;

const getVerificationKey = async () => {
	try {
		const bucket = getFirebaseAdminStorage().bucket();
		const tempFilePath = path.join(os.tmpdir(), "verification_key.json"); // Universal temp directory

		// Download the verification key from Firebase Storage
		await bucket
			.file("verification/verification_key.json")
			.download({ destination: tempFilePath });

		// Read and parse the verification key JSON
		const verificationKeyData = await fs.readFile(tempFilePath, "utf-8");
		return JSON.parse(verificationKeyData);
	} catch (error) {
		logger.error("Error fetching verification key:", error);
		throw new Error("Failed to retrieve verification key");
	}
};

const verifyTransaction = async (req, res) => {
	logger.info("transactionController verifyTransaction execution started");
	try {
		// validate the body
		if (!req.body) {
			logger.error("Invalid request data");
			return res.status(400).send("Invalid data");
		}

		// Validate input
		const { publicInputs, proof } = req.body;
		if (!publicInputs || !proof) {
			return res
				.status(400)
				.json({ error: "publicInputs and proof are required" });
		}

		// Fetch the verification key from Firebase Storage
		const verificationKey = await getVerificationKey();

		// Verify the zk-SNARK proof using snarkjs
		const result = await snarkjs.groth16.verify(
			verificationKey,
			publicInputs,
			proof
		);
		logger.info("transactionController verifyTransaction execution end");

		if (result) {
			return res
				.status(200)
				.json({ message: "Proof is valid", valid: true });
		} else {
			return res.status(400).json({
				message: "Proof is not valid",
				valid: false,
			});
		}
	} catch (error) {
		logger.error("Verification failed:", error);
		return res.status(500).json({ error: "Internal server error" });
	}
};

module.exports = {
	verifyTransaction,
};
