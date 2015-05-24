#*******************************************************
#
# Builds the bootloader for the S1 Baseband Processor
#
# created: 22.02.15
#
#*******************************************************


# application name
OUTPUT = BasebandBootloader
APPLICATION_NAME = OEM_Bootloader

# select hardware
PLATFORM = S1_QUADROCOPTER
PLATFORM_NAME = S1_Prototype_1_Baseband_Processor
ARCH = csr


# select required features
FEATURES = 			\
	BOOTLOADER		\
	BUILTIN_I2C_MASTER	\
	UNICODE			\
	DFU			\
	NVM			\
	PWM_1			\
	I2C_MASTER		\
	BLE_PERIPHERAL		\
	GATT_SERVICE		\
	I2C_SERVICE


# include application specific source files
SRC = main.c


# let the framework control the actual build process
FRAMEWORK = ./../../shared/
include $(FRAMEWORK)/Makefile
