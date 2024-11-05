const Joi = require("joi");

const User = Joi.object({
	id: Joi.string(),
	firstName: Joi.string().required(),
	lastName: Joi.string().required(),
	mobileNo: Joi.string().required(),
	pin: Joi.string().required(),
	deviceId: Joi.string().required(),
	cloudMsgToken: Joi.string().required(),
	publicKey: Joi.string().required(),
	isActive: Joi.bool().required(),
	// aadharHash: Joi.string().required(),
});

module.exports = User;
