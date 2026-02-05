using System;
using System.Collections.Generic;
using System.Linq;

public record HexCoordinate(int X, int Y);

public class HexPathfinder
{
    // For POINTY-TOP hexagons i Unity (Odd-R layout)
    // Forskydningen sker på rækkerne (Y), derfor er naboerne afhængige af Y-paritet.
    private static readonly int[][][] NeighborOffsetsOddR = {
        // Lige rækker (Y % 2 == 0)
        new int[][] {
            new[] { 1, 0 },  // Øst
            new[] { 0, 1 },  // Nordøst
            new[] { -1, 1 }, // Nordvest
            new[] { -1, 0 }, // Vest
            new[] { -1, -1 },// Sydvest
            new[] { 0, -1 }  // Sydøst
        },
        // Ulige rækker (Y % 2 == 1)
        new int[][] {
            new[] { 1, 0 },  // Øst
            new[] { 1, 1 },  // Nordøst
            new[] { 0, 1 },  // Nordvest
            new[] { -1, 0 }, // Vest
            new[] { 0, -1 }, // Sydvest
            new[] { 1, -1 }  // Sydøst
        }
    };

    public List<HexCoordinate> FindPath(int startX, int startY, int targetX, int targetY)
    {
        var start = new HexCoordinate(startX, startY);
        var target = new HexCoordinate(targetX, targetY);

        var openSet = new Dictionary<HexCoordinate, float>();
        var gScore = new Dictionary<HexCoordinate, float>();
        var parent = new Dictionary<HexCoordinate, HexCoordinate>();

        openSet[start] = GetHeuristic(start, target);
        gScore[start] = 0;

        while (openSet.Count > 0)
        {
            var current = openSet.OrderBy(kvp => kvp.Value).First().Key;
            openSet.Remove(current);

            if (current.X == target.X && current.Y == target.Y)
            {
                return ReconstructPath(parent, current, start);
            }

            foreach (var neighbor in GetNeighbors(current))
            {
                // Vi antager en kost på 1 pr. felt
                float tentativeGScore = gScore[current] + 1;

                if (!gScore.ContainsKey(neighbor) || tentativeGScore < gScore[neighbor])
                {
                    parent[neighbor] = current;
                    gScore[neighbor] = tentativeGScore;
                    float fScore = tentativeGScore + GetHeuristic(neighbor, target);
                    openSet[neighbor] = fScore;
                }
            }
        }

        return new List<HexCoordinate>();
    }

    private List<HexCoordinate> GetNeighbors(HexCoordinate hex)
    {
        var neighbors = new List<HexCoordinate>();

        // VIGTIGT: I Pointy-top bruger vi Y-aksen til paritet
        int parity = Math.Abs(hex.Y) % 2;
        var offsets = NeighborOffsetsOddR[parity];

        foreach (var offset in offsets)
        {
            neighbors.Add(new HexCoordinate(hex.X + offset[0], hex.Y + offset[1]));
        }
        return neighbors;
    }

    private float GetHeuristic(HexCoordinate a, HexCoordinate b)
    {
        var ac = OffsetToCube(a);
        var bc = OffsetToCube(b);

        return (Math.Abs(ac.q - bc.q) + Math.Abs(ac.r - bc.r) + Math.Abs(ac.s - bc.s)) / 2f;
    }

    private (int q, int r, int s) OffsetToCube(HexCoordinate hex)
    {
        // Konvertering fra Odd-R Offset til Cube koordinater
        // q = x - (y - (y&1)) / 2
        // r = y
        int q = hex.X - (hex.Y - (Math.Abs(hex.Y) % 2)) / 2;
        int r = hex.Y;
        int s = -q - r;
        return (q, r, s);
    }

    private List<HexCoordinate> ReconstructPath(
        Dictionary<HexCoordinate, HexCoordinate> parent,
        HexCoordinate current,
        HexCoordinate start)
    {
        var path = new List<HexCoordinate>();

        while (!(current.X == start.X && current.Y == start.Y))
        {
            path.Add(current);
            if (!parent.ContainsKey(current)) break;
            current = parent[current];
        }

        path.Reverse();
        return path;
    }
}