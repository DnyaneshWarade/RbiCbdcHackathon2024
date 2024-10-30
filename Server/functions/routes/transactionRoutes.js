const express = require("express");
const { verifyTransaction } = require("../controllers/transactionController");

const router = express.Router();

router.post("/verify", verifyTransaction);

module.exports = router;
