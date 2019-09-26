Mobile LIDAR Scanning Colourising tool developped by MachineSquad Ltd(UK)

The software works, but there is an issue with orientation. There is also a partially complete LAS reader class that will ideally need to be finished.

The way the software works is by locating which points have been acquired at the same time as a panoramic picture, and then using transforms from the SBET (.out) file it matches the points to their corresponding pixels. The timing of the pictures relies on an extra .dat file, in which the third line should be in the following format: UTCTime:dd/mm/yyyy hh:mm:ss.ssssss

The software is provided as is, under GNU AGPL license terms.
