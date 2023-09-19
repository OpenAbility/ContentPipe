# The the CPKG format
The CPKG format is a basic file format for storing assets.
It is closely based on formats like VPK and WAD.

The core concept behind it is to store all of the data in a node tree.

The file works in a little-endian manner(since x86 is little-endian).

| NAME          | SIZE    | DESCRIPTION                                    |
|---------------|---------|------------------------------------------------|
| Header        | 4 bytes | Should be "CPKG"                               |
| Version Major | 2 bytes | Specifies the CPRT version                     |
| Version Minor | 2 bytes | Specifies the CPRT version                     |
| PRTS header   | 4 bytes | Should be "PRTS"                               |
| Part Count    | 4 bytes | Specifies how many parts we expect to read     |
All values are unsigned.

Part 0 is ALWAYS the nodes part.

# Nodes

Base nodes are built as following:

| NAME  | SIZE   | DESCRIPTION                               |
|-------|--------|-------------------------------------------|
| Flags | 1 byte | A set of flags for the node, see the spec |
And flags are as following:

| NAME      | SIZE  | DESCRIPTION                                        |
|-----------|-------|----------------------------------------------------|
| Directory | 1 bit | If true, the node has child nodes, otherwise false |
| Link      | 1 bit | If true, this node is a link to another node       |

Then, for further structure, there are 3 main types of nodes
## File Nodes
A file node is, outside of the "Flags" field, specified like this:

| NAME  | SIZE    | DESCRIPTION                            |
|-------|---------|----------------------------------------|
| Part  | 4 bytes | The part in which the data is stored   |
| Chunk | 4 bytes | The chunk where the node data starts   |
## Directory Nodes

| NAME        | SIZE    | DESCRIPTION                                                                                           |
|-------------|---------|-------------------------------------------------------------------------------------------------------|
| Child Count | 4 bytes | How many children are in this directory?                                                              |
| Children    | ?       | A list of pairs of null-term strings & ints, the string being the name and the int being the child ID |
## Link Nodes
A link node just has a 4 byte uint to the node it points to.

# Parts
Parts are another big part of the content system.

They can be used to split apart large file systems into a smaller, more
digestible manner, reduce file sizes and optimize for disk reading.

A part is stored in a CPRT file, which is structured as following:

| NAME          | SIZE    | DESCRIPTION                                                   |
|---------------|---------|---------------------------------------------------------------|
| Header        | 4 bytes | Should be "CPRT"                                              |
| Version Major | 2 bytes | Specifies the CPRT version                                    |
| Version Minor | 2 bytes | Specifies the CPRT version                                    |
| Chunk Count   | 4 bytes | Specifies the number of chunks                                |
| Chunks        | ?       | *[Chunk Count]* pairs of 2 uint32s. #1 is start and #2 is end |
| Data Size     | 4 bytes | The size of the data in the file                              |
| Data          | ?       | The raw bytes representing the data in the file               |

Please note that all parts are storing their positions relative to the start
of the chunk.

The part file with nodes may contain nodes, only nodes and nothing but nodes.

The node with index 0 should be at chunk 0, index 1 should be chunk 1 etc.