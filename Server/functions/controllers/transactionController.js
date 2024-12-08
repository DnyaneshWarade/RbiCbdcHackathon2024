const { logger } = require("firebase-functions");
const snarkjs = require("snarkjs");
const os = require("os");
const path = require("path");
const { getFirebaseAdminStorage } = require("../firebaseInit");
const fs = require("fs").promises;
const {
	awCollection,
	txLogCollection,
} = require("../constants/collectionConstants");
const { getFirebaseAdminDB, getFirebaseMessaging } = require("../firebaseInit");
const { encryptDataWithPublicKey } = require("../helpers/crypto");
const { getAnonymousWallet } = require("../helpers/walletHelper");
const { Console } = require("console");
const { getVerificationKey } = require("../helpers/transactionHelper");

const loadMoney = async (req, res) => {
	try {
		// Extract details from the request body
		const { token, requestId, publicKey, proof, publicInputs, amount } =
			req.body;

		if (
			!token ||
			!requestId ||
			!publicKey ||
			!proof ||
			!publicInputs ||
			!amount
		) {
			return res.status(400).json({
				message: "Missing required fields",
			});
		}
		const verificationKey = await getVerificationKey("load_money");

		// Verify proof
		const isProofValid = await snarkjs.groth16.verify(
			verificationKey,
			publicInputs,
			proof
		);

		if (!isProofValid) {
			logger.error("Invalid proof");
			return res.status(400).json({ error: "Invalid proof" });
		}

		// Retrieve balance from database
		const database = getFirebaseAdminDB();
		const querySnap = await database
			.collection(awCollection)
			.where("publicKey", "==", publicKey)
			.get();

		if (querySnap.empty) {
			logger.error("User not found");
			return res.status(404).json({ error: "User not found" });
		}

		const doc = querySnap.docs[0];
		const data = doc.data();

		// Update balances
		const newBalance = data.balance + amount;

		await doc.ref.update({ balance: newBalance });

		// Log the transaction
		const transactionLog = {
			proof: JSON.stringify(proof),
			publicInputs: JSON.stringify(publicInputs),
			requestId: requestId,
			timestamp: new Date().toISOString(),
		};

		await database.collection(txLogCollection).add(transactionLog);

		// Send the success notification to the user
		// Define the message payload
		const message = {
			notification: {
				title: "Loaded Money Successfully",
				body: "Amount has been loaded successfully kindly check transaction in app for details",
			},
			data: {
				status: `{ "requestId": "${requestId}", "status": "success" }`,
				accountState: encryptDataWithPublicKey(
					publicKey,
					JSON.stringify({ balance: newBalance })
				),
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

const processSenderTransaction = async (req, res) => {
	try {
		// Extract details from the request body
		const {
			receiverPublicKey,
			requestId,
			senderZkp,
			newWalletState,
			amount,
		} = req.body;

		if (
			!receiverPublicKey ||
			!requestId ||
			!senderZkp ||
			!newWalletState ||
			!amount
		) {
			return res.status(400).json({
				message: "Missing required fields",
			});
		}

		var token;
		// try to get the token from public key
		var wallet = await getAnonymousWallet(receiverPublicKey);
		if (!wallet) {
			return res.status(404).send("public key not found");
		} else {
			token = wallet.cloudMsgToken;
		}

		// Define the message payload
		const message = {
			notification: {
				title: "Transaction Initiated",
				body: "Transaction has been initiated kindly check app for details",
			},
			data: {
				transaction: JSON.stringify(req.body),
			},
			token: token,
		};

		// Send the notification
		const response = await getFirebaseMessaging().send(message);
		logger.info("Notification sent successfully:", response);

		res.status(200).json({
			message: "Transaction initialized successfully",
		});
	} catch (error) {
		logger.error(error);
		res.status(500).json({
			message: "Failed to initiate transaction",
			error: error.message,
		});
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
		const response = await getFirebaseMessaging().send(message);
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

const processTransaction = async (req, res) => {
	logger.info("processTransaction controller execution started");
	try {
		// Step 1: Validate request body
		const {
			senderPublicInputs,
			senderProof,
			receiverPublicInputs,
			receiverProof,
			txValue,
			senderPublicKey,
			receiverPublicKey,
		} = req.body;

		if (
			!senderPublicInputs ||
			!senderProof ||
			!receiverPublicInputs ||
			!receiverProof ||
			!txValue ||
			!senderPublicKey ||
			!receiverPublicKey
		) {
			logger.error("Missing required fields");
			return res.status(400).json({ error: "Missing required fields" });
		}

		// Fetch the verification keys from Firebase Storage
		const senderVerificationKey = getVerificationKey("sender");
		const receiverVerificationKey = getVerificationKey("receiver");
		// Step 2: Verify sender's proof
		const isSenderProofValid = await snarkjs.groth16.verify(
			senderVerificationKey,
			senderPublicInputs,
			senderProof
		);

		if (!isSenderProofValid) {
			logger.error("Invalid sender proof");
			return res.status(400).json({ error: "Invalid sender proof" });
		}

		// Step 3: Verify receiver's proof
		const isReceiverProofValid = await snarkjs.groth16.verify(
			receiverVerificationKey,
			receiverPublicInputs,
			receiverProof
		);

		if (!isReceiverProofValid) {
			logger.error("Invalid receiver proof");
			return res.status(400).json({ error: "Invalid receiver proof" });
		}

		// Step 4: Retrieve sender and receiver balances from database
		const database = getFirebaseAdminDB();
		const senderQuerySnap = await database
			.collection(awCollection)
			.where("publicKey", "==", senderPublicKey)
			.get();

		const receiverQuerySnap = await database
			.collection("users")
			.where("publicKey", "==", receiverPublicKey)
			.get();

		if (senderQuerySnap.empty || receiverQuerySnap.empty) {
			logger.error("Sender or receiver public key not found");
			return res.status(404).json({ error: "User not found" });
		}

		const senderDoc = senderQuerySnap.docs[0];
		const receiverDoc = receiverQuerySnap.docs[0];
		const senderData = senderDoc.data();
		const receiverData = receiverDoc.data();

		if (senderData.balance < txValue) {
			logger.error("Sender has insufficient balance");
			return res.status(400).json({ error: "Insufficient balance" });
		}

		// Step 5: Update balances
		const senderNewBalance = senderData.balance - txValue;
		const receiverNewBalance = receiverData.balance + txValue;

		await senderDoc.ref.update({ balance: senderNewBalance });
		await receiverDoc.ref.update({ balance: receiverNewBalance });

		// Step 6: Log the transaction
		const transactionLog = {
			senderProof,
			senderPublicInputs,
			receiverProof,
			receiverPublicInputs,
			timestamp: new Date().toISOString(),
		};

		await database.collection(txLogCollection).add(transactionLog);

		// Step 7: Send notifications

		const receiverNotification = {
			token: receiverData.cloudMsgToken,
			notification: {
				title: "Transaction Received",
				body: `You have received ${txValue} coins`,
			},
			data: {
				senderAccountState: encryptDataWithPublicKey(
					senderPublicKey,
					JSON.stringify({ balance: senderNewBalance })
				),
				receiverAccountState: encryptDataWithPublicKey(
					receiverPublicKey,
					JSON.stringify({ balance: receiverNewBalance })
				),
				txValue: txValue.toString(),
			},
		};

		if (receiverData.cloudMsgToken) {
			const response = await getFirebaseMessaging.send(
				receiverNotification
			);
			logger.info("Notification sent to receiver", response);
		}

		logger.info("processTransaction controller execution completed");

		// Step 8: Send success response
		res.status(200).json({
			message: "Transaction completed successfully",
			transaction: transactionLog,
			data: {
				senderNewBalance: JSON.stringify(senderNewBalance),
				receiverNewBalance: JSON.stringify(receiverNewBalance),
				txValue: txValue.toString(),
			},
		});
	} catch (error) {
		logger.error("Error in processTransaction controller:", error.message);
		res.status(500).json({
			error: "Transaction processing failed",
			details: error.message,
		});
	}
};

const receiverToSenderTx = async (req, res) => {
	logger.info("transactionController receiverToSenderTx execution started");
	try {
		// validate the body
		if (!req.body) {
			logger.error("Invalid request data");
			return res.status(400).send("Invalid data");
		}

		// Validate input
		const { senderAccountState, publicKey, txValue } = req.body;
		if (!senderAccountState || !publicKey || !txValue) {
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
				body: `You have successfully sent ${txValue} coins!`,
			},
			data: {
				senderAccountState: senderAccountState,
				txValue: txValue.toString(),
			},
		};

		// Send notification
		const response = await getFirebaseMessaging.send(message);
		logger.info("Notification sent successfully:", response);
		logger.info("transactionController receiverToSenderTx execution end");

		// Return success or failed status
		res.status(200).json({
			message: "Notification sent successfully",
		});
	} catch (error) {
		logger.error(error);
		res.status(500).json({
			message: "Failed to send notification from receiver to sender",
			error: error.message,
		});
	}
};

const generateProof = async (req, res) => {
	logger.info("transactionController generateSenderProof execution started");
	try {
		// validate the body
		if (!req.body) {
			logger.error("Invalid request data");
			return res.status(400).send("Invalid data");
		}

		// Validate input
		const { name, input } = req.body;
		if (!input || !name) {
			logger.error("Missing required fields");
			return res.status(400).json({ error: "Missing required fields" });
		}

		const { proof, publicSignals } = await calculateProof(input, name);
		logger.info("transactionController generateSenderProof execution end");

		res.status(200).json({
			success: true,
			data: { proof, publicSignals },
		});
	} catch (error) {
		logger.error(error);
		res.status(500).json({
			message: "Failed to generate proof",
			success: false,
		});
	}
};

async function calculateProof(input, name) {
	if (!input || !name) {
		return;
	}
	const bucket = getFirebaseAdminStorage().bucket();
	const tempFileCircuitPath = path.join(os.tmpdir(), "circuit.wasm");
	const tempFileZkeyPath = path.join(os.tmpdir(), "circuit_0001.zkey");

	// Download from Firebase Storage
	await bucket
		.file(`proof/${name}_circuit.wasm`)
		.download({ destination: tempFileCircuitPath });
	await bucket
		.file(`proof/${name}_circuit_0001.zkey`)
		.download({ destination: tempFileZkeyPath });

	// Read and parse the verification key JSON
	const circuitData = await fs.readFile(tempFileCircuitPath);
	const zkeyData = await fs.readFile(tempFileZkeyPath);
	try {
		const res = await snarkjs.groth16.fullProve(
			input,
			circuitData,
			zkeyData
		);
		const { proof, publicSignals } = res;
		return { proof, publicSignals };
	} catch (error) {
		logger.error(error);
	}
}

module.exports = {
	loadMoney,
	processSenderTransaction,
	senderToReceiverTx,
	processTransaction,
	receiverToSenderTx,
	generateProof,
};
