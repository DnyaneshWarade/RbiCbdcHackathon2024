const Joi = require("joi");

const User = Joi.object({
	id: Joi.string().required(),
	imei: Joi.string().required(),
	cloudMsgToken: Joi.string().required(),
	publicKey: Joi.string().required(),
	isActive: Joi.bool().required(),
	// aadharHash: Joi.string().required(),
});

module.exports = User;
