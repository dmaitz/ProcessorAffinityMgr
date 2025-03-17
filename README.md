# ProcessorAffinityMgr

ProcessorAffinityMgr is a user-friendly Windows service for managing process CPU affinity.  
With this service, you can control which type of CPU cores specific processes use to optimize system performance.

## Features

- **Easy Configuration**: Define which processes should run on specific CPU core type. (Performance (P)-Cores or Efficient (E)-Cores)
- **Automatic Application**: The service ensures that the defined affinities are applied whenever the processes start. (Windows Management Instrumentation, WMI)
- **Stability**: As a Windows service, ProcessorAffinityMgr runs in the background, ensuring consistent settings application.

## Installation

1. **Download**: https://github.com/dmaitz/ProcessorAffinityMgr/releases/latest
2. **Install** install ProcessorAffinityMgr.msi
3. **Configure** edit config.json in %ProgramFiles%\ProcessorAffinityMgr

## Configuration
Example:
```json
{
  "PCoreCount": 0,   
  "ProcessRules": [
    {
      "ProcessName": "chrome.exe",
      "Arguments": "--incognito",
      "CoreType": "p-core"
    },
    {
      "ProcessName": "chrome.exe",
      "Arguments": "--disable-gpu",
      "CoreType": "e-core"
    },
    {
      "ProcessName": "notepad.exe",
      "Arguments": "",
      "CoreType": "e-core"
    },
    {
      "ProcessName": "vlc.exe",
      "Arguments": "",
      "CoreType": "p-core"
    }
  ]
}
```

```"PCoreCount": 0``` -> overwrites core count type autodetection (e.g. for i5-13600KF PCoreCount: 12)

```json
"ProcessRules": [
    {
      "ProcessName": "chrome.exe",
      "Arguments": "--incognito",
      "CoreType": "p-core"
    }
]
```

```"ProcessName": "chrome.exe"``` -> binary filename  
```"Arguments": "--incognito"``` -> apply only when specific commandline arguments are used (contains check)  
```"CoreType": "p-core"``` -> p-core (all Performance-Cores) or e-core (all Efficient-Cores)  

## License
This project is licensed under the MIT License. See the [LICENSE](https://github.com/dmaitz/ProcessorAffinityMgr/blob/main/LICENSE) file for details.
