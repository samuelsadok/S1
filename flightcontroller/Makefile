#*******************************************************
#
# FlightController Makefile
#
# Builds the firmware for the Flight Processor
#
# created: 24.02.15
#
#*******************************************************


# application name
OUTPUT = FlightController
APPLICATION_NAME = OEM_Flight_Controller

# select hardware
PLATFORM := S1_QUADROCOPTER
PLATFORM_NAME := S1_Prototype_1_Flight_Processor
ARCH := avr
MCU := ATxmega32E5


# select required features
FEATURES = 			\
	BUILTIN_SYSCLK_32MHZ	\
	BUILTIN_RTC		\
	BUILTIN_TWIC_MASTER	\
	BUILTIN_TWIC_SLAVE	\
	BUILTIN_TIMERS		\
	BUILTIN_PWM		\
	EXT_INT			\
	POWER_MGR		\
	I2C_MASTER		\
	UNICODE			\
	DFU			\
	NVM			\
	MATH_EXTENSIONS		\
	MPU			\
	MPU6050			\
	UAV			\
	LOCAL_UAV		\
	PWM_MOTOR


# include application specific source files
SRC = main.c


# let the framework control the actual build process
FRAMEWORK = ./../shared-c/
include $(FRAMEWORK)/Makefile
