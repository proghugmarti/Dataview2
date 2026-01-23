# DataView2
# Changelog

# Version Changes

## 1.1.45 (01/08/2025)
- Updated Survey Segmentation import and workflow
- Added chainage and a button (top of the map) to recalculate by survey 
- Synced z,x with video graphics and implemented the button for minimizing video player.
- Added "c" hotkey for play/stop button in video player when video layer is loaded.
- Added survey list page
- Updated LAS import message
- Added all the summary info from the db in the segment summary
- Projects are deleted from database if they fail to import.
- Fixed - now it shows an error message when any survey processing fails before displaying the processing log dialog.
- Adjusted code to support XML-only processing per survey in multi-survey workflow.
- Enabled color coding based on MTQ for crack summary
- LASPoint table hidden
- Changed the name's of downloaded CSV files from View Data - Tables to use the selected survey's file name.

## 1.1.40 (06/06/2025)
- Improved open/import project, and backups
- Survey Segmentation has its own button 
- Synced Crack Summary when cracks nodes are deleted. 
- Removed crack classification from auto defects in PCI rating and allow the user to select only one PCI out of two options
- Survey Segmentation improved, feature to read csv, and load surveys templates in the map 
- Auto rutting fixed, more consistent creating similar to manual calculations
- Video Frames improved, track angle added, and layer is hidden but still playing images. synced video with lcms images and segments
- Export bugs fixed
- Events closing to avoid memory leak
- Fixed drawing tool button  overlay from blocking map inputs
- Labels can be added when editing color coding. 
- Improved PCI-  added number of slabs for concrete. added section id in pci rating, removed pcc pavement to unit set. Not allowing same rating name. can import with number of slabs

## 1.1.39 (06/05/2025)
- Limit IRI Processing to Selected FIS Range.
- Improved creation of summary interval polygons in curved road.
- Optimized selecting and moving segments and defects to be faster.
- Fixed UI for import multi processing
- For processing saves the last backup only
- Added the exporting and importing functionality of sample unit set.
- Delete relevant summaries when surveys are deleted.
- Updated survey segmentation points show where user clicks.
- Added layer for segmentation.
- Improved PCI workflow, sample unit staying after refresh, added sample unit summary and added highlight for crack classification.
- Added a button to show/hide sample unit 
- Showing segment layer when loading the map and segments.
- Importing Shape files for survey Template (Woods Format) 
- New Survey Template Structure for Road work flow and support of long Survey Sets, now Txt file instead of a set of GeoJSON files.
- Added functionality to get coordinates of normal cameras with Positioning tables.
- Fixed reprocessing segments from map bug.
 
 
 

## 1.1.37 (4/17/2025)
- Fixed bug where layers were hidden from the Layer Menu if they contained no defects. 
- Fixed issues with the Multiselect tool. Fixed and changed behavior in LAS files, including polygon size, rut issues, complete GPS information, and export. 
- Fixed disappearing segment tooltips in the Manage Segment feature. 
- Fixed the listing of surveys after importing a project. 
- Fixed the issue where FOD data wasn't fetched in the UI. 
- Added PASER, Segment Grid table, and Severity properties to the QC. Additionally, introduced a new table, "Crack Summary," to display the entire crack by its Crack ID instead of by node. 
- Updated Survey Project Management features (e.g., creating projects, deleting projects, handling local files, import options, and displaying messages).


## 1.1.13 (11/12/2024)
- Bug fixed: PARSER checkbox and offset
- Added up to 25 spaces for new tables
- RoadInspect enabled PCI module 
- Added fault crack faulting object for processing as suboption of cracking
- Las files import can create a new survey or pick a existing survey

## 1.1.11 (04/12/2024)
- PCI summary 1/3 
- Changes in custom tables
- bug fix IRI images
- las files processing different type of point format
- Import projects

## 1.1.10 (03/12/2024)
- Updating rutting layer
- Button to redirect to specific survey in the map
- Upgrading las file processing, graphic and rutting calculation bugs 

## 1.1.9 (25/11/2024)
- Moved QC menu to the map side.
- Improved export data
- PASER as layer with severity colors
- Review crack classification independently 
- Las files importing and measuring rutting 


## 1.1.8 (19/11/2024)
- p2p and polyline in the survey template


## 1.1.7 (8/11/2024)
- Color code, edit and delete the new tables.
- Added the multiline and area measurement and allow this feature to be used from adding attributes.
- Reprocessing segments, with control restrictions
- Feature to delete the layers including metaTable, shapefiles by survey.


## 1.1.6 (1/11/2024)
- Table Views: new objects included and bug fixes, filter by survey
- Crack classification layer over map and jpg
- Fix bugs processing and crashing (licence verification, no data for layers, others)
- Offset by data and survey
- New Color Palete and fonts UI


## 1.1.5 (22/10/2024)
- Fix bugs processing and crashing
- Pavemetrics automatic PCI 

## 1.1.4 (18/10/2024)
- ERD, PPF Files produced in processing
- Import LAS Files
- Import Video

## 1.1.3 (16/10/2024)

- Sacks and Bumps in parallel after roughtnes calculation of entire surveyy
- LCMSAnalyser.dll 

## 1.1.2 (7/10/2024)

- Export images to Datahub.
- Export Areas and Blocks to Datahub.
- Added new objects from LCMS:
    1. Bleeding
    2. Curb and Dropoff
    3. Geometry
    4. Lane Marking (won't show on the map, only saving the data)
    5. Marking Contour
    6. MMO
    7. Pumping
    8. Ravelling
    9. Roughness
    10. Rumble Strip
    11. Rutting
    12. Sealed Crack
    13. Shove
    14. MacroTexture
    15. Water Traps
- Crack Classification (Segment Grid) included in processing (and new layer).

## 1.1.1 (8/8/2024)

- Export FOD objects to Datahub.
- Datahub export settings added to UI; user can change Datahub server.
- Disable actions while map is zooming; no more automatic loading depending on user view.

## 1.1.0 (5/08/2024)

- Export Shapefiles.
- Download License File using UI.
- User can now add new dynamic user table objects with their own fields.
- RumbleStrip and IRI new procedure.
- Export Cracks to Datahub.





