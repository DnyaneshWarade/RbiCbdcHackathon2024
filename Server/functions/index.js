const express = require("express");
const functions = require("firebase-functions");
const { initializeFirebaseApp } = require("./firebaseInit");
const transactionRoutes = require("./routes/transactionRoutes");
const userRoutes = require("./routes/userRoutes");
const helmet = require("helmet");

initializeFirebaseApp();

const app = express();

// Middlewares
app.use(express.json());
app.use(helmet());

// Routes
app.use("/transaction", transactionRoutes);
app.use("/user", userRoutes);

exports.api = functions.region("asia-southeast1").https.onRequest(app);
