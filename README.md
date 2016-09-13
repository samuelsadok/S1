# AppInstall Innovation Labs S1


The S1 project is a small pocket drone, controlled by your smartphone via Bluetooth.


## Project State

Until now, about 10 instances of a prototype were manufactured. The firmware was written to a point where the throttle could be controlled by an iOS App. However, since then, the PID-controller has never been properly tuned, so flight could not yet be demonstrated.

To allow for better understanding of the physical system, and ultimately enable autonomous controller calibration, the implementation of a software-in-the-loop simulator was started.

The desire to make the contol protocol and application more universal contributed to the inception of the AmbientOS project. Since then, no progress was made on the S1 project. It will be resumed once the AmbientOS framework is capable enough to carry the S1 control App.

## Hardware Overview

The most important hardware components on the S1 prototype 1 hardware are:
- an AVR ATXmega32E5 microcontroller, dedicated to flight control tasks ("flight controller")
- a CSR1010 bluetooth microcontroller, dedicated to bluetooth communication ("baseband processor")
- an InvenSense MPU6050 inertial sensor (connected to the flight controller)
- 4 MOSFETs used to control the motors (connected to the flight controller)

The two microcontrollers are connected through an I2C connection, where the baseband processor
is the master and the flight controller is the slave.


## Known Hardware Issues

Hardware bugs in the S1 prototype 1 hardware, to be solved in updated versions:

 - the power supply is not stable when the motors draw a lot of current (possibly solved using a large (330 µF) capacitor)
 - the motor drivers require a capacitor and a resistor to protect them from high currents (else they break after some time)
 - the LEDs (and motors?) must not be connected to BAT- as this prevents the device from switching off completely


## Folder Structure

| baseband			|    application specific files for the baseband controller firmware                         |
| baseband_bootloader		|    application specific files for the baseband controller bootloader                       |
| flightcontroller		|    application specific files for the flight controller firmware                           |
| flightcontroller_bootloader	|    application specific files for the flight controller bootloader                         |
| factory_bootloader	    	|    application specific files for the factory bootloader                                   |
| asf *				|    Atmel Software Framework (hardware abstraction for Atmel microcontrollers)              |
| shared-c *			|    C code shared by all firmware applications                                              |
| shared-c# *			|    C# code shared by all applications                                                      |
| schematics			|    Altium Designer circuit schematics and PCB layout of the quadrocopter and base station  |

* these folders are git subtrees (i.e. they are git repositories themselves)




## Git Structure

[To be defined]

