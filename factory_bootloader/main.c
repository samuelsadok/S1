/*
 * S1 Factory Bootloader main.c
 *
 * This application is to be installed on the Flight Controller alongside the Flight Controller I2C Bootloader.
 * Installation is done via the PDI connector, hence this application can also be used whenever the device is bricked.
 *
 * It writes the bootloader for the baseband processor onto the on-board EEPROM.
 * After the written data is verified, the factory bootloader disables itself to place the Flight Controller
 * into DFU mode. At this point the device is ready to receive an initial over-the-air firmware update.
 *
 * created: 07.03.2014
 *
 */ 

#include <system.h>
#include <hardware/soft_i2c_master.h>
#include <hardware/i2c_eeprom.h>
#include <hardware/nvm.h>


#define FIRMWARE_BUFFER_SIZE	256
char buffer[FIRMWARE_BUFFER_SIZE];

#define CONTROL_HEADER_SIZE		(6) // the first 3 words in the image file are control header
const char image[] PROGMEM = {
#  include "bootloader_img.h" // this file is generated from the baseband bootloader image before the compiler is invoked (handled by the makefile)
};


i2c_device_t eeprom = ONBOARD_EEPROM;


// Writes the firmware onto the on-board EEPROM.
status_t flash_firmware(size_t offset, size_t length) {
	status_t status;
	size_t addr;

	// write
	addr = offset;
	for (size_t len = length; len;) {
		size_t bytes = min(FIRMWARE_BUFFER_SIZE, len);
		memcpy_P(buffer, image + addr, bytes);
		if ((status = i2c_eeprom_write(&eeprom, addr, buffer, bytes)))
			return status;
		addr += bytes;
		len -= bytes;
	}

	// verify
	addr = offset;
	for (size_t len = length; len;) {
		size_t bytes = min(FIRMWARE_BUFFER_SIZE, len);
		if ((status = i2c_eeprom_read(&eeprom, addr, buffer, bytes)))
			return status;
		if (memcmp_P(buffer, image + addr, bytes))
			return STATUS_DATA_CORRUPT;
		addr += bytes;
		len -= bytes;
	}

	return STATUS_SUCCESS;
}


// Renders the baseband firmware unusable
status_t destroy_firmware(void) {
	char trash[CONTROL_HEADER_SIZE] = { 0 }; // clear the control header
	return i2c_eeprom_write(&eeprom, 0, trash, sizeof(trash));
}


void set_signal(bool on) {
	gpio_set(SIGNAL1_PIN, on == SIGNAL1_PIN_ACTIVE_HIGH);
	gpio_set(SIGNAL2_PIN, on == SIGNAL2_PIN_ACTIVE_HIGH);
}


int main(void)
{
	io_init(); // initialize processor and built-in peripherals
	soft_i2c_init();

	gpio_config_output(SIGNAL1_PIN, !SIGNAL1_PIN_ACTIVE_HIGH);
	gpio_config_output(SIGNAL2_PIN, !SIGNAL2_PIN_ACTIVE_HIGH);

	// wait some time to ensure that the baseband processor no longer accesses the I2C EEPROM
	_delay_ms(1000);

	// signal the start of the flash procedure
	set_signal(1);
	_delay_ms(50);
	set_signal(0);


	// write firmware to EEPROM
	status_t status;

	if ((status = destroy_firmware()))
		bug_check(status, 0);
	if ((status = flash_firmware(CONTROL_HEADER_SIZE, sizeof(image) - CONTROL_HEADER_SIZE)))
		bug_check(status, 1);
	if ((status = flash_firmware(0, CONTROL_HEADER_SIZE)))
		bug_check(status, 1);


	// trash NVM data and reset, so that the local bootloader starts normally
	char trash[NVM_SANITY_LENGTH] = { 0 };
	nvm_write(NVM_SANITY_OFFSET, trash, NVM_SANITY_LENGTH);
	__reset();
}
