/*
*
* S1 Flight Controller I2C bootloader specific definitions
*
* created: 14.03.15
*
*/
#ifndef __APPLICATION_H__
#define __APPLICATION_H__


/* error handling */

#define AUTO_RESET



/* Non-volatile memory allocations (this must match the allocation used in the main application) */

#define NVM_VERSION				(0)
#define NVM_SANITY_OFFSET		(0)
#define NVM_SANITY_LENGTH		(2)
#define NVM_APPINFO_OFFSET		(NVM_SANITY_OFFSET + NVM_SANITY_LENGTH)
#define NVM_APPINFO_LENGTH		(6)
#define NVM_BUG_LOG_OFFSET		(NVM_APPINFO_OFFSET + NVM_APPINFO_LENGTH)
#define NVM_BUG_LOG_LENGTH		(8)
#define NVM_FIXUP_OFFSET		(NVM_BUG_LOG_OFFSET + NVM_BUG_LOG_LENGTH)



/* I2C slave endpoints */

#define I2C_ENDPOINTS {								\
	I2C_DFU_SERVICE									\
}



/* DFU mode settings */

// holding down the power button for >5s at startup forces the device
// (including the DFU master) into DFU mode.
#define DFU_HOLD_CONDITION	(pwr_switch_pressed())

#define SIGNAL1_PIN					MOTOR_FL_PIN
#define SIGNAL1_PIN_ACTIVE_HIGH		MOTOR_PINS_ACTIVE_HIGH
#define SIGNAL2_PIN					MOTOR_FR_PIN
#define SIGNAL2_PIN_ACTIVE_HIGH		MOTOR_PINS_ACTIVE_HIGH



#endif // __APPLICATION_H__