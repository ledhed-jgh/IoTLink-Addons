# IoTLink-Addons
Addons for IoTLink

# Microsoft Teams Monitor
This Addon watches the MS Teams log file for status changes


## Installation
Extract the contents of MicrosoftTeamsMonitor_0.0.0.x.zip to:
```%ProgramData%\IOTLink\Addons\```. 
This should create a folder named .\MicrosoftTeamsMonitor


## Configuration
Edit config.yaml and change:
```teams:
     logfile: 
```
Point it to your Teams log file. Typically: ```C:\Users\YOUR-USERNAME-HERE\AppData\Roaming\Microsoft\Teams\logs.txt```. 
You can obtain the full path by running this in PowerShell as the user who's status you'd like to monitor with IoTLink:
```
Write-Host $env:APPDATA\Microsoft\Teams\logs.txt
```


## Home Assistant
I tried to include code to enable MQTT Autodiscovery in Home Assistant, but I haven't tested it as I don't currently run MQTT Autodiscovery.
MQTT Topic:
```
PREFIX/CLIENTID/microsoft-teams-monitor/status
```
PREFIX = 'prefix:' from %ProgramData%\IOTLink\Configs\configuration.yaml under 'General MQTT Settings'</br>
CLIENTID = 'clientId:' from %ProgramData%\IOTLink\Configs\configuration.yaml under 'General MQTT Settings'</br>

NOTE: If you leave the 'clientId:' blank I believe IoTLink uses a combination of WORKGROUP/HOSTNAME

### MQTT Sensor
```(yamnl)
sensor:
  - platform: mqtt
    name: My Teams Status
    state_topic: "iotlink/workgroup/hostname/microsoft-teams/status"
    value_template: "{{ value }}"
    icon: mdi:microsoft-teams
```


## Reference
* https://gitlab.com/iotlink/iotlink/-/wikis/Developers/Home


## Thanks
* thewindev - https://www.youtube.com/watch?v=kOBQ5vz7KMo
* Egglestron - https://community.home-assistant.io/t/microsoft-teams-status/202388/38
