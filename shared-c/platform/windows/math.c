/*
*
* Imitates the exact behaviour of the arch/avr/math.S implementation.
*
* created: 01.04.15
*
*/

#include <system.h>

#ifdef USING_MATH_EXTENSIONS


// 128 asin values for the asin function
uint16_t asin_lookup_table[] = {
	0x0000, 0x0051, 0x00A3, 0x00F4, 0x0146, 0x0198, 0x01E9, 0x023B, 0x028C, 0x02DE, 0x0330, 0x0381, 0x03D3, 0x0425, 0x0477, 0x04C9,
	0x051B, 0x056D, 0x05C0, 0x0612, 0x0664, 0x06B7, 0x070A, 0x075C, 0x07AF, 0x0802, 0x0855, 0x08A9, 0x08FC, 0x0950, 0x09A3, 0x09F7,
	0x0A4B, 0x0AA0, 0x0AF4, 0x0B49, 0x0B9E, 0x0BF3, 0x0C48, 0x0C9D, 0x0CF3, 0x0D49, 0x0D9F, 0x0DF5, 0x0E4C, 0x0EA3, 0x0EFA, 0x0F52,
	0x0FA9, 0x1001, 0x105A, 0x10B2, 0x110B, 0x1165, 0x11BE, 0x1218, 0x1273, 0x12CE, 0x1329, 0x1385, 0x13E1, 0x143D, 0x149A, 0x14F7,
	0x1555, 0x15B4, 0x1612, 0x1672, 0x16D2, 0x1732, 0x1793, 0x17F5, 0x1857, 0x18BA, 0x191D, 0x1982, 0x19E7, 0x1A4C, 0x1AB3, 0x1B1A,
	0x1B82, 0x1BEA, 0x1C54, 0x1CBF, 0x1D2A, 0x1D97, 0x1E04, 0x1E73, 0x1EE2, 0x1F53, 0x1FC5, 0x2038, 0x20AD, 0x2123, 0x219A, 0x2213,
	0x228D, 0x2309, 0x2387, 0x2407, 0x2488, 0x250C, 0x2592, 0x261A, 0x26A4, 0x2731, 0x27C1, 0x2854, 0x28EA, 0x2984, 0x2A21, 0x2AC3,
	0x2B69, 0x2C13, 0x2CC4, 0x2D7A, 0x2E37, 0x2EFC, 0x2FC9, 0x30A1, 0x3184, 0x3276, 0x3379, 0x3493, 0x35C9, 0x3729, 0x38C9, 0x3AE7
};

// 128 asin delta values fot the asin function. These values represent the step size to the next table value, divided by 16. They are used to archive higher than 128-step precision.
uint8_t asin_lookup_table_delta[] = {
	0x05, 0x05, 0x05, 0x05, 0x05, 0x05, 0x05, 0x05, 0x05, 0x05, 0x05, 0x05, 0x05, 0x05, 0x05, 0x05,
	0x05, 0x05, 0x05, 0x05, 0x05, 0x05, 0x05, 0x05, 0x05, 0x05, 0x05, 0x05, 0x05, 0x05, 0x05, 0x05,
	0x05, 0x05, 0x05, 0x05, 0x05, 0x05, 0x05, 0x05, 0x05, 0x05, 0x05, 0x05, 0x05, 0x05, 0x05, 0x05,
	0x06, 0x06, 0x06, 0x06, 0x06, 0x06, 0x06, 0x06, 0x06, 0x06, 0x06, 0x06, 0x06, 0x06, 0x06, 0x06,
	0x06, 0x06, 0x06, 0x06, 0x06, 0x06, 0x06, 0x06, 0x06, 0x06, 0x06, 0x06, 0x06, 0x06, 0x06, 0x06,
	0x07, 0x07, 0x07, 0x07, 0x07, 0x07, 0x07, 0x07, 0x07, 0x07, 0x07, 0x07, 0x07, 0x07, 0x08, 0x08,
	0x08, 0x08, 0x08, 0x08, 0x08, 0x08, 0x09, 0x09, 0x09, 0x09, 0x09, 0x09, 0x0A, 0x0A, 0x0A, 0x0A,
	0x0B, 0x0B, 0x0B, 0x0C, 0x0C, 0x0D, 0x0D, 0x0E, 0x0F, 0x10, 0x12, 0x13, 0x16, 0x1A, 0x22, 0x52
};


// input: 0xF800: -1, 0x0000: 0, 0x0800: 1
// output: 0x8000: -180�, 0x0000: 0�, 0x7FFF: 180� (the actual range is only -90�...+90�)
int16_t asin_lookup(int16_t val) {
	bool wasNegative = (val < 0);
	if (wasNegative)
		val = ~val;

	int16_t output;

	if (val < 0x0800) {
		size_t i = (val >> 4) & 0xFF;
		output = asin_lookup_table[i] + (val & 0xF) * asin_lookup_table_delta[i];
	} else {
		output = 0x3FFF;
	}

	return (wasNegative ? ~output : output);
}


// input: quaternion fields, where 0xC000: -1, 0x0000: 0, 0x4000: 1
void math_quaternion_to_ypr(int16_t qW, int16_t qX, int16_t qY, int16_t qZ, math_ypr_t *output) {
	*output = (math_ypr_t) {
		.Roll = asin_lookup(((int32_t)qY * (int32_t)qZ + (int32_t)qX * (int32_t)qW) >> 16),
		.Pitch = asin_lookup(((int32_t)qY * (int32_t)qW - (int32_t)qZ * (int32_t)qX) >> 16),
		.Yaw = 0 // todo: calculate yaw (see AVR implementation comments for hints)
	};
}


int16_t math_kalman_filter(math_kalman_t *context, int16_t currentValue) {
	int32_t delta = constrain32((int32_t)currentValue - (int32_t)context->lastPrediction, ~0x7FFF, 0x7FFF);
	delta *= context->reactiveness + 1;
	delta += context->lastSlopeB0 + (context->lastSlopeB1 << 8) + (context->lastSlopeB2 << 16);
	delta = constrain32(delta, ~0x7FFFFF, 0x7FFFFF);
	context->lastSlopeB0 = delta & 0xFF;
	context->lastSlopeB1 = (delta >> 8) & 0xFF;
	context->lastSlopeB2 = (delta >> 16) & 0xFF;
	return context->lastPrediction = constrain32(((int32_t)currentValue + (((int32_t)context->predictionTime * (delta >> 8)) >> 16)), ~0x7FFF, 0x7FFF);
}


#endif
