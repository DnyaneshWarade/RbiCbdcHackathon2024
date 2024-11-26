const express = require("express");
const {
	loadMoney,
	processSenderTransaction,
	senderToReceiverTx,
	processTransaction,
	receiverToSenderTx,
} = require("../controllers/transactionController");

const router = express.Router();

router.post("/loadMoney", loadMoney);
router.post("/processTx", processTransaction);
router.post("/senderToReceiverTx", processSenderTransaction);
router.post("/receiverToSender", receiverToSenderTx);

module.exports = router;
