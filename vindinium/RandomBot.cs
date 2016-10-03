using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace vindinium
{
    class RandomBot
    {
        ServerStuff serverStuff;
        int hx, hy, maxX, maxY, boardWidth, boardHeight;
        string lastDir = "";
        int[][] visits;
        public RandomBot(ServerStuff serverStuff)
        {
            this.serverStuff = serverStuff;
        }

        //starts everything
        public void run()
        {
            Console.Out.WriteLine("random bot running");

            serverStuff.createGame();

            if (serverStuff.errored == false)
            {
                //opens up a webpage so you can view the game, doing it async so we dont time out
                new Thread(delegate()
                {
                    System.Diagnostics.Process.Start(serverStuff.viewURL);
                }).Start();
            }

            boardWidth = serverStuff.board.Length;
            boardHeight = serverStuff.board[0].Length;
            maxX = boardWidth - 1;
            maxY = boardHeight - 1;

            Random random = new Random();
            List<string> unblockedDirs;
            List<Tile> unblockedTiles;
            string chosenDir = "";


            while (serverStuff.finished == false && serverStuff.errored == false)
            {
                hy = serverStuff.myHero.pos.x;
                hx = serverStuff.myHero.pos.y;
                B.Tiles[hx][hy].Visits++;
                unblockedDirs = getUnblockedDirections();
                unblockedTiles = getUnblockedTiles();
                if(unblockedTiles.Count == 0)
                {
                    chosenDir = Direction.North;
                }
                else if(unblockedTiles.Count == 1)
                {
                    chosenDir = TileToDir(unblockedTiles[0]);
                }
                else
                {
                    unblockedTiles = unblockedTiles.OrderBy(x => x.Visits).ToList();
                    unblockedTiles = unblockedTiles.Where(x => x.Visits == unblockedTiles[0].Visits).ToList();
                    chosenDir = TileToDir(unblockedTiles[random.Next(0, unblockedTiles.Count)]);
                }
                lastDir = chosenDir;
                
                serverStuff.moveHero(chosenDir);
                Console.Out.WriteLine("completed turn " + serverStuff.currentTurn);
            }

            if (serverStuff.errored)
            {
                Console.Out.WriteLine("error: " + serverStuff.errorText);
            }

            Console.Out.WriteLine("random bot finished");
        }

        public bool isBlocked(int x, int y)
        {
            var blocked = serverStuff.board[x][y] == TileType.IMPASSABLE_WOOD ||
                serverStuff.board[x][y] == TileType.HERO_1 ||
                serverStuff.board[x][y] == TileType.HERO_2 ||
                serverStuff.board[x][y] == TileType.HERO_3 ||
                serverStuff.board[x][y] == TileType.HERO_4 ||
                serverStuff.board[x][y] == TileType.GOLD_MINE_1;
            blocked = blocked || (serverStuff.board[x][y] == TileType.TAVERN && serverStuff.myHero.life > 50);
            return blocked;
        }

        public List<string> getUnblockedDirections()
        {
            List<string> returnList = new List<string>();
            if (hx > 0 && !isBlocked(hx - 1, hy))
                returnList.Add(Direction.West);
            if (hx < maxX && !isBlocked(hx + 1, hy))
                returnList.Add(Direction.East);
            if (hy > 0 && !isBlocked(hx, hy - 1))
                returnList.Add(Direction.North);
            if (hy < maxY && !isBlocked(hx, hy + 1))
                returnList.Add(Direction.South);
            return returnList;
        }

        public List<Tile> getUnblockedTiles()
        {
            List<Tile> returnList = new List<Tile>();
            if (hx > 0 && !isBlocked(hx - 1, hy))
                returnList.Add(B.Tiles[hx - 1][hy]);
            if (hx < maxX && !isBlocked(hx + 1, hy))
                returnList.Add(B.Tiles[hx + 1][hy]);
            if (hy > 0 && !isBlocked(hx, hy - 1))
                returnList.Add(B.Tiles[hx][hy - 1]);
            if (hy < maxY && !isBlocked(hx, hy + 1))
                returnList.Add(B.Tiles[hx][hy + 1]);
            return returnList;
        }

        public string TileToDir (Tile tile)
        {
            if(hx == tile.X)
            {
                if(hy < tile.Y)
                {
                    return Direction.South;
                }
                else
                {
                    return Direction.North;
                }
            }
            else
            {
                if (hx < tile.X)
                {
                    return Direction.East;
                }
                else
                {
                    return Direction.West;
                }
            }
        }
        public Tile DirToTile (string dir)
        {
            if (hx > 0 && dir == Direction.West)
                return B.Tiles[hx-1][hy];
            if (hx < maxX && dir == Direction.East)
                return B.Tiles[hx + 1][hy];
            if (hy > 0 && dir == Direction.North)
                return B.Tiles[hx][hy-1];
            if (hy < maxY && dir == Direction.South)
                return B.Tiles[hx][hy+1];
            return null;
        }
    }
}
