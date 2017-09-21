# RdbExporter

Exports data from the SWL .rdbdata files.

Requires the -i parameter with a path to the SWL Install Directory. The --rdb option specifies which RDB Type to export. If a type is unknown it will by default export as raw .dat files.


## Example Commands

Print help

    rdbexporter.exe -?
    
List RDB Types (use -la for all types)

    rdbexporter.exe -i "C:\Program Files (x86)\Funcom\Secret World Legends" -l

Export a specific RDB Type by ID:

    rdbexporter.exe -i "C:\Program Files (x86)\Funcom\Secret World Legends" --rdb 1010042
    
Export a specific RDB Type by name (should match the Name in list, in quotes if multiple words):

    rdbexporter.exe -i "C:\Program Files (x86)\Funcom\Secret World Legends" --rdb strings
    
    rdbexporter.exe -i "C:\Program Files (x86)\Funcom\Secret World Legends" --rdb "Flash Images"
