#*******************************************************
#
# FlightController Makefile
#
# Builds the I2C bootloader for the Flight Processor
#
# created: 24.02.15
#
#*******************************************************


# application name
OUTPUT = FlightControllerBootloader
APPLICATION_NAME = OEM_Bootloader

# select hardware
PLATFORM := S1_QUADROCOPTER
PLATFORM_NAME := S1_Prototype_1_Flight_Processor
ARCH := avr
MCU := ATxmega32E5


# select required features
FEATURES = 			\
	BOOTLOADER		\
	BUILTIN_SYSCLK_32MHZ	\
	BUILTIN_RTC		\
	BUILTIN_TWIC_SLAVE	\
	POWER_MGR		\
	UNICODE			\
	DFU			\
	NVM


# since this is a bootloader, we must specify the offset (byte address for the ATXmega32E5)
# the bootloader starts before the actual bootloader section because it doesn't fit
BOOTLOADER_OFFSET := 0x7000
BOOTLOADER_SECTION := 0x8000

# include application specific source files
SRC = main.c


# let the framework control the actual build process
FRAMEWORK = ./../../shared/
include $(FRAMEWORK)/Makefile
