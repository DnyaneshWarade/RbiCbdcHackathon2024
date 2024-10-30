const express = require("express");
const {
	updateUserCloudMsgToken,
	updateUser,
} = require("../controllers/userController");

const router = express.Router();

router.post("/updateUserCloudMsgToken", updateUserCloudMsgToken);
router.post("/updateUser", updateUser);

module.exports = router;
