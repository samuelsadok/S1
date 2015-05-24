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
OUTPUT = FactoryBootloader
APPLICATION_NAME = OEM_Bootloader

# select hardware
PLATFORM := S1_QUADROCOPTER
PLATFORM_NAME := S1_Prototype_1_Flight_Processor
ARCH := avr
MCU := ATxmega32E5


# select required features
FEATURES = 			\
	BUILTIN_SYSCLK_32MHZ	\
	BUILTIN_RTC		\
	NVM			\
	I2C_MASTER		\
	SOFT_I2C_MASTER		\
	I2C_EEPROM


# include application specific source files
SRC = main.c




#================================================================
# special procedure to embed baseband bootloader
#================================================================

BOOTLOADER_DIR := ../baseband_bootloader
BOOTLOADER_IMG := $(BOOTLOADER_DIR)/depend_Release_CSR101x_A05/baseband.img

# the baseband bootloader image is embedded in main.c
main.c: bootloader_img.h

# convert bootloader image from XUV-format to a C-array
bootloader_img.h: $(BOOTLOADER_IMG)
	sed -e 's/^ *@\([0-9a-fA-F]*\)/\/\* \1 \*\//g' -e 's/ *\([0-9a-fA-F]\{2\}\)\([0-9a-fA-F]\{2\}\)$$/ 0x\2, 0x\1,/g' $(BOOTLOADER_IMG) > bootloader_img.h

# compile baseband bootloader
ifdef DONT_BUILD_BOOTLOADER
MAKE_BOOTLOADER := echo omitting baseband bootloader remake
else
MAKE_BOOTLOADER := make all
endif
$(BOOTLOADER_IMG):
	@(cd $(BOOTLOADER_DIR); $(MAKE_BOOTLOADER))


	

# let the framework control the actual build process
FRAMEWORK = ./../../shared/
include $(FRAMEWORK)/Makefile
