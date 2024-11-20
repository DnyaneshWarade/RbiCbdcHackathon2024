const express = require("express");
const {
	loadMoney,
	verifyTransaction,
	senderToReceiverTx,
} = require("../controllers/transactionController");

const router = express.Router();

router.post("/loadMoney", loadMoney);
router.post("/verify", verifyTransaction);
router.post("/senderToReceiver", senderToReceiverTx);

module.exports = router;
