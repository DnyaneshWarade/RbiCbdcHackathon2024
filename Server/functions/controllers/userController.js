const { logger } = require("firebase-functions/v1");
const User = require("../models/user");
const { getFirebaseAdminAuth } = require("../firebaseInit");

// create user
const createUser = async (req, res) => {
	logger.info("userController createUser execution started");
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
		var result = await createNewUser(req.body);
		if (result.Id) {
			logger.info("User successfuly created with id: ", result.Id);
			return res.status(201).json(result.Id);
		} else {
			logger.error("Failed to create user with response: ", result);
			if (!req.body.Id) {
				await deleteUserInFirebase(req.body.email);
				logger.info("Revert Created user in firebase");
			}
			return res.status(result.code).json(result.res);
		}
	} catch (error) {
		logger.error(error);
	}
	res.status(400).send("Invalid request");
};

module.exports = {
	createUser,
	// getUsers,
	// getAllUsers,
	// getUserAccountStatus,
	// enableDisableUserAccount,
	// deleteUserAccount,
	// forgotUserPassword,
};
