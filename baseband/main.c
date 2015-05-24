

#include <system.h>



int main(void) {

	// todo: init LEDs
	// todo: init comms with flight processor

	PioSetModes((1UL << PIO_LED_L) | (1UL << PIO_LED_R), pio_mode_user);
	PioSetDir(PIO_LED_L, TRUE);
	PioSetDir(PIO_LED_R, TRUE);
	PioSetPullModes((1UL << PIO_LED_L) | (1UL << PIO_LED_R),
					pio_mode_strong_pull_up);

	PioSet(PIO_LED_R, 1UL);
	PioSet(PIO_LED_L, 1UL);

	// todo: initialize profile specific data
	// todo: read configuration store (blah = CSReadUserKey(KEY_INDEX);)

	ble_start();

	return 0;
}
