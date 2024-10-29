const express = require("express");
const functions = require("firebase-functions");
const { initializeFirebaseApp } = require("./firebaseInit");
const userRoutes = require("./routes/userRoutes");
const helmet = require("helmet");
const app = express();

initializeFirebaseApp();

// Middlewares
app.use(express.json());
app.use(helmet());

// Routes
app.use("/user", userRoutes);

exports.api = functions.region("asia-southeast1").https.onRequest(app);
