const { getFirebaseAdminDB } = require("../firebaseInit");
const { awCollection } = require("../constants/collectionConstants");
const { logger } = require("firebase-functions");

const getAnonymousWallet = async (publicKey) => {
	if (!publicKey) {
		return undefined;
	}

	try {
		// try to get the token from public key
		var database = getFirebaseAdminDB();
		let querySnap = await database
			.collection(awCollection)
			.where("publicKey", "==", publicKey)
			.get();
		if (!querySnap.docs[0]) {
			return undefined;
		} else {
			let docs = querySnap.docs.map((doc) => doc.data());
			return docs[0];
		}
	} catch (error) {
		logger.error(error);
	}
};

module.exports = {
	getAnonymousWallet,
};
