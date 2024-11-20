const express = require("express");
const {
	loadMoney,
	verifyTransaction,
} = require("../controllers/transactionController");

const router = express.Router();

router.post("/loadMoney", loadMoney);
router.post("/verify", verifyTransaction);

module.exports = router;
