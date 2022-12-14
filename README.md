<p align="center">
  <img src=https://user-images.githubusercontent.com/17510335/193412949-1803ce85-9d45-445e-86af-d24c07f90594.png width=200 height=200 />
</p>
<h1 align="center">EnergyStar X</h1>
<p align="center">
  Throttle background programs automatically for better battery life.
</p>
<br>

## Requirements

- Windows 11 21H2 or above. For best result, use Windows 11 22H2.
- The EcoQoS API requires a recent processor, see <https://devblogs.microsoft.com/performance-diagnostics/introducing-ecoqos> for details.

## Usage

### Unpackaged

- Get EnergyStar X from [Github Releases](https://github.com/ArakawaHenri/EnergyStarX/releases) in the version that matches your computer's ISA.
- Unzip and run EnergyStar.exe.
- Follow the instructions to install .Net 6.0 if you have not previously installed it.

### MSIX Packaged

- If you prefer to get releases via MSIX packages, you can:<br>
<a href="https://www.microsoft.com/store/apps/9NM58D33RWHJ" target="_blank" rel="noreferrer noopener"><img src=https://getbadgecdn.azureedge.net/images/en-us%20dark.svg width=240 height=100 /></a>

## Implemented Features & Known Issues

- [x] Start-with-System
- [ ] Program whitelist
- [ ] In some special cases this may result in programs that need performance being throttled because they are in the background. For example the JVM while the Java version of Minecraft is running.

## Privacy Statement

EnergyStar X does not collect any information, and is not guaranteed to be responsible for any privacy issues as well.

EnergyStar X不会收集您的任何信息，也不保证对您的任何隐私安全问题负责。

## Credits to

- This Project could not have been born without the support of [@imbushuo's original project](https://github.com/imbushuo/EnergyStar) and [@Shisheng233](https://github.com/Shisheng233).
- [@LinusL](https://github.com/Linus0080) made us the beautiful logo.
