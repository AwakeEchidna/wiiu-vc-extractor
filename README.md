# wiiu-vc-extractor
Extracts Wii U Virtual Console roms from dumps created via DDD

This currently only supports extracting NES and SNES roms. Note that most VC titles are not clean roms but have been modified from their original state.

## Basic Usage
`WiiuVcExtractor <rpx_file>`

`WiiuVcExtractor WUP-FCSE.rpx`

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

### SNES Extraction
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
Getting size of rom...
Total SNES rom size: 524288 Bytes
Getting rom data...
Writing to Pilotwings.smc...
Writing SNES rom data...
SNES rom has been created successfully at Pilotwings.smc
============================================================================
WUP-JA7E.rpx has been extracted to Pilotwings.smc successfully.
============================================================================
```

## Credits
Decompression of rpx files is possible due to the following tool created by 0CBH0:
https://github.com/0CBH0/wiiurpxtool

The upcoming GBA support will be possible due to research and code performed by ajd4096:
https://github.com/ajd4096/inject_gba
