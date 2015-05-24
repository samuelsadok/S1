/*
 * GravityNegotiation.c
 *
 * Created: 07.03.2014 15:20:18
 *  Author: Samuel
 */ 

/*#include <system/system.h>
#include <baseband.h>
#include <system/math.h>*/
#include <system.h>
#include <uav/uav.h>


#define PITCH_LOG_SIZE	256


#define SAFETY_LIMIT 0x2000

/*



// Initializes power management
void pwr_init(void) {
	ioport_set_pin_level(GPIO_WAKE, 0);
	ioport_set_pin_mode(GPIO_WAKE, IOPORT_MODE_TOTEM);
	ioport_set_pin_dir(GPIO_WAKE, IOPORT_DIR_OUTPUT);
	
	//ioport_set_pin_level(GPIO_PWR_CTRL, 1); // as soon as the user releases the power button the gate capacitor starts discharging. We can keep it on by setting PWR_CTRL to high.
	//ioport_set_pin_dir(GPIO_PWR_CTRL, IOPORT_DIR_OUTPUT);
	//ioport_set_pin_mode(GPIO_PWR_CTRL, IOPORT_MODE_TOTEM);
	ioport_set_pin_mode(GPIO_PWR_SW, IOPORT_MODE_PULLDOWN);
	ioport_set_pin_dir(GPIO_PWR_SW, IOPORT_DIR_INPUT);
}

void pwr_wake_baseband(void) {
	ioport_set_pin_level(GPIO_WAKE, 1);
	_delay_ms(1);
	ioport_set_pin_level(GPIO_WAKE, 0);
}

// Shuts down the entire system down by opening the main power supply MOSFET.
// This routine does not return.
void pwr_shutdown(void) {
	ioport_set_pin_level(GPIO_PWR_CTRL, 1);
	while (1);
}


uint8_t pwr_needs_shutdown(void) {
	static uint8_t wasLow = 0;
	uint8_t power_switch = ioport_get_pin_level(GPIO_PWR_SW);
	if (!power_switch) wasLow = 1;
	return (wasLow && power_switch);
}

uint8_t pwr_switch_pressed(void) {
	static uint8_t state = 1;
	uint8_t lastState = state;
	state = ioport_get_pin_level(GPIO_PWR_SW);
	if (!lastState && state) {
		_delay_ms(50);
		return 1;
	}
	return 0;
}
*/


/*

typedef struct {
	uint16_t Version;
	struct {
		int16_t Throttle;
		math_ypr_t Attitude;
	} ControlInput;
	struct {
		math_kalman_t KalmanY;
		math_kalman_t KalmanP;
		math_kalman_t KalmanR;
		math_pid_t PidY;
		math_pid_t PidP;
		math_pid_t PidR;
	} Configuration;
	struct {
		math_ypr_t Attitude;
		math_ypr_t AngularRate;
	} PhysicalState;
	struct {
		uint16_t PitchLogCursor;
		int16_t PitchSensorLog[PITCH_LOG_SIZE];
		int16_t PitchActionLog[PITCH_LOG_SIZE];
	} DebugLog;
} register_file_t;


// the camera-estimated delay between input and output is around 150ms

register_file_t state = {
	.Version = 0x0001,
	.ControlInput = {
		.Throttle = 0,
		.Attitude = { .Yaw = 0, .Pitch = 0, .Roll = 0, .Flipped = 0 }
	},
	.Configuration = {
		.KalmanY = KALMAN_S,
		.KalmanP = KALMAN_S,
		.KalmanR = KALMAN_S,
		.PidY = CREATE_PID(0, 0, 0, 0),
		.PidP = PID_PR,
		.PidR = PID_PR
	},
	.DebugLog = {
		.PitchLogCursor = 0
	}
};


char *i2cRegisterFile = (char *)&state;
*/


int main(void)
{
	CREATE_ONBOARD_MPU();
	mpu_t mpu = ONBOARD_MPU;
	uav_t *uav = &localUAV;

	//_delay_ms(2000);
	io_init(); // initialize processor and built-in peripherals
	pwr_init(); // must be very early


	local_uav_set_mpu(&mpu);

	
	//_delay_ms(2000);
	//pwr_wake_baseband();
	//_delay_ms(2000);
	

	status_t status = STATUS_SUCCESS;
	LOGI("posA");
	if (!status) status = mpu_reset(&mpu);
	LOGI("posB");
	if (status) LOGE("err1");
	if (!status) status = mpu_start(&mpu);
	LOGI("posC");
	if (status) LOGE("err2");
	if (!status) status = uav_init(uav);
	LOGI("posD");
	if (status) LOGE("err3");
	if (!status) status = uav_on(uav);
	LOGI("posE");
	if (status) LOGE("err4");

	if (status != STATUS_SUCCESS) {
		LOGE("setup failed! %s, %d\n", "[todo: convert error code]", status);
		bug_check(status, 0);
	}
	
	LOGI("pos1");

	
	//tc45_write_cc(&TCC4, TC45_CCA, 320);
	//tc45_write_cc(&TCC4, TC45_CCB, 320);
	//tc45_write_cc(&TCC4, TC45_CCC, 320);
	//tc45_write_cc(&TCC4, TC45_CCD, 320);
	//tc45_enable_cc_channels(&TCC4, TC45_CCACOMP);
	//tc45_enable_cc_channels(&TCC4, TC45_CCBCOMP);
	//tc45_enable_cc_channels(&TCC4, TC45_CCCCOMP);
	//tc45_enable_cc_channels(&TCC4, TC45_CCDCOMP);
	//PORTC.REMAP |= 0x0F; // set PWM output pins
	
	//tc45_set_overflow_interrupt_level(&TCC5, TC45_INT_LVL_LO);

	uav_config_t config = {
		.reactivenessR = 30, .predictionTimeR = 50,
		.reactivenessP = 30, .predictionTimeP = 50,
		.PValP = 5, .IValP = 5, .DValP = 0, .maxIP = 2,
		.PValR = 5, .IValR = 5, .DValR = 0, .maxIR = 2
	};
	uav_config(uav, &config);
	
	float throttle = 0;
	uint8_t jumping = 1;
	for (;;) {
	    if (pwr_switch_pressed())
			jumping = !jumping;
		
		if (jumping) {
			//multirotor_set_motors(&device, 0x7FFF, 0, 0, 0);
			//_delay_ms(170);
			//multirotor_set_motors(&device, 0, 0x7FFF, 0, 0);
			////_delay_ms(30); // empty
			//_delay_ms(30);
			//
			//multirotor_set_motors(&device, 500, 0, 0, 0);
			//_delay_ms(2000);
			//multirotor_set_motors(&device, 0, 0, 0, 0);
			

			_delay_ms(1000);
			uav_control(uav, throttle = 0, NULL);
			for (size_t i = 0; i < 200; i++) {
				uav_control(uav, throttle += (1.0f / 200.0f), NULL);
				_delay_ms(5);
			}
			uav_control(uav, throttle = 0, NULL);
			//jumping = 0;
		}
	}
}



/*void __kernel_panic(uint16_t line, uint16_t error_code, uint16_t param1, uint16_t param2) {
	multirotor_init(&device);
	multirotor_on(&device);
	
	wdt_reset_mcu();
	while (1) {
		multirotor_set_motors(&device, 0x3FFF, 0, 0, 0);
		_delay_ms(10);
		multirotor_set_motors(&device, 0, 0, 0, 0);
		_delay_ms(1950);
	}
}*/


