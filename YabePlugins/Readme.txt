------------------------------------------------------------------------------------------------
			               Yabe Plugins
------------------------------------------------------------------------------------------------

Plugins are compiled as Dll files and should be put close to Yabe.exe.
This allows users to adds private fonctionnalities & menus to Yabe (but C# knowledge is mandatory).

The settings parameter 'Plugins' should give the list of all plugins (names only without '.dll')
separated with a , or a  ; Without that plugins are not loaded at Yabe startup.


CheckReliability
	First plugin (based on an idea of Alexander Jaszkowski) to shows how to make it. 
	It check all objects in the selected device and displays all of them when the reliability 
	property value is not 0 (No Fault detected).

CheckStatusFlags
	Same as previous but with Status Flags.

ListCOV_Increment
	By Alexander Jaszkowski : displays all COV_INCREMENT values, for network's behaviour debug.

GlobalCommander
	By Lance Tollenaar : Allows to send the same value into several properties of multiple objects
	in multiple devices.

A usefull plugin by yourself, not too much dedicated to your application (or customizable), send it,
I'will add it.