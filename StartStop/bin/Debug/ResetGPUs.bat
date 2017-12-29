REM OverdriveNTool.exe -r0 -p0Vega56
devcon find *> list.txt
REM Choose a piece of the string that identifies the device, and verify it is unique:  
devcon find *DEV_130F
devcon disable *DEV_130F
REM devcon enable *DEV_130F