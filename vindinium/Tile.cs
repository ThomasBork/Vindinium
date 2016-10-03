using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace vindinium
{
    public class Tile
    {
        public TileType Type { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public int Visits { get; set; }
        public Tile (TileType typ, int x, int y)
        {
            this.Type = typ;
            this.X = x;
            this.Y = y;
        }
    }
}
