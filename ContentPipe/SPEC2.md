# The CDIR version 2 spec
I decided to redesign how CDIR worked a lil' bit.
Most things are still the same, such as the segment files, but the main
CDIR file has been revamped.

## Directory File base structure:
- Header - 4 bytes - "CDR2"
- Version - 4 bytes - UInt32 version number. Should be 2.
- Directory - ? bytes - The root directory(name may be null)
- Segments - 4 bytes - UInt32 length of following array
- Segment - 8 * [Segments] bytes - List of UInt64's specifying the starting index of a segment in memory.


## Directory structure:
- Name - ? bytes - A null-terminated string.
- Directories - 4 bytes - UInt32 count
- Directory - [Directories] * ? bytes - A list of Directories
- Files - 4 bytes - UInt32 count
- File - [Files] * ? bytes - A list of files
  - Name - ? bytes - A null-terminated string
  - Checksum - 8 bytes - UInt64 checksum for the file data
  - Offset - 8 bytes - UInt64 offset into the file data
  - Length - 4 bytes - UInt32 file data length

## Segment files
- Header - 4 bytes - "CSEG"
- Data - ? bytes - Raw file data
  all file reads should be done starting 4 bytes into the file.

## File naming
File names work as following:

The root dir is called "[dir].cdir", and each segment is "[dir].cdir_[part]".

So if we have `Content.cdir`, it will be split up into e.g the following files:
- `Content.cdir` - Directory File
- `Content.cdir_0` - Segment 0
- `Content.cdir_1` - Segment 1
- `Content.cdir_2` - Segment 2
- `Content.cdir_3` - Segment 3
  And so on