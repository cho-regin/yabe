------------------------------------------------------------------------------------------------
				Some codes based on Yabe stack
------------------------------------------------------------------------------------------------

LibBacnet
	Contains all the classes for a new development in the compiled file : LibBacnet.Dll
	could be useful for instance for Visual Basic programmers (or others)
	Ready for both client and server applications
	If using BacnetEthernet, copy also PacketDotNet.dll & SharpPcap.dll with LibBacnet.dll

BasicReadWrite
	Send a Whois to all devices on the net
	Get back all the Iam responses
	Read Present_Value property on the object ANALOG_INPUT:1 provided by the device 1026
	Write Present_Value property on the object ANALOG_OUTPUT:0 provided by the device 4000

BasicAdviseCOV
	Send a Whois to all devices on the net
	Get back all the Iam responses
	Advise to OBJECT_ANALOG_INPUT:1 provided by the device 1026, for 60 secondes
	Write on the console each notification

BasicAlarmListener
	Can send reponses to WhoIs query : own device id is 2000
	Can send responses to ReadProperty/ReadPropertyMultiple
	Write on the console each Alarm or Event received (broadcast or unicast)

BasicServer
	Send an Iam message : own device id is 1234
	Can send reponses to WhoIs query
	Offers three objects OBJECT_DEVICE:1234, OBJECT_ANALOG_INPUT:0, OBJECT_ANALOG_VALUE:0
	Only OBJECT_ANALOG_VALUE:0.PRESENT_VALUE could be write
	OBJECT_ANALOG_INPUT_0.PRESENT_VALUE change continously :
		PRESENT_VALUE = OBJECT_ANALOG_VALUE_0.PRESENT_VALUE * Sin (w.t);

BBMDDemo
	BBMD services on a simple device server with only one Device Object.
	Foreign devices accepted.
	Tested with
		- Wago 750/830 (vendor Id 222)
		- Newron DoGate (vendor Id 451)
		- Sauter EY-AS521 (vendor Id 80)
	Also running on a Raspberry Pi on Linux/Mono

AnotherStorageImplementation
	Shows another way, much complex than the original one in DeviceStorage.cs file
	(without the Xml descriptor also) to write a server.
	Each Bacnetobject is a object in the C# code.
	So a class must be written for each Bacnet object type, but this give the
	possibility to have a complex behaviour in objects code.
	Shows also dynamic creation/destruction of objects by a remote client.
	Actual objects types are :
		Device
		Structured View
		Analog Input, Analog Value, Analog Output
			(With Intrinsect reporting)
		Digital Input, Digial Value, Digital Output
		Multistates Input, Multistates Value, Multistates Output
			(With Intrinsect reporting)
		Characters String
		File
		Trendlog
		Notification Class
		Schedule
		Calendar
		DateTime
		... send us your own code !
	Objects persistance could be achieved with serialization

Bacnet.Room.Simulator
	This windows form application "simulates" an heating/cooling room controler.
	See the Readme file in the application directory.

Xamarin 
	This directory contains codes for Android devices, using Xamarin development 
	environment. See the Readme file in the directory.

RaspberrySample
	This application is similar to BasicServer, for Raspberry Pi with Mono.
	DeviceDescriptor.xml should be modify in order to access as your want
	the GPIO pins :
		OBJECT_BINARY_INPUT:x - GPIOX will be configured and used as an input
		OBJECT_BINARY_OUTPUT:x - GPIOX will be configured and used as an output
			the PRESENT_VALUE will be apply to the output at the begining

	When adding new objects, take care to add it also in the PROP_OBJECT_LIST of 
	the DEVICE_OBJECT

	In the original DeviceDescriptor.xml file one can found
		GPIO4 as input and GPIO7, GPIO8 as output

	Also OBJECT_ANALOG_INPUT:0 get the CPU temperature

	Mono should be installed (mono complete not mono runtime it's not enough !), 
	and to start the code in sudo mode : 
		sudo mono ./RaspberrySample.exe
	DeviceDescriptor.xml must be in the application directory

	Tested on a Raspberry Pi Model B. Similar code based on AnotherStorageImplementation  
	run 24/24 acting also as a BBMD somewhere in France.
 
	Ready with small modifications (Gpio) for Intel/Edison, Texas/BeagleBone, and a  
	lot of Linux plateforms with Mono installed.

RaspberryNetCore
	The exact copy of RaspberrySample in a visual Studio NetCore ready project

Enocean/Bacnet Gateway
	A work done by Christopher Guenther, using AnotherStorageImplementation, 
	is available at : http://sourceforge.net/projects/enocean-csharp/

BacnetToDatabase
	A sample application that will transfer all 'present values' from a given 
	device, to a SQL database. (SQL CE Local DB.)

Wheather2_to_Bacnet
	A windows service application (or console) able to get back
	WeatherUnlock data (http://www.weatherunlocked.com), and provides it on Bacnet.
	See comments in Readme file in the application directory and look at 
	Wheather2config.reg file for configuration.

	Available data are : Temperature, Windspeed, Humidity, Pressure, Winddir, 
	Weather Description, Sunset & Sunrise time, DewPoint & VaporPressure.
	No weather forecast data.

MultipleDevices
	Based on AnotherStorageImplementation.
	Shows how to run several devices, due to the Udp multisockets strategies used by 
	Yabe core code. All devices share the same Udp Port 47808 for broadcast activities
	and uses an exclusive socket for all others exchanges.

BACnetSCsampleNode
	Quite the same as BasicReadWrite but using BACnet/SC on TLS1.3 Websocket.
	Some X509 & a Hub is required (see BACnet/SC Refercence Stack on SourceForge : 
	https://sourceforge.net/projects/bacnet-sc-reference-stack/ ) for a starter.

BACnet/SC Hub 
	See the readme file in the corresponding subdirectory.
	EXE application is given for no Visual Studio users.
