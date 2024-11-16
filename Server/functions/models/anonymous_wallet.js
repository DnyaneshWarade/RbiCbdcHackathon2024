const Joi = require("joi");

const Anonymous_Wallet = Joi.object({
	id: Joi.string(),
	cloudMsgToken: Joi.string().required(),
	publicKey: Joi.string().required(),
	isActive: Joi.bool().required(),
});

module.exports = Anonymous_Wallet;
