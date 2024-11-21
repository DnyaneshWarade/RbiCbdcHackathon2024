const { logger } = require("firebase-functions");
const snarkjs = require("snarkjs");
const os = require("os");
const path = require("path");
const { getFirebaseAdminStorage } = require("../firebaseInit");
const fs = require("fs").promises;
const { awCollection } = require("../constants/collectionConstants");
const { getFirebaseAdminDB, getFirebaseMessaging } = require("../firebaseInit");

const loadMoney = async (req, res) => {
	try {
		// Extract details from the request body
		const { token, requestId } = req.body;

		if (!token || !requestId) {
			return res.status(400).json({
				message: "Missing required fields: 'token', 'requestId'",
			});
		}

		// ToDo : Add wallet state/commitment in the central log

		// Send the success notification to the user
		// Define the message payload
		const message = {
			notification: {
				title: "Loaded Money Successfully",
				body: "Amount has been loaded successfully kindly check transaction in app for details",
			},
			data: {
				status: `{ "requestId": "${requestId}", "status": "success" }`,
			},
			token: token,
		};

		// Send the notification
		const response = await getFirebaseMessaging().send(message);
		logger.info("Notification sent successfully:", response);

		res.status(200).json({
			message: "Loaded money successfully",
		});
	} catch (error) {
		logger.error(error);
		res.status(500).json({
			message: "Failed to load money",
			error: error.message,
		});
	}
};

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

const senderToReceiverTx = async (req, res) => {
	logger.info("transactionController senderToReceiverTx execution started");
	try {
		// validate the body
		if (!req.body) {
			logger.error("Invalid request data");
			return res.status(400).send("Invalid data");
		}

		// Validate input
		const { senderPublicInputs, senderProof, txValue, publicKey } =
			req.body;
		if (!senderPublicInputs || !senderProof || !txValue || !publicKey) {
			logger.error("Missing required fields");
			return res.status(400).json({ error: "Missing required fields" });
		}

		const database = getFirebaseAdminDB();
		let querySnap = await database
			.collection(awCollection)
			.where("publicKey", "==", publicKey)
			.get();

		if (!querySnap.docs[0]) {
			return res.status(404).send("key not found");
		}
		const doc = querySnap.docs[0].data();
		const walletData = doc;
		let cloudMsgToken;
		// Implement push notification logic to sender
		if (walletData.isActive) {
			cloudMsgToken = walletData.cloudMsgToken;
		}
		if (!cloudMsgToken) {
			logger.error("No active cloudMsgToken found for publicKey");
			return res
				.status(404)
				.json({ error: "User not active for notifications" });
		}

		// Prepare notification payload
		const message = {
			token: cloudMsgToken,
			notification: {
				title: "Transaction Alert",
				body: `You have received ${txValue} coins!`,
			},
			data: {
				senderProof: JSON.stringify(senderProof),
				senderPublicInputs: JSON.stringify(senderPublicInputs),
				txValue: txValue.toString(),
			},
		};

		// Send notification
		const response = await admin.messaging().send(message);
		logger.info("Notification sent successfully:", response);
		logger.info("transactionController senderToReceiverTx execution end");

		// Return success or failed status
		res.status(200).json({
			message: "Notification sent successfully",
		});
	} catch (error) {
		logger.error(error);
		res.status(500).json({
			message: "Failed to send notification from sender to receiver",
			error: error.message,
		});
	}
};

module.exports = {
	loadMoney,
	verifyTransaction,
	senderToReceiverTx,
};
