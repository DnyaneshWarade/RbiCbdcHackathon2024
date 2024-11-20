const Joi = require("joi");

const User = Joi.object({
	id: Joi.string().allow(""),
	firstName: Joi.string().required(),
	lastName: Joi.string().required(),
	mobileNo: Joi.string().required(),
	pin: Joi.string().required(),
	deviceId: Joi.string().required(),
	isAnonymousWCreated: Joi.bool().required(),
	// aadharHash: Joi.string().required(),
});

module.exports = User;
