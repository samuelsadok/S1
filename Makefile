
.DEFAULT_GOAL = all
QUOTE="


SIMULATOR := C:/Data/Code/C\#/RemoteControl/Simulator/bin/Debug/Simulator.exe


#================================================================
# locations of all firmware files
#================================================================

BASEBAND_DIR := baseband
BASEBAND_IMG := $(BASEBAND_DIR)/depend_Release_CSR101x_A05/Baseband.img
BASEBAND_BOOTLOADER_DIR := baseband_bootloader
BASEBAND_BOOTLOADER_IMG := $(BASEBAND_BOOTLOADER_DIR)/depend_Release_CSR101x_A05/BasebandBootloader.img
FLIGHT_CONTROLLER_DIR := flightcontroller
FLIGHT_CONTROLLER_IMG := $(FLIGHT_CONTROLLER_DIR)/bin/FlightController.hex
FLIGHT_CONTROLLER_SIM := $(FLIGHT_CONTROLLER_DIR)/simulation/FlightController.exe
FLIGHT_CONTROLLER_BOOTLOADER_DIR := flightcontroller_bootloader
FLIGHT_CONTROLLER_BOOTLOADER_IMG := $(FLIGHT_CONTROLLER_BOOTLOADER_DIR)/bin/FlightControllerBootloader.hex
FACTORY_BOOTLOADER_DIR := factory_bootloader
FACTORY_BOOTLOADER_IMG := $(FACTORY_BOOTLOADER_DIR)/bin/FactoryBootloader.hex

ALL_IMGS := \
	$(BASEBAND_IMG) \
	$(BASEBAND_BOOTLOADER_IMG) \
	$(FLIGHT_CONTROLLER_IMG) \
	$(FLIGHT_CONTROLLER_BOOTLOADER_IMG) \
	$(FACTORY_BOOTLOADER_IMG)

ALL_DIRS := \
	$(BASEBAND_DIR) \
	$(BASEBAND_BOOTLOADER_DIR) \
	$(FLIGHT_CONTROLLER_DIR) \
	$(FLIGHT_CONTROLLER_BOOTLOADER_DIR) \
	$(FACTORY_BOOTLOADER_DIR)

OUTDIR := ./bin
FACTORY_IMG := $(OUTDIR)/s1_factory_bootloader.hex
FIRMWARE_IMG := $(OUTDIR)/s1_firmware.bin


#================================================================
# build rules for all firmware components
#================================================================

$(BASEBAND_IMG):
	(cd $(BASEBAND_DIR); make all)

$(BASEBAND_BOOTLOADER_IMG):
	(cd $(BASEBAND_BOOTLOADER_DIR); make all)

$(FLIGHT_CONTROLLER_IMG):
	(cd $(FLIGHT_CONTROLLER_DIR); make all)

$(FLIGHT_CONTROLLER_SIM):
	(cd $(FLIGHT_CONTROLLER_DIR); make -f MakeSimulation all)

$(FLIGHT_CONTROLLER_BOOTLOADER_IMG):
	(cd $(FLIGHT_CONTROLLER_BOOTLOADER_DIR); make all)

$(FACTORY_BOOTLOADER_IMG):
	(cd $(FACTORY_BOOTLOADER_DIR); make all)





#================================================================
# output files
#================================================================

$(OUTDIR):
	mkdir $(QUOTE)$(OUTDIR)$(QUOTE)

# merge 2 hex files into one (the addresses don't overlap by design)
$(FACTORY_IMG): $(OUTDIR) $(FACTORY_BOOTLOADER_IMG) $(FLIGHT_CONTROLLER_BOOTLOADER_IMG)
	sed -e '/^:00[0-9a-fA-F]\{4\}01[0-9a-fA-F]\{2\}$$/ r $(subst /,\/,$(FLIGHT_CONTROLLER_BOOTLOADER_IMG))' -e '//d' < $(FACTORY_BOOTLOADER_IMG) > $(FACTORY_IMG)

# merge binary firmware files for both MCU's into a single firmware file
$(FIRMWARE_IMG): $(OUTDIR) $(BASEBAND_IMG) $(FLIGHT_CONTROLLER_IMG)




#================================================================
# build rules
#================================================================	

all: $(FACTORY_IMG) $(FIRMWARE_IMG)

factory-install: $(FACTORY_IMG)

# todo: make path relative to working directory
simulate: $(FLIGHT_CONTROLLER_SIM) FORCE
	(cd .; $(QUOTE)$(SIMULATOR)$(QUOTE) C:/Data/Projects/s1/s1.xml)

clean:
	(cd $(FACTORY_BOOTLOADER_DIR); make clean)

FORCE:

.PHONY: all clean
