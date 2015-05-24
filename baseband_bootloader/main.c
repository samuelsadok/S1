

#include <system.h>


timer_t ledTimer;

void led_toggle(void *context) {
	static bool state = 0;

	state = !state;
	PioSet(PIO_LED_R, state);
	PioSet(PIO_LED_L, !state);

	ledTimer = (timer_t) CREATE_TIMER(100, led_toggle, 0);
	timer_start(&ledTimer);
}


int main(void) {
	PioSetModes((1UL << PIO_LED_L) | (1UL << PIO_LED_R), pio_mode_user);
	PioSetDir(PIO_LED_L, TRUE);
	PioSetDir(PIO_LED_R, TRUE);
	PioSetPullModes((1UL << PIO_LED_L) | (1UL << PIO_LED_R),
					pio_mode_strong_pull_up);

	ble_start();

	led_toggle(NULL);

	return 0;
}
