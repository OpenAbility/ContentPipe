# The CDIR Format

## Concept
The idea is as following:
- We split up a large filesystem into smaller, more digestible files
- Reading files should be as fast as possible. We can do this by simply storing a lookup
   table using file name hashes.
- Basically, it's a segmented tarball.

## Directory files
It's time to bring in the magic!

Each CPKG is specified as a directory. This directory is a simple file with the following structure:

- Header - 4 bytes - "CDIR"
- Indices - 4 bytes - UInt32 length of following array
- Index - 16 * [Indices] bytes - List of file data structured as following:
  - Hash - 4 bytes - UInt32 hash(todo: decide hash system)
  - Checksum - 4 bytes - UInt64 checksum for the file data.
  - Offset - 8 bytes - UInt64 offset into the file data
  - Length - 4 bytes - UInt32
- Segments - 4 bytes - UInt32 length of following array
- Segment - 8 * [Segments] bytes - List of UInt64's specifying the starting index of a segment in memory.

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