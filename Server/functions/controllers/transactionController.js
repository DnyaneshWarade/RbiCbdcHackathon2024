const { logger } = require("firebase-functions");
const snarkjs = require("snarkjs");
const os = require("os");
const path = require("path");
const { getFirebaseAdminStorage } = require("../firebaseInit");
const fs = require("fs").promises;

const getVerificationKeys = async () => {
	try {
		const bucket = getFirebaseAdminStorage().bucket();
		const tempFilePathSender = path.join(
			os.tmpdir(),
			"sender_verification_key.json"
		); // Universal temp directory
		const tempFilePathReceiver = path.join(
			os.tmpdir(),
			"receiver_verification_key.json"
		);

		// Download the verification keys from Firebase Storage
		await bucket
			.file("verification/sender_verification_key.json")
			.download({ destination: tempFilePathSender });
		await bucket
			.file("verification/receiver_verification_key.json")
			.download({ destination: tempFilePathReceiver });

		// Read and parse the verification key JSON
		const verificationKeyDataSender = await fs.readFile(
			tempFilePathSender,
			"utf-8"
		);
		const verificationKeyDataReceiver = await fs.readFile(
			tempFilePathReceiver,
			"utf-8"
		);
		return {
			senderVerificationKey: JSON.parse(verificationKeyDataSender),
			receiverVerificationKey: JSON.parse(verificationKeyDataReceiver),
		};
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
		const {
			senderPublicInputs,
			senderProof,
			receiverPublicInputs,
			receiverProof,
		} = req.body;
		if (
			!senderPublicInputs ||
			!senderProof ||
			!receiverPublicInputs ||
			!receiverProof
		) {
			return res
				.status(400)
				.json({ error: "publicInputs and proof are required" });
		}

		// Fetch the verification keys from Firebase Storage
		const { senderVerificationKey, receiverVerificationKey } =
			await getVerificationKeys();

		// Verify the zk-SNARK proof using snarkjs
		const senderResult = await snarkjs.groth16.verify(
			senderVerificationKey,
			senderPublicInputs,
			senderProof
		);
		const receiverResult = await snarkjs.groth16.verify(
			receiverVerificationKey,
			receiverPublicInputs,
			receiverProof
		);
		logger.info("transactionController verifyTransaction execution end");

		if (senderResult && receiverResult) {
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
