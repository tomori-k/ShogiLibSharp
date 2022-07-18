SET COMPlus_EnableAVX2=1
SET COMPlus_EnableSSE41=1
SET COMPlus_EnableSSSE3=1
SET COMPlus_EnableSSE2=1

dotnet test -c Release

SET COMPlus_EnableAVX2=0
SET COMPlus_EnableSSE41=1
SET COMPlus_EnableSSSE3=1
SET COMPlus_EnableSSE2=1

dotnet test -c Release

SET COMPlus_EnableAVX2=0
SET COMPlus_EnableSSE41=0
SET COMPlus_EnableSSSE3=1
SET COMPlus_EnableSSE2=1

dotnet test -c Release

SET COMPlus_EnableAVX2=0
SET COMPlus_EnableSSE41=0
SET COMPlus_EnableSSSE3=0
SET COMPlus_EnableSSE2=1

dotnet test -c Release

SET COMPlus_EnableAVX2=0
SET COMPlus_EnableSSE41=0
SET COMPlus_EnableSSSE3=0
SET COMPlus_EnableSSE2=0

dotnet test -c Release