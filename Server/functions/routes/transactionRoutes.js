const express = require("express");
const {
	loadMoney,
	senderToReceiverTx,
	processTransaction,
	receiverToSenderTx,
} = require("../controllers/transactionController");

const router = express.Router();

router.post("/loadMoney", loadMoney);
router.post("/processTx", processTransaction);
router.post("/senderToReceiver", senderToReceiverTx);
router.post("/receiverToSender", receiverToSenderTx);

module.exports = router;
