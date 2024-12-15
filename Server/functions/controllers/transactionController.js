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
		const { token, requestId, zkp, accountState, blind } = req.body;

		if (!token || !requestId || !zkp || !accountState || !blind) {
			return res.status(400).json({
				message: "Missing required fields",
			});
		}
		const zkpJson = JSON.parse(zkp);
		const verificationKey = await getVerificationKey("load_money");

		// Verify proof
		const isProofValid = await snarkjs.groth16.verify(
			verificationKey,
			zkpJson.publicSignals,
			zkpJson.proof
		);

		if (!isProofValid) {
			logger.error("Invalid proof");
			return res.status(400).json({ error: "Invalid proof" });
		}

		// Log the transaction
		const transactionLog = {
			zk_proof: JSON.stringify(zkpJson.proof),
			requestId: requestId,
			accountState: accountState,
			timestamp: new Date().toISOString(),
		};
		var database = getFirebaseAdminDB();
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
				// accountState: encryptDataWithPublicKey(
				// 	publicKey,
				// 	JSON.stringify({ balance: newBalance })
				// ),
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
		const { ReceiverPublicKey, SenderCloudToken, Trx, SenderZkp } =
			req.body;

		if (!ReceiverPublicKey || !SenderCloudToken || (!Trx && !SenderZkp)) {
			return res.status(400).json({
				message: "Missing required fields",
			});
		}

		// try to get the token from public key
		var wallet = await getAnonymousWallet(ReceiverPublicKey);
		if (!wallet) {
			return res.status(404).send("public key not found");
		} else {
			req.body.ReceiverCloudToken = wallet.cloudMsgToken;
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
			token: req.body.ReceiverCloudToken,
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

const zkpSenderToReceiverTx = async (req, res) => {
	logger.info(
		"transactionController zkpSenderToReceiverTx execution started"
	);
	try {
		// validate the body
		if (!req.body) {
			logger.error("Invalid request data");
			return res.status(400).send("Invalid data");
		}

		// Validate input
		const {
			requestId,
			receiverPublicKey,
			senderCloudToken,
			zkp,
			senderAccountState,
			senderBlind,
			trxEncryptedSate,
			trxBlind,
		} = req.body;
		if (
			!requestId ||
			!receiverPublicKey ||
			!senderCloudToken ||
			!zkp ||
			!senderAccountState ||
			!senderBlind ||
			!trxEncryptedSate ||
			!trxBlind
		) {
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
		logger.info(
			"transactionController zkpSenderToReceiverTx execution end"
		);

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
			ReceiverCloudToken,
			ReceiverZkp,
			ReceiverAccountState,
			SenderCloudToken,
			SenderZkp,
			SenderAccountState,
		} = req.body;

		if (
			!ReceiverCloudToken ||
			!ReceiverZkp ||
			!ReceiverAccountState ||
			!SenderCloudToken ||
			!SenderZkp ||
			!SenderAccountState
		) {
			logger.error("Missing required fields");
			return res.status(400).json({ error: "Missing required fields" });
		}

		// Fetch the verification keys from Firebase Storage
		const senderVerificationKey = await getVerificationKey("sender");
		const receiverVerificationKey = await getVerificationKey("receiver");

		// Step 2: Verify sender's proof
		const senderZkpJson = JSON.parse(SenderZkp);
		const isSenderProofValid = await snarkjs.groth16.verify(
			senderVerificationKey,
			senderZkpJson.publicSignals,
			senderZkpJson.proof
		);

		if (!isSenderProofValid) {
			logger.error("Invalid sender proof");
			return res.status(400).json({ error: "Invalid sender proof" });
		}

		// Step 3: Verify receiver's proof
		const receiverZkpJson = JSON.parse(ReceiverZkp);
		const isReceiverProofValid = await snarkjs.groth16.verify(
			receiverVerificationKey,
			receiverZkpJson.publicSignals,
			receiverZkpJson.proof
		);

		if (!isReceiverProofValid) {
			logger.error("Invalid receiver proof");
			return res.status(400).json({ error: "Invalid receiver proof" });
		}
		console.log("Reached successfully up to here");

		// Step 4: Log the transaction
		const transactionLog = {
			sender_zk_proof: JSON.stringify(senderZkpJson.proof),
			receiver_zk_proof: JSON.stringify(receiverZkpJson.proof),
			senderAccountState: SenderAccountState,
			receiverAccountState: ReceiverAccountState,
			timestamp: new Date().toISOString(),
		};

		var database = getFirebaseAdminDB();
		await database.collection(txLogCollection).add(transactionLog);

		// Step 5: Send notifications
		const receiverNotification = {
			token: ReceiverCloudToken,
			notification: {
				title: "Money received Successfully",
				body: `You have received money, for details please check the app`,
			},
		};

		const response = await getFirebaseMessaging().send(
			receiverNotification
		);

		logger.info("Notification sent to receiver", response);
		logger.info("processTransaction controller execution completed");
		res.status(200).json({
			message: "Transaction completed successfully",
			transaction: transactionLog,
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
		const { TrxId, SenderCloudToken } = req.body;
		if (!TrxId || !SenderCloudToken) {
			logger.error("Missing required fields");
			return res.status(400).json({ error: "Missing required fields" });
		}

		// Prepare notification payload
		const message = {
			token: SenderCloudToken,
			notification: {
				title: "Money sent Successfully",
				body: `Your transaction is completed, for details please check the app`,
			},
			data: {
				status: TrxId,
			},
		};

		// Send notification
		const response = await getFirebaseMessaging().send(message);
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

		res.status(200).json({ proof, publicSignals });
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

const getzkwasm = async (req, res) => {
	logger.info("transactionController getzkwasm execution started");
	try {
		// validate the body
		if (!req.query.name) {
			logger.error("Invalid request data");
			return res.status(400).send("Invalid data");
		}

		const bucket = getFirebaseAdminStorage().bucket();
		const tempFileCircuitPath = path.join(os.tmpdir(), "circuit.wasm");
		const tempFileZkeyPath = path.join(os.tmpdir(), "circuit_0001.zkey");

		// Download from Firebase Storage
		await bucket
			.file(`proof/${req.query.name}_circuit.wasm`)
			.download({ destination: tempFileCircuitPath });
		await bucket
			.file(`proof/${req.query.name}_circuit_0001.zkey`)
			.download({ destination: tempFileZkeyPath });

		// Read and parse the verification key JSON
		const circuitData = await fs.readFile(tempFileCircuitPath);
		const zkeyData = await fs.readFile(tempFileZkeyPath);

		logger.info("transactionController getzkwasm execution end");
		res.set("access-control-allow-origin", "*");
		res.set("cross-origin-opener-policy", "*");
		res.set("cross-origin-resource-policy", "*");
		res.set("x-frame-options", "*");
		res.status(200).json({
			success: true,
			data: { circuitData, zkeyData },
		});
	} catch (error) {
		logger.error(error);
		res.status(500).json({
			message: "Failed to get the circuit files",
			success: false,
		});
	}
};

module.exports = {
	loadMoney,
	processSenderTransaction,
	zkpSenderToReceiverTx,
	processTransaction,
	receiverToSenderTx,
	generateProof,
	getzkwasm,
};
