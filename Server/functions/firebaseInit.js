const admin = require("firebase-admin");
const serviceAccount = require("./rbicbdchackathon2024-firebase-adminsdk-fir03-74a49bebb5.json");

let firebaseApp;
const initializeFirebaseApp = () => {
	firebaseApp = admin.initializeApp({
		credential: admin.credential.cert(serviceAccount),
		storageBucket: process.env.STORAGE_BUCKET,
	});
};

const getFirebaseAdminAuth = () => {
	if (firebaseApp) {
		return firebaseApp.auth();
	} else {
		undefined;
	}
};

const getFirebaseAdminStorage = () => {
	if (firebaseApp) {
		return firebaseApp.storage();
	} else {
		undefined;
	}
};

const getFirebaseAdminDB = () => {
	if (firebaseApp) {
		return firebaseApp.firestore();
	} else {
		undefined;
	}
};

const getFirebaseMessaging = () => {
	if (firebaseApp) {
		return firebaseApp.messaging();
	} else {
		undefined;
	}
};

module.exports = {
	getFirebaseAdminAuth,
	initializeFirebaseApp,
	getFirebaseAdminStorage,
	getFirebaseAdminDB,
	getFirebaseMessaging,
};
