OverdriveNTool.exe -consoleonly -r0 -r1 -r2 -r3 -r4 -r5 -r6  -p0Vega56 -p1RX580 -p2RX480 -p3RX580 -p4RX580 -p5RX580 -p6RX580

devcon find *> list.txt
devcon disable *DEV_687F
devcon enable *DEV_687F
timeout /t 5
