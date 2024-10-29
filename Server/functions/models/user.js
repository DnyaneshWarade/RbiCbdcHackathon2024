const Joi = require("joi");

const User = Joi.object({
	Id: Joi.string(),
	imei: Joi.string().required(),
	cloudMsgToken: Joi.string().required(),
	publicKey: Joi.string().required(),
	isActive: Joi.bool().required(),
	aadharHash: Joi.string().required(),
});

module.exports = User;
