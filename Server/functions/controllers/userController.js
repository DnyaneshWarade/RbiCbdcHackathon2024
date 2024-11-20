const { logger } = require("firebase-functions/v1");
const User = require("../models/user");
const { getFirebaseAdminAuth, getFirebaseAdminDB } = require("../firebaseInit");
const {
	usersCollection,
	awCollection,
} = require("../constants/collectionConstants");

// update user cloud message token
const updateUserCloudMsgToken = async (req, res) => {
	logger.info("userController updateUserCloudMsgToken execution started");
	try {
		// validate the body
		if (!(req.body.publicKey && req.body.cloudMsgToken)) {
			logger.error("Invalid request data");
			response
				.status(400)
				.send("Invalid data, please specify public key and token");
		}

		// check if entry already exists in db
		var database = getFirebaseAdminDB();
		let querySnap = await database
			.collection(awCollection)
			.where("publicKey", "==", req.body.publicKey)
			.get();

		let docRef;
		if (!querySnap.docs[0]) {
			// add new entry in db
			docRef = await database.collection(awCollection).add(req.body);
		} else {
			// update the token if it is different
			docRef = await database
				.collection(awCollection)
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

const getUserCloudMsgToken = async (req, res) => {
	logger.info("userController getUserCloudMsgToken execution started");
	try {
		// validate the body
		if (!req.query.publicKey) {
			logger.error("Invalid request data");
			response
				.status(400)
				.send("Invalid data, please specify public key");
		}

		// check if entry already exists in db
		var database = getFirebaseAdminDB();
		let querySnap = await database
			.collection(awCollection)
			.where("publicKey", "==", req.query.publicKey)
			.get();

		if (!querySnap.docs[0]) {
			return res.status(404).send("key not found");
		} else {
			//let docs = querySnap.docs.map((doc) => doc.data());
			return res.status(200).json(querySnap.docs[0].data());
		}
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
		var docRef;

		if (!req.body.id) {
			// add new entry in db
			docRef = await database.collection(usersCollection).add(req.body);
		} else {
			// update the token if it is different
			docRef = database.collection(usersCollection).doc(req.body.id);
			if (!docRef) {
				return res
					.status(404)
					.send(`user with doc id ${docRef.id} not found`);
			}
			await docRef.update(req.body);
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
	getUserCloudMsgToken,
};
