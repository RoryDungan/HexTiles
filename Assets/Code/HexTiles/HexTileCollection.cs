using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HexTiles
{
    /// <summary>
    /// Wrapper around our internal data structure for storing hex tiles.
    /// </summary>
    public interface IHexTileCollection : IEnumerable<HexTile>
    {
        /// <summary>
        /// Add a tile to the collection.
        /// </summary>
        void Add(HexCoords coords, HexTile tile);

        /// <summary>
        /// Remove a tile at the specified coordinates.
        /// </summary>
        void Remove(HexCoords coords);

        /// <summary>
        /// Delete all elements of the collection.
        /// </summary>
        void Clear();

        /// <summary>
        /// Return whether or not our collection contains the specified tile.
        /// </summary>
        bool Contains(HexCoords key);

        /// <summary>
        /// Array accessor.
        /// </summary>
        HexTile this[HexCoords index] { get; }

        /// <summary>
        /// Try to get the value at the specified coordinates and return whether it was successful or not.
        /// </summary>
        bool TryGetValue(HexCoords index, out HexTile tile);

        /// <summary>
        /// Enumerable of all the coordinates stored in this collection.
        /// </summary>
        ICollection<HexCoords> Keys { get; }
    }

    /// <summary>
    /// Wrapper around our internal data structure for storing hex tiles.
    /// </summary>
    class HexTileCollection : IHexTileCollection
    {
        private Hashtable tiles = new Hashtable();

        /// <summary>
        /// Array accessor.
        /// </summary>
        public HexTile this[HexCoords index]
        {
            get
            {
                return (HexTile)tiles[index];
            }
        }

        /// <summary>
        /// Add a tile to the collection.
        /// </summary>
        public void Add(HexCoords coords, HexTile tile)
        {
            tiles.Add(coords, tile);
        }

        /// <summary>
        /// Remove a tile at the specified coordinates.
        /// </summary>
        public void Remove(HexCoords coords)
        {
            tiles.Remove(coords);
        }

        /// <summary>
        /// Delete all elements of the collection.
        /// </summary>
        public void Clear()
        {
            tiles.Clear();
        }

        /// <summary>
        /// Enumerable of all the coordinates stored in this collection.
        /// </summary>
        public ICollection<HexCoords> Keys
        {
            get
            {
                return (ICollection<HexCoords>)tiles.Keys;
            }
        }

        /// <summary>
        /// Return whether or not our collection contains the specified tile.
        /// </summary>
        public bool Contains(HexCoords key)
        {
            return tiles.Contains(key);
        }

        /// <summary>
        /// Try to get the value at the specified coordinates and return whether it was successful or not.
        /// </summary>
        public bool TryGetValue(HexCoords index, out HexTile tile)
        {
            if (tiles.Contains(index))
            {
                tile = (HexTile)tiles[index];
                return true;
            }
            tile = null;
            return false;
        }

        public IEnumerator<HexTile> GetEnumerator()
        {
            foreach (var tile in tiles.Values)
            {
                yield return (HexTile)tile;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
