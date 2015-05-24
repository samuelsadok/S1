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



/* software I2C master */

#define SOFT_I2C_PORTS	\
	CREATE_SOFT_I2C_PORT(eeprom_port, IOPORT_CREATE_PIN(PORTD, 1), IOPORT_CREATE_PIN(PORTD, 0))



/* on-board I2C EEPROM settings */

#define ONBOARD_EEPROM_ADDR_PINS	0 // A[2:0] of the onboard EEPROM
#define ONBOARD_EEPROM				CREATE_SOFT_I2C_DEVICE(eeprom_port, 0x50 | ONBOARD_EEPROM_ADDR_PINS, 2);
#define EEPROM_PAGESIZE				128
#define EEPROM_WRITE_TIME		5	// 5 ms



/* signal settings */

#define SIGNAL1_PIN					MOTOR_FL_PIN
#define SIGNAL1_PIN_ACTIVE_HIGH		MOTOR_PINS_ACTIVE_HIGH
#define SIGNAL2_PIN					MOTOR_FR_PIN
#define SIGNAL2_PIN_ACTIVE_HIGH		MOTOR_PINS_ACTIVE_HIGH



#endif // __APPLICATION_H__