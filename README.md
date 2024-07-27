# Revit-add-in_RT2
Revitaddin_RT2, developed with BCK Architecture in Berlin, is designed to conduct acoustic analyses specifically within rectangular Revit models room.
It calculates reverberation times across various frequencies, utilizing both the Sabine and Eyring formulas. 
In addition, the add-in computes air absorption values, suggests optimal reverberation times, and identifies the Schroeder frequency along with standing wave frequencies—all based on the geometry of the room.

# System Requirements
•	Revit: Compatible with Revit 2023.
•	.NET Framework: Requires .NET Framework 4.8 to be installed.
•	SQLite: This add-in uses version 1.0.118.0. Ensure that this version or a compatible one is installed.

# Installation Instructions
1.	Download the Add-in:
•	Extract the ZIP file to a folder on your computer.

2.	Copy the Add-in Files:
•	Copy and extract the folder „Revit_RT2_addin“  to the Revit Add-ins folder on your system. The location will vary based on the version of Revit and your user setup:
•	For all users: C:\ProgramData\Autodesk\Revit\Addins\2023\
•	For the current user: C:\Users\[Username]\AppData\Roaming\Autodesk\Revit\Addins\2023\

3.	Verify Installation:
•	Launch Revit. You should see in the Revit Ribbon > Add-ins> External tools> RevitAddin_RT2




# Usage Instructions
•	Before using this Revit add-in, update the Absorption_coeff database table with the absorption coefficients of materials relevant to your project
•	The add-in has been created for an English version of Revit, so select ENG in the language section. 
•	To analyze room acoustics, select slabs and external walls in Revit (inclusive of windows and doors).

# Support
Developed in collaboration with BCK Architektur. Contributions to enhance functionality are welcome.

# License
Revitaddin_RT2 is available under the MIT License, permitting reuse, modification, and distribution.
