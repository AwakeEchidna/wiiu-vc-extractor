# wiiu-vc-extractor
Extracts Wii U Virtual Console roms from dumps created via [DDD](https://github.com/dimok789/ddd/releases) or from the SNES Mini.

This currently only supports extracting GBA, NES, FDS, and SNES roms. 
# Note:
- Most VC titles are not clean roms but have been modified from their original state.
- Famicom Disk System games are not playable, but can be hex-edited to be playable.

## Installation
### Windows
1. Ensure that you have installed the .Net Framework version 4.5.2 or newer (https://docs.microsoft.com/en-us/dotnet/framework/install/)
2. Download the latest release zip file from https://github.com/wheatevo/wiiu-vc-extractor/releases/latest
3. Extract it to your local computer

### Linux
1. Ensure that you have installed Mono (http://www.mono-project.com/docs/getting-started/install/)
2. Download the latest release zip file from https://github.com/wheatevo/wiiu-vc-extractor/releases/latest
3. Extract it to your local computer

### Mac
1. Ensure that you have installed Mono (http://www.mono-project.com/docs/getting-started/install/)
2. Download the latest release zip file from https://github.com/wheatevo/wiiu-vc-extractor/releases/latest
3. Extract it to your local computer


## Basic Usage
`WiiuVcExtractor <dump_file>`

```
=====================================
Wii U Virtual Console Extractor 0.5.1
=====================================
Extracts roms from Virtual Console games dumped by DDD or from the SNES Mini.

Usage:
wiiuvcextractor [-v] [rpx_or_psb.m_file]
  - Extract a rom from a Virtual Console dump

wiiuvcextractor --version
  - Display current version


Usage Examples:
wiiuvcextractor alldata.psb.m
wiiuvcextractor WUP-FAME.rpx
wiiuvcextractor CLV-P-SAAAE.sfrom
wiiuvcextractor -v WUP-JBBE.rpx
```

### Running under Mono
`mono WiiuVcExtractor <dump_file>`

## Example Runs
### NES Extraction
```
WiiuVcExtractor.exe WUP-FCSE.rpx
============================================================================
Starting extraction of rom from WUP-FCSE.rpx...
============================================================================
RPX file detected!
Decompressing RPX file...
Decompression complete.
Checking if this is an NES VC title...
Checking WUP-FCSE.rpx.extract...
NES Rom Detected!
Virtual Console Title: WUP-FCSE
NES Title: MEGA MAN 6
Getting number of PRG and CHR pages...
PRG Pages: 32
CHR Pages: 0
Total NES rom size: 524304 Bytes
Fixing VC NES Header...
Getting rom data...
Writing to MEGA MAN 6.nes...
Writing NES rom header...
Writing NES rom data...
NES rom has been created successfully at MEGA MAN 6.nes
============================================================================
WUP-FCSE.rpx has been extracted to MEGA MAN 6.nes successfully.
============================================================================
```

### FDS Extraction
```
WiiuVcExtractor.exe WUP-FA9E.rpx
============================================================================
Starting extraction of rom from WUP-FA9E.rpx...
============================================================================
RPX file detected!
Decompressing RPX file...
Decompression complete.
Checking if this is an NES VC title...
Checking WUP-FA9E.rpx.extract...
Not an NES VC Title
Checking if this is an SNES VC title...
Checking WUP-FA9E.rpx.extract...
Checking for the SNES WUP header
Not an SNES VC Title
Checking if this is a Famicom Disk System VC title...
Checking WUP-FA9E.rpx.extract...
Famicom Disk System Rom Detected!
Virtual Console Title: WUP-FA9E
FDS Title: Super Mario Bros The Lost Levels
Total FDS rom size: 65500 Bytes
Getting rom data...
Writing to Super Mario Bros The Lost Levels.fds...
Writing rom data...
============================================================================
Famicom Disk System rom has been created successfully at Super Mario Bros The Lost Levels.fds
============================================================================
```

### SNES Extraction (rpx)
```
============================================================================
Starting extraction of rom from WUP-JA7E.rpx...
============================================================================
RPX file detected!
Decompressing RPX file...
Decompression complete.
Checking if this is an NES VC title...
Checking WUP-JA7E.rpx.extract...
Not an NES VC Title
Checking if this is an SNES VC title...
Checking WUP-JA7E.rpx.extract...
SNES Rom Detected!
Virtual Console Title: WUP-JA7E
SNES Title: Pilotwings
SNES Header Name: PILOTWINGS
Getting size of rom...
Total SNES rom size: 524288 Bytes
Getting rom data...
Extracting PCM Data...
Found the first PCM offset at 163937
Reading PCM data into memory...
Writing to Pilotwings.sfc...
Writing SNES rom data...
SNES rom has been created successfully at Pilotwings.sfc
============================================================================
WUP-JA7E.rpx has been extracted to Pilotwings.sfc successfully.
============================================================================
```

### SNES Extraction (sfrom)
```
============================================================================
Starting extraction of rom from CLV-P-SAAHE.sfrom...
============================================================================
Checking if this is an SNES VC title...
Checking CLV-P-SAAHE.sfrom...
SNES Rom Detected!
Virtual Console Title: WUP-JAJE
SNES Title: Super Metroid
SNES Header Name: Super Metroid
Getting size of rom...
Total SNES rom size: 3145728 Bytes
Getting rom data...
Extracting PCM Data...
Found the first PCM offset at 2609193
Reading PCM data into memory...
Writing to Super Metroid.sfc...
Writing SNES rom data...
SNES rom has been created successfully at Super Metroid.sfc
============================================================================
CLV-P-SAAHE.sfrom has been extracted to Super Metroid.sfc successfully.
============================================================================
```

### GBA Extraction
```
WiiuVcExtractor.exe alldata.psb.m
============================================================================
Starting extraction of rom from alldata.psb.m...
============================================================================
PSB file detected!
Decompressing PSB file...
Checking for PSB data file alldata.bin...
Found rom subfile at aawre1.120.m
    Offset: 36358144
    Length: 2408199
Decompressing rom...
Decompressing PSB file...
Checking if this is a GBA VC title...
Checking aawre1.120.m.extract...
GBA Rom Detected!
GBA Rom Code: AWRE
GBA Title: Advance Wars
Writing to Advance Wars.gba...
GBA rom has been created successfully at Advance Wars.gba
============================================================================
alldata.psb.m has been extracted to Advance Wars.gba successfully.
============================================================================
```

## Credits
Decompression of rpx files is possible due to the following tool created by 0CBH0: https://github.com/0CBH0/wiiurpxtool

Decompression and decryption of psb.m files is possible due to research and code created by ajd4096: https://github.com/ajd4096/inject_gba
