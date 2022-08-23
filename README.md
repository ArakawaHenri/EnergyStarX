# EnergyStar

Throttle background programs automatically for better battery life.

## Requirements

- Windows 11 21H2 or above is required. For best result, use Windows 11 22H2.
- The EcoQoS API requires a recent processor, see https://devblogs.microsoft.com/performance-diagnostics/introducing-ecoqos/ for details.

## Usage

- Unzip and run EnergyStar.exe

## Known Issues

- Currently, the program stores the config file under `~\AppData\Local\EnergyStar\ApplicationData\` for data persistance. The configuration file will instead be stored in the directory where the program is located.
- Start-with-System has not yet been implemented.
- Program whitelisting has not yet been implemented.
- In some special cases this may result in programs that need performance being throttled because they are in the background. For example the JVM while the Java version of Minecraft is running.
