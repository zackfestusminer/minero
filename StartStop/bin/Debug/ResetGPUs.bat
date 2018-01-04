SET PATH=%PATH%;C:\Users\ESTEBANO\Source\Repos\minero\StartStop\bin\Debug
OverdriveNTool.exe -consoleonly -r0  -p0Vega56

devcon find *> list.txt
devcon disable *DEV_687F
devcon enable *DEV_687F
timeout /t 5
