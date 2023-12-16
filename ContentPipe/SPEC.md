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
- Index - 20 * [Indices] bytes - List of file data structured as following:
  - Hash - 4 bytes - UInt32 hash(todo: decide hash system)
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

The root dir is called "[dir].cdir", and each segment is "[dir].cseg_[part]"