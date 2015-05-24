/*
 * Bootloader main.c
 *
 * created: 07.03.2014
 *
 */ 
#include <system.h>


int main(void)
{
	io_init(); // initialize processor and built-in peripherals
	pwr_init(); // must be very early

	gpio_config_output(SIGNAL1_PIN, !SIGNAL1_PIN_ACTIVE_HIGH);
	gpio_config_output(SIGNAL2_PIN, !SIGNAL2_PIN_ACTIVE_HIGH);

	// signal DFU mode through motor activity
	do {
		gpio_set(SIGNAL1_PIN, SIGNAL1_PIN_ACTIVE_HIGH);
		gpio_set(SIGNAL2_PIN, SIGNAL2_PIN_ACTIVE_HIGH);
		_delay_ms(10);
		gpio_set(SIGNAL1_PIN, !SIGNAL1_PIN_ACTIVE_HIGH);
		gpio_set(SIGNAL2_PIN, !SIGNAL2_PIN_ACTIVE_HIGH);
		_delay_ms(990);
	} while (!pwr_switch_pressed());

	pwr_shutdown();
}
