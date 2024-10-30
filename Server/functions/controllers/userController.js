const { logger } = require("firebase-functions/v1");
const User = require("../models/user");
const { getFirebaseAdminAuth, getFirebaseAdminDB } = require("../firebaseInit");

const usersCollection = "Users";

// update user cloud message token
const updateUserCloudMsgToken = async (req, res) => {
	logger.info("userController updateUserCloudMsgToken execution started");
	try {
		// validate the body
		if (
			!((req.body.imei || req.body.publicKey) && req.body.cloudMsgToken)
		) {
			logger.error("Invalid request data");
			response
				.status(400)
				.send(
					"Invalid data, please specify imei or public key and token"
				);
		}

		let variable = req.body.imei ? "imei" : "publicKey";
		let value = req.body.imei ?? req.body.publicKey;
		// check if entry already exists in db
		var database = getFirebaseAdminDB();
		let querySnap = await database
			.collection(usersCollection)
			.where(variable, "==", value)
			.get();

		let docRef;
		if (!querySnap.docs[0]) {
			// add new entry in db
			docRef = await database.collection(usersCollection).add({
				variable: value,
				cloudMsgToken: req.body.cloudMsgToken,
			});
		} else {
			// update the token if it is different
			docRef = await database
				.collection(usersCollection)
				.doc(querySnap.docs[0].id)
				.set({ cloudMsgToken: req.body.cloudMsgToken });
		}
		logger.info("userController updateUserCloudMsgToken execution end");
		return res
			.status(200)
			.send(`token has been updated successfully in doc id ${docRef.id}`);
	} catch (error) {
		logger.error(error);
	}
	res.status(400).send("Invalid request");
};

const updateUser = async (req, res) => {
	logger.info("userController updateUser execution started");
	try {
		// validate the body
		if (!req.body) {
			logger.error("Invalid request data");
			response.status(400).send("Invalid data");
		}

		const { error } = User.validate(req.body);
		if (error) {
			logger.error("User validation failed: ", error);
			return res.status(400).send(error.details);
		}

		// store in db
		var database = getFirebaseAdminDB();
		const docRef = database.collection(usersCollection).doc(req.body.id);
		const doc = await docRef.get();

		if (!doc) {
			// add new entry in db
			return res
				.status(404)
				.send(`document with id ${req.body.id} not found`);
		} else {
			// update the token if it is different
			await docRef.update({
				imei: req.body.imei,
				cloudMsgToken: req.body.cloudMsgToken,
				publicKey: req.body.publicKey,
			});
		}
		logger.info("userController updateUser execution end");
		return res
			.status(200)
			.send(`user has been updated successfully in doc id ${docRef.id}`);
	} catch (error) {
		// await deleteUserInFirebase(req.body.email);
		logger.error(error);
	}
	res.status(400).send("Invalid request");
};

module.exports = {
	updateUserCloudMsgToken,
	updateUser,
	// getAllUsers,
	// getUserAccountStatus,
	// enableDisableUserAccount,
	// deleteUserAccount,
	// forgotUserPassword,
};
