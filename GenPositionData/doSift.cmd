echo off

set BIN=D:\ll\private\research\private\face\facesort\genpositiondata\bin\x86\release\GenPositionData.exe
set CLASSIFIER=D:\ll\private\research\private\CollaborativeLibs_01\LibFaceDetect\faceDetect\Classifier\classifier.txt
set orig=\\dpufsrv\Data\Images\faceDetect\recoSuites

for %%F in (testLabel testUnlabel ) do (
echo doing %%F
set cmd=%BIN% -normSIFT -siftPartNum 4  -generateReco  -detectPath %CLASSIFIER% -generatePatch -preDetectPhoto -skipFaceDetect -dataFilePrefix %%F_blur_ %orig%\%%F.txt

echo %BIN% -normSIFT -width 32 -height 32 -siftPartNum 4  -generateReco  -detectPath %CLASSIFIER% -generatePatch -preDetectPhoto -skipFaceDetect -dataFilePrefix %%F_sift_ %orig%\%%F.txt
%BIN% -normSIFT -width 32 -height 32  -siftPartNum 4  -generateReco  -detectPath %CLASSIFIER% -generatePatch -preDetectPhoto -skipFaceDetect -dataFilePrefix %%F_sift_ %orig%\%%F.txt


)
