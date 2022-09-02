# EnergyStar-GUI 

<img src=https://user-images.githubusercontent.com/17510335/188151471-c3793437-a3ff-40bf-9168-efcbd3bd2613.png width=240 height=240 /><br>
Throttle background programs automatically for better battery life. 

## Requirements 

- Windows 11 21H2 or above is required. For best result, use Windows 11 22H2. 
- The EcoQoS API requires a recent processor, see https://devblogs.microsoft.com/performance-diagnostics/introducing-ecoqos/ for details. 

## Usage 

- Unzip and run EnergyStar.exe 
- Follow the instructions to install .Net 6.0 if you have not previously installed it. 

## Known Issues 

- Currently, the program stores the config file under `~\AppData\Local\EnergyStar\ApplicationData\` for data persistance. The configuration file will instead be stored in the directory where the program is located in future releases. 
- ~~Start-with-System has not yet been implemented.~~
- Program whitelisting has not yet been implemented. 
- In some special cases this may result in programs that need performance being throttled because they are in the background. For example the JVM while the Java version of Minecraft is running. 

## Credits to 

- This Project could not have been born without the support of [@imbushuo's original project](https://github.com/imbushuo/EnergyStar) and [@Shisheng233](https://github.com/Shisheng233). 

## Privacy Statement 

EnergyStar X does not collect any information, and is not guaranteed to be responsible for any privacy issues as well.<br>
EnergyStar X不会收集您的任何信息，也不保证对您的任何隐私安全问题负责。 
