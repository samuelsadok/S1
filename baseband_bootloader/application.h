/*
*
* S1 Baseband Processor bootloader application specific definitions
*
* created: 14.03.15
*
*/
#ifndef __APPLICATION_H__
#define __APPLICATION_H__


/* error handling */

#define AUTO_RESET



/* Non-volatile memory allocations (this must match the allocation used in the main application) */

#define NVM_VERSION				(0)
#define NVM_SANITY_OFFSET		(0)
#define NVM_SANITY_LENGTH		(2)
#define NVM_APPINFO_OFFSET		(NVM_SANITY_OFFSET + NVM_SANITY_LENGTH)
#define NVM_APPINFO_LENGTH		(6)
#define NVM_BUG_LOG_OFFSET		(NVM_APPINFO_OFFSET + NVM_APPINFO_LENGTH)
#define NVM_BUG_LOG_LENGTH		(8)
#define NVM_BLE_DATA_OFFSET		(NVM_BUG_LOG_OFFSET + NVM_BUG_LOG_LENGTH)
#define NVM_BLE_DATA_LENGTH		(36)
#define NVM_FIXUP_OFFSET		(NVM_BLE_DATA_OFFSET + NVM_BLE_DATA_LENGTH)



/* BLE services */

#define BLE_ENDPOINTS {								\
	BLE_IMPORT_ENDPOINT(gatt, GATT_SERVICE),		\
	BLE_DFU_SERVICE									\
}



/* DFU mode settings */

// can't hold boot process on the baseband processor (need to hold on DFU slave instead)
#define DFU_HOLD_CONDITION	(0)
#define DFU_SLAVES			{ CREATE_BUILTIN_I2C_DEVICE(FLIGHT_CONTROLLER_ADDRESS, 2) }



/* bluetooth low energy settings */

#define BLE_APPEARANCE		BLE_APPEARANCE_UNKNOWN
#define BLE_MAIN_SERVICE	UUID_FLIGHT_SERVICE // todo: adjust
#define BLE_DEVICE_NAME		"S1 Prototype DFU Mode"
#define BLE_PAIRING_NONE
#define BLE_PUBLIC_ADDRESS

// If this value is too large, a stack overflow can occur in the access indication handler (ble_gatt_access_indication)
#define BLE_MAX_TRANSFER_LENGTH		100 // check with ATT_MTU
#define BLE_MAX_CONNECTIONS			(1)	// maximum number of concurrent connections
#define BLE_MAX_BONDS				(1) // number of bonds that application should be able remember

// interval values for advertising
#define BLE_FC_ADVERTISING_INTERVAL_MIN          (60 * MILLISECOND)
#define BLE_FC_ADVERTISING_INTERVAL_MAX          (60 * MILLISECOND)
#define BLE_RP_ADVERTISING_INTERVAL_MIN          (1280 * MILLISECOND)
#define BLE_RP_ADVERTISING_INTERVAL_MAX          (1280 * MILLISECOND)

// Time out values for fast and slow advertisements (in ms)
#define BLE_BONDED_DEVICE_ADVERT_TIMEOUT_VALUE	(10000)
#define BLE_FAST_ADV_TIMEOUT					(30000)
#define BLE_SLOW_ADV_TIMEOUT					(60000)

// Maximum number of connection parameter update requests that can be send when connected
#define BLE_MAX_NUM_CONN_PARAM_UPDATE_REQS       (2)

// Preferred connection parameter values should be within the range specified in the Bluetooth specification.

// connection interval in number of frames
#define BLE_PREFERRED_MAX_CON_INTERVAL          0x0320 // 1s
#define BLE_PREFERRED_MIN_CON_INTERVAL          0x0320 // 1s

// Slave latency in number of connection intervals
#define BLE_PREFERRED_SLAVE_LATENCY             0x0000 // 0 conn_intervals

// Supervision timeout (ms) = PREFERRED_SUPERVISION_TIMEOUT * 10 ms
#define BLE_PREFERRED_SUPERVISION_TIMEOUT       0x0258 // 6s

// The application should retry the 'connection paramter update' procedure after time TGAP(conn_param_timeout) which is 30 seconds.
#define BLE_GAP_CONN_PARAM_TIMEOUT				(30000)



#endif // __APPLICATION_H__
