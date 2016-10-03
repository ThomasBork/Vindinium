using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace vindinium
{
    class FarmBot
    {
        ServerStuff serverStuff;
        int hx, hy, maxX, maxY, boardWidth, boardHeight;
        string lastDir = "";
        int[][] visits;
        TileType myMine, myHero;
        Random random;
        int lastHP = 100;


        public FarmBot(ServerStuff serverStuff)
        {
            this.serverStuff = serverStuff;
        }

        //starts everything
        public void run()
        {
            Console.Out.WriteLine("farm bot running");

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
            switch(serverStuff.myHero.id)
            {
                case 1:
                    myMine = TileType.GOLD_MINE_1;
                    myHero = TileType.HERO_1;
                    break;
                case 2:
                    myMine = TileType.GOLD_MINE_2;
                    myHero = TileType.HERO_2;
                    break;
                case 3:
                    myMine = TileType.GOLD_MINE_3;
                    myHero = TileType.HERO_3;
                    break;
                case 4:
                    myMine = TileType.GOLD_MINE_4;
                    myHero = TileType.HERO_4;
                    break;
                default:
                    break;
            }

            random = new Random();
            List<Tile> desirableTiles;
            string chosenDir = "";


            while (serverStuff.finished == false && serverStuff.errored == false)
            {
                hy = serverStuff.myHero.pos.x;
                hx = serverStuff.myHero.pos.y;

                var shortestPaths = GetShortestPaths();
                #region output shortest path
                //Console.Clear();
                //for (var y = 0; y < shortestPaths[0].Length; y++)
                //{
                //    for (var x = 0; x < shortestPaths.Length; x++)
                //    {
                //        if (shortestPaths[x][y] == null)
                //        {
                //            Console.Write("--");
                //        }
                //        else
                //        {
                //            if (shortestPaths[x][y].Count > 9)
                //            {
                //                Console.Write(shortestPaths[x][y].Count);
                //            }
                //            else
                //            {
                //                Console.Write("0" + shortestPaths[x][y].Count);
                //            }
                //        }
                //    }
                //    Console.WriteLine("");
                //}
                #endregion output shortest path

                if (JustDied())
                {
                    ResetVisits();
                }

                B.Tiles[hx][hy].Visits++;
                
                desirableTiles = GetUnblockedTiles();

                if(serverStuff.myHero.life < 20)
                {
                    desirableTiles = desirableTiles.Where(x => !IsNewMine(x)).ToList();
                }

                if(desirableTiles.Any(x=> IsNewMine(x))){
                    desirableTiles = desirableTiles.Where(x => IsNewMine(x)).ToList();
                }
                else if(serverStuff.myHero.life < 90 && desirableTiles.Any(x=>x.Type == TileType.TAVERN))
                {
                    desirableTiles = desirableTiles.Where(x => x.Type == TileType.TAVERN).ToList();
                }
                else
                {
                }

                var tavernPath = GetPathToClosestTavern(shortestPaths);
                var found = false;
                if(tavernPath != null && (serverStuff.myHero.life < 40 || (serverStuff.myHero.life < 90 && tavernPath.Count < 3)))
                {
                    chosenDir = TileToDir(tavernPath[0]);
                    found = true;
                }
                else if (serverStuff.myHero.life > 20)
                {
                    var weakHeroes = serverStuff.heroes.Where(x => x.id != serverStuff.myHero.id && x.life < serverStuff.myHero.life);
                    if(weakHeroes.Any())
                    {
                        List<Tile> shortestHeroPath = null;
                        foreach(var weakling in weakHeroes)
                        {
                            var pathToWeakling = shortestPaths[weakling.pos.x][weakling.pos.y];
                            if (pathToWeakling != null)
                            {
                                if(shortestHeroPath == null)
                                {
                                    shortestHeroPath = pathToWeakling;
                                }
                                else
                                {
                                    if(pathToWeakling.Count < shortestHeroPath.Count)
                                    {
                                        shortestHeroPath = pathToWeakling;
                                    }
                                }
                            }
                        }
                        
                        if (shortestHeroPath != null && shortestHeroPath.Count < 3)
                        {
                            chosenDir = TileToDir(shortestHeroPath[0]);
                            found = true;
                        }
                    }
                    
                    if(!found)
                    {
                        var minePath = GetPathToClosestMine(shortestPaths);
                        if (minePath != null)
                        {
                            chosenDir = TileToDir(minePath[0]);
                            found = true;
                        }
                    }
                }
                else
                {
                }

                if (!found) {
                    chosenDir = ChooseRandomLeastVisitedDir(desirableTiles);
                }

                lastHP = serverStuff.myHero.life;

                Console.WriteLine("From (" + hx + ","+ hy + ") " + chosenDir);
                serverStuff.moveHero(chosenDir);
                Console.Out.WriteLine("completed turn " + serverStuff.currentTurn);
            }

            if (serverStuff.errored)
            {
                Console.Out.WriteLine("error: " + serverStuff.errorText);
            }

            Console.Out.WriteLine("random bot finished");
        }

        private string PathToString(List<Tile> path)
        {
            var s = "";
            foreach(var tile in path)
            {
                s += "(" + tile.X + "," + tile.Y + ")->";
            }
            return s;
        }

        private List<Tile> GetPathToClosestTavern(List<Tile>[][] shortestPaths)
        {
            return GetPathToClosestTile(shortestPaths, new List<TileType> { TileType.TAVERN });
        }
        private List<Tile> GetPathToClosestMine(List<Tile>[][] shortestPaths)
        {
            var mineTypes = new List<TileType> { TileType.GOLD_MINE_1, TileType.GOLD_MINE_2, TileType.GOLD_MINE_3, TileType.GOLD_MINE_4, TileType.GOLD_MINE_NEUTRAL };
            mineTypes.Remove(myMine);
            return GetPathToClosestTile(shortestPaths, mineTypes);
        }
        private List<Tile> GetPathToClosestHero(List<Tile>[][] shortestPaths)
        {
            var mineTypes = new List<TileType> { TileType.HERO_1, TileType.HERO_2, TileType.HERO_3, TileType.HERO_4 };
            mineTypes.Remove(myHero);
            return GetPathToClosestTile(shortestPaths, mineTypes);
        }

        private List<Tile> GetPathToClosestTile(List<Tile>[][] shortestPaths, List<TileType> allowedTypes)
        {
            var tiles = new List<Tile>();
            foreach(var typ in allowedTypes)
            {
                tiles.AddRange(GetTilesWithType(typ));
            }
            List<Tile> bestPath = shortestPaths[tiles[0].X][tiles[0].Y];
            foreach (var tile in tiles)
            {
                if(bestPath == null)
                {
                    bestPath = shortestPaths[tile.X][tile.Y];
                }
                else if (shortestPaths[tile.X][tile.Y] != null && shortestPaths[tile.X][tile.Y].Count < bestPath.Count)
                {
                    bestPath = shortestPaths[tile.X][tile.Y];
                }
            }
            return bestPath;
        }

        private List<Tile> GetTilesWithType(TileType typ)
        {
            List<Tile> result = new List<Tile>();
            for(var x = 0; x<boardWidth; x++)
            {
                for(var y = 0; y<boardHeight; y++)
                {
                    if(B.Tiles[x][y].Type == typ)
                    {
                        result.Add(B.Tiles[x][y]);
                    }
                }
            }
            return result;
        }

        private List<Tile>[][] GetShortestPaths()
        {
            List<Tile>[][] paths = new List<Tile>[boardWidth][];
            for(var x = 0; x<boardWidth; x++)
            {
                paths[x] = new List<Tile>[boardHeight];
            }

            var pathsToFollow = new List<List<Tile>>();

            GetShortestPathsInner(paths, pathsToFollow);

            while(pathsToFollow.Count > 0)
            {
                List<Tile> shortestPath = pathsToFollow[0];
                for(int i = 0; i<pathsToFollow.Count; i++)
                {
                    if(shortestPath.Count > pathsToFollow[i].Count)
                    {
                        shortestPath = pathsToFollow[i];
                    }
                }
                pathsToFollow.Remove(shortestPath);
                GetShortestPathsInner(paths, pathsToFollow, shortestPath);
            }

            return paths;
        }

        private void GetShortestPathsInner(List<Tile>[][] paths, List<List<Tile>> pathsToFollow, List<Tile> currentPath = null)
        {
            if(currentPath == null)
            {
                foreach (var tile in GetAdjacentTiles(B.Tiles[hx][hy]))
                {
                    if (!IsBlocked(tile))
                    {
                        var newPath = new List<Tile> { tile };
                        pathsToFollow.Add(newPath);
                        paths[tile.X][tile.Y] = newPath;
                    }
                }
            }
            else
            {
                foreach (var tile in GetAdjacentTiles(currentPath.Last()))
                {
                    if (!IsBlocked(tile))
                    {
                        var newPath = Clone(currentPath);
                        newPath.Add(tile);
                        var path = paths[tile.X][tile.Y];
                        if (path == null || path.Count > newPath.Count)
                        {
                            paths[tile.X][tile.Y] = newPath;
                            pathsToFollow.Add(newPath);
                        }
                    }
                }
            }
        }

        private List<Tile> Clone(List<Tile> tiles)
        {
            var newTiles = new List<Tile>();
            foreach(var tile in tiles)
            {
                newTiles.Add(tile);
            }
            return newTiles;
        }

        private Tile GetCurrentTile()
        {
            return B.Tiles[hx][hy];
        }

        private List<Tile> GetAdjacentTiles(Tile tile)
        {
            List<Tile> tiles = new List<Tile>();

            if (tile.X > 0)
                tiles.Add(B.Tiles[tile.X - 1][tile.Y]);
            if (tile.X < maxX)
                tiles.Add(B.Tiles[tile.X + 1][tile.Y]);
            if (tile.Y > 0)
                tiles.Add(B.Tiles[tile.X][tile.Y - 1]);
            if (tile.Y < maxY)
                tiles.Add(B.Tiles[tile.X][tile.Y + 1]);

            return tiles;
        }

        private bool JustDied()
        {
            if (
                serverStuff.myHero.life == 100 && lastHP < 50 &&
                hx == serverStuff.myHero.spawnPos.y && hy == serverStuff.myHero.spawnPos.x)
            {
                return true;
            }
            return false;
        }

        private void ResetVisits()
        {
            for(int x = 0; x<boardWidth; x++)
            {
                for(int y = 0; y<boardHeight; y++)
                {
                    B.Tiles[x][y].Visits = 0;
                }
            }
        }

        private string ChooseRandomLeastVisitedDir(List<Tile> tiles)
        {
            if (tiles.Count == 0)
            {
                return Direction.North;
            }
            else if (tiles.Count == 1)
            {
                return TileToDir(tiles[0]);
            }
            else
            {
                tiles = tiles.OrderBy(x => x.Visits).ToList();
                tiles = tiles.Where(x => x.Visits == tiles[0].Visits).ToList();
                return TileToDir(tiles[random.Next(0, tiles.Count)]);
            }
        }

        private bool IsNewMine (Tile tile)
        {
            return tile.Type != myMine && (
                tile.Type == TileType.GOLD_MINE_1 ||
                tile.Type == TileType.GOLD_MINE_2 ||
                tile.Type == TileType.GOLD_MINE_3 ||
                tile.Type == TileType.GOLD_MINE_4 ||
                tile.Type == TileType.GOLD_MINE_NEUTRAL
            );
        }

        public bool IsBlocked(Tile tile)
        {
            return IsBlocked(tile.X, tile.Y);
        }

        public bool IsBlocked(int x, int y)
        {
            var blocked = serverStuff.board[x][y] == TileType.IMPASSABLE_WOOD ||
                serverStuff.board[x][y] == TileType.HERO_1 ||
                serverStuff.board[x][y] == TileType.HERO_2 ||
                serverStuff.board[x][y] == TileType.HERO_3 ||
                serverStuff.board[x][y] == TileType.HERO_4 ||
                serverStuff.board[x][y] == myMine;
            blocked = blocked || (serverStuff.board[x][y] == TileType.TAVERN && serverStuff.myHero.life > 90);
            blocked = blocked || (serverStuff.myHero.life < 20 &&
                (serverStuff.board[x][y] == TileType.GOLD_MINE_1 ||
                serverStuff.board[x][y] == TileType.GOLD_MINE_2 ||
                serverStuff.board[x][y] == TileType.GOLD_MINE_3 ||
                serverStuff.board[x][y] == TileType.GOLD_MINE_4));
            return blocked;
        }

        public List<string> GetUnblockedDirections()
        {
            List<string> returnList = new List<string>();
            if (hx > 0 && !IsBlocked(hx - 1, hy))
                returnList.Add(Direction.West);
            if (hx < maxX && !IsBlocked(hx + 1, hy))
                returnList.Add(Direction.East);
            if (hy > 0 && !IsBlocked(hx, hy - 1))
                returnList.Add(Direction.North);
            if (hy < maxY && !IsBlocked(hx, hy + 1))
                returnList.Add(Direction.South);
            return returnList;
        }

        public List<Tile> GetUnblockedTiles()
        {
            List<Tile> returnList = new List<Tile>();
            if (hx > 0 && !IsBlocked(hx - 1, hy))
                returnList.Add(B.Tiles[hx - 1][hy]);
            if (hx < maxX && !IsBlocked(hx + 1, hy))
                returnList.Add(B.Tiles[hx + 1][hy]);
            if (hy > 0 && !IsBlocked(hx, hy - 1))
                returnList.Add(B.Tiles[hx][hy - 1]);
            if (hy < maxY && !IsBlocked(hx, hy + 1))
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
