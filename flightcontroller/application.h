/*
*
* S1 Flight Controller application specific definitions
*
* created: 14.03.15
*
*/
#ifndef __APPLICATION_H__
#define __APPLICATION_H__


/* error handling */

#define AUTO_RESET



/* Non-volatile memory allocations  (this must match the allocation used in the bootloader) */

#define NVM_VERSION				(0)
#define NVM_SANITY_OFFSET		(0)
#define NVM_SANITY_LENGTH		(2)
#define NVM_APPINFO_OFFSET		(NVM_SANITY_OFFSET + NVM_SANITY_LENGTH)
#define NVM_APPINFO_LENGTH		(6)
#define NVM_BUG_LOG_OFFSET		(NVM_APPINFO_OFFSET + NVM_APPINFO_LENGTH)
#ifdef USING_SIMULATION
#  define NVM_BUG_LOG_LENGTH		(16)
#else
#  define NVM_BUG_LOG_LENGTH		(8)
#endif
#define NVM_FIXUP_OFFSET		(NVM_BUG_LOG_OFFSET + NVM_BUG_LOG_LENGTH)



/* I2C slave endpoints */

#define I2C_ENDPOINTS {								\
	I2C_DFU_SERVICE/*,*/								\
	/*I2C_UAV_SERVICE,*/								\
	/*I2C_MOTION_SERVICE*/								\
}



/* MPU6050 driver settings */

#define MPU_MAX_CALLBACKS	(5)
#define SWAP_PITCH_ROLL
#define INVERT_PITCH



/* default UAV configuration */

// #define PID_PR		CREATE_PID(4, 4, 3, 2) oscillation within range (D too low)

//#define PID_PR		CREATE_PID(2, 3, 7, 2)
//#define PID_PR		CREATE_PID(2, 1, 10, 2)
#define PID_PR		CREATE_PID(5, 5, 0, 2)

#define KALMAN_S	CREATE_KALMAN(30, 50)	// sensor
#define KALMAN_M	CREATE_KALMAN(16, 50)	// motors



/* quadrocopter definition */

#define UAV_ROTORS {															\
	{ CREATE_MOTOR(MOTOR_FL_PIN), { 1, -1 }, 1, KALMAN_M }, /* front left */	\
	{ CREATE_MOTOR(MOTOR_FR_PIN), { 1, 1 }, -1, KALMAN_M }, /* front right */	\
	{ CREATE_MOTOR(MOTOR_BL_PIN), { -1, -1 }, -1, KALMAN_M }, /* rear left */	\
	{ CREATE_MOTOR(MOTOR_BR_PIN), { -1, 1 }, 1, KALMAN_M } /* rear right */		\
}




/* simulation specific definitions */

#ifdef USING_SIMULATION


#define LOG_VERBOSITY (3)

#define CREATE_MOTOR(_gpio)	CREATE_VIRTUAL_MOTOR((_gpio))

#define MOTOR_FL_PIN	"mFL"
#define MOTOR_FR_PIN	"mFR"
#define MOTOR_BL_PIN	"mBL"
#define MOTOR_BR_PIN	"mBR"


#define CREATE_ONBOARD_MPU()		virtual_mpu_t onBoardMpu = CREATE_VIRTUAL_MPU(CREATE_SIM_OBJ("mpu"), 1500)
#define ONBOARD_MPU					APPLY_VIRTUAL_MPU(&onBoardMpu)
#define SWAP_PITCH_ROLL
#define INVERT_PITCH


#define pwr_init()
#define pwr_switch_pressed()	(0)

#define wdt_reset()


#endif


#endif // __APPLICATION_H__