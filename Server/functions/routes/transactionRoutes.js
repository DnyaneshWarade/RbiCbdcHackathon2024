const express = require("express");
const {
	loadMoney,
	processSenderTransaction,
	zkpSenderToReceiverTx,
	processTransaction,
	receiverToSenderTx,
	generateProof,
	getzkwasm,
} = require("../controllers/transactionController");

const router = express.Router();

router.post("/loadMoney", loadMoney);
router.post("/processTx", processTransaction);
router.post("/senderToReceiverTx", processSenderTransaction);
router.post("/zkpSenderToReceiverTx", zkpSenderToReceiverTx);
router.post("/receiverToSender", receiverToSenderTx);
router.post("/generateProof", generateProof);
router.get("/getzkwasm", getzkwasm);

module.exports = router;
