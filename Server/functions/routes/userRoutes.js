const express = require("express");
const {
	updateUserCloudMsgToken,
	getUserCloudMsgToken,
	updateUser,
} = require("../controllers/userController");

const router = express.Router();

router.post("/updateUserCloudMsgToken", updateUserCloudMsgToken);
router.post("/getUserCloudMsgToken", getUserCloudMsgToken);
router.post("/updateUser", updateUser);

module.exports = router;
