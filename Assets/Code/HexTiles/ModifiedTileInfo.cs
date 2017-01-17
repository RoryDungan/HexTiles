namespace HexTiles
{
    /// <summary>
    /// Information about a tile that has been modified, used for 
    /// recording undo actions.
    /// </summary>
    public struct ModifiedTileInfo
    {
        /// <summary>
        /// The chunk containing the tile that was added.
        /// </summary>
        public HexChunk Chunk { get; private set; }

        /// <summary>
        /// Whether the chunk was just created, already existed and was modified,
        /// or was deleted. Needed for recording what kind of Undo operation to use.
        /// </summary>
        public ChunkOperation Operation { get; private set; }

        public enum ChunkOperation 
        {
            Added,
            Modified
        }

        public ModifiedTileInfo(HexChunk chunk, ChunkOperation operation) 
        {
            Chunk = chunk;
            Operation = operation;
        }
    }
}