using System;
using System.Collections.Generic;

class Program
{
    private const int HallLength = 11;
    private const int RoomCount = 4;
    
    private static readonly int[] Cost = [1, 10, 100, 1000];
    private static readonly int[,] DistFromHallToRooms = GetDistFromHallToRooms();
    
    private static readonly char[] DigitToChar = ['.', 'A', 'B', 'C', 'D'];
    private static readonly byte[] CharToDigit = new byte[128];

    static Program()
    {
        CharToDigit['A'] = 1;
        CharToDigit['B'] = 2;
        CharToDigit['C'] = 3;
        CharToDigit['D'] = 4;
    }

    private static int Solve(List<string> lines)
    {
        var rooms = ParseInput(lines);
        var hall = new char[HallLength];
        var depth = rooms[0].Length;
        var start = PackToUlong(hall, rooms, depth);
        
        return AStar(start, depth);
    }

    private static void Main()
    {
        var lines = new List<string>();

        while (Console.ReadLine() is { } line)
            lines.Add(line);

        var result = Solve(lines);
        Console.WriteLine(result);
    }

    private static char[][] ParseInput(List<string> lines)
    {
        var roomDepth = lines.Count - 3;
        var rooms = new char[RoomCount][];

        for (var room = 0; room < RoomCount; room++)
        {
            rooms[room] = new char[roomDepth];
            var roomPos = 3 + room * 2;
            
            for (var depth = 0; depth < roomDepth; depth++)
                rooms[room][depth] = lines[2 + depth][roomPos];
        }

        return rooms;
    }

    private static ulong PackToUlong(char[] hall, char[][] rooms, int depth)
    {
        var v = 0UL;
        for (var i = 0; i < HallLength; i++)
        {
            var dig = CharToDigit[hall[i]];
            v = v * 5 + dig;
        }

        for (var r = 0; r < RoomCount; r++)
        for (var d = 0; d < depth; d++)
        {
            var dig = CharToDigit[rooms[r][d]];
            v = v * 5 + dig;
        }
        
        return v;
    }

    private static void UnpackFromUlong(ulong v, int depth, char[] hallOut, char[][] roomsOut)
    {
        var total = HallLength + RoomCount * depth;
        for (var i = total - 1; i >= 0; i--)
        {
            var dig = v % 5UL;
            if (i < HallLength)
                hallOut[i] = DigitToChar[dig];
            else
            {
                var idx = i - HallLength;
                roomsOut[idx / depth][idx % depth] = DigitToChar[dig];
            }
            
            v /= 5UL;
        }
            
    }

    private static int[,] GetDistFromHallToRooms()
    {
        var distFromHallToRooms = new int[HallLength, RoomCount];
        for (var h = 0; h < HallLength; h++)
        for (var r = 0; r < RoomCount; r++)
            distFromHallToRooms[h, r] = Math.Abs(h - (r + 1) * 2);
        
        return distFromHallToRooms;
    }
    
    private static bool IsGoal(ulong state, int depth)
    {
        var hall = new char[HallLength];
        var rooms = new char[4][];
        for (var r = 0; r < 4; r++) 
            rooms[r] = new char[depth];
        UnpackFromUlong(state, depth, hall, rooms);
        
        for (var i = 0; i < HallLength; i++) 
            if (hall[i] != '.') 
                return false;
        
        for (var r = 0; r < 4; r++)
        for (var d = 0; d < depth; d++) 
            if (rooms[r][d] == '.' || rooms[r][d] - 'A' != r)
                return false;
        
        return true;
    }
    
    private static int AStar(ulong start, int depth)
    {
        var open = new PriorityQueue<ulong, int>();
        var stateFullCost = new Dictionary<ulong, int>();
        
        var hallBuf = new char[HallLength];
        var roomsBuf = new char[RoomCount][];
        for (var r = 0; r < 4; r++)
            roomsBuf[r] = new char[depth];
        
        var heuristic = Heuristic(start, depth);
        open.Enqueue(start, heuristic);
        stateFullCost[start] = 0;

        while (open.Count > 0)
        {
            var state = open.Dequeue();
            if (!stateFullCost.TryGetValue(state, out var cost)) 
                continue;
            
            if (IsGoal(state, depth)) 
                return cost;

            foreach (var (newState, moveCost) in GenerateNeighbors(state, depth, hallBuf, roomsBuf))
            {
                var newCost = cost + moveCost;
                if (stateFullCost.TryGetValue(newState, out var prev) && newCost >= prev) 
                    continue;
                
                stateFullCost[newState] = newCost;
                heuristic = Heuristic(newState, depth);
                open.Enqueue(newState, newCost + heuristic);
            }
        }
        
        return -1;
    }

    private static int Heuristic(ulong state, int depth)
    {
        var hall = new char[HallLength];
        var rooms = new char[RoomCount][];
        for (var r = 0; r < RoomCount; r++) 
            rooms[r] = new char[depth];
        
        UnpackFromUlong(state, depth, hall, rooms);
        
        var fullEnergy = 0;
        for (var i = 0; i < HallLength; i++)
        {
            if (hall[i] == '.') 
                continue;
            
            var letter = hall[i] - 'A';
            var steps = DistFromHallToRooms[i, letter] + 1;
            fullEnergy += steps * Cost[letter];
        }

        for (var r = 0; r < 4; r++)
        {
            var room = rooms[r];
            for (var i = 0; i < room.Length; i++)
            {
                if (room[i] == '.')
                    continue;
                
                var letter = room[i] - 'A';
                if (letter == r)
                {
                    var belowOk = true;
                    for (var j = i; j < room.Length; j++)
                        if (room[j] != room[i])
                        {
                            belowOk = false; 
                            break;
                        }
                    
                    if (belowOk) 
                        continue;
                }

                var steps = i + Math.Abs((r - letter) * 2) + 2;
                fullEnergy += steps * Cost[letter];
            }
        }
        
        return fullEnergy;
    }

    private static IEnumerable<(ulong newState, int moveCost)> GenerateNeighbors(ulong packed, int depth, char[] hallBuf, char[][] roomsBuf)
    {
        UnpackFromUlong(packed, depth, hallBuf, roomsBuf);
        
        for (var h = 0; h < HallLength; h++)
        {
            if (hallBuf[h] == '.') 
                continue;
            
            var letter = hallBuf[h] - 'A';
            var room = roomsBuf[letter];
            
            var foreign = false;
            for (var k = 0; k < depth; k++)
                if (room[k] != '.' && room[k] != hallBuf[h])
                {
                    foreign = true; 
                    break;
                }
            
            if (foreign) 
                continue;
            
            var door = (1 + letter) * 2;
            var step = h < door ? 1 : -1;
            var cur = h + step;
            
            var blocked = false;
            while (cur != door + step)
            {
                if (hallBuf[cur] != '.')
                {
                    blocked = true; 
                    break;
                }
                cur += step;
            }
            
            if (blocked) 
                continue;
            
            var place = -1;
            for (var d = depth - 1; d >= 0; d--)
                if (room[d] == '.')
                {
                    place = d; 
                    break;
                }
            
            if (place == -1) 
                continue;
            
            var stepsNeeded = Math.Abs(h - door) + place + 1;
            var cost = stepsNeeded * Cost[letter];
            
            var newHall = new char[HallLength];
            for (var i = 0; i < HallLength; i++) 
                newHall[i] = hallBuf[i];
            
            var newRooms = new char[4][];
            for (var r = 0; r < 4; r++)
            {
                newRooms[r] = new char[depth];
                for (var d = 0; d < depth; d++) 
                    newRooms[r][d] = roomsBuf[r][d];
            }
            newHall[h] = '.';
            newRooms[letter][place] = hallBuf[h];
            yield return (PackToUlong(newHall, newRooms, depth), cost);
        }
        
        for (var r = 0; r < 4; r++)
        {
            var room = roomsBuf[r];
            var top = -1;
            for (var i = 0; i < depth; i++) 
                if (room[i] != '.') 
                { 
                    top = i; 
                    break; 
                }
            
            if (top == -1) 
                continue;
            
            var letter = room[top] - 'A';
            if (letter == r)
            {
                var ok = true;
                for (var j = top; j < depth; j++)
                    if (room[j] != room[top])
                    {
                        ok = false;
                        break;
                    }

                if (ok)
                    continue;
            }

            var door = (r + 1) * 2;
            for (var h = door - 1; h >= 0; h--)
            {
                if (hallBuf[h] != '.') 
                    break;
                
                if (h == 2 || h == 4 || h == 6 || h == 8) 
                    continue;
                
                var stepsNeeded = top + 1 + (door - h);
                var cost = stepsNeeded * Cost[letter];

                var newHall = new char[HallLength];
                for (var i = 0; i < HallLength; i++) 
                    newHall[i] = hallBuf[i];
                
                var newRooms = new char[4][];
                for (var rr = 0; rr < 4; rr++)
                {
                    newRooms[rr] = new char[depth];
                    for (var d = 0; d < depth; d++) 
                        newRooms[rr][d] = roomsBuf[rr][d];
                }
                
                newHall[h] = room[top];
                newRooms[r][top] = '.';
                
                yield return (PackToUlong(newHall, newRooms, depth), cost);
            }
            
            for (var h = door + 1; h < HallLength; h++)
            {
                if (hallBuf[h] != '.') 
                    break;
                
                if (h == 2 || h == 4 || h == 6 || h == 8) 
                    continue;
                
                var stepsNeeded = top + 1 + (h - door);
                var cost = stepsNeeded * Cost[letter];

                var newHall = new char[HallLength];
                for (var i = 0; i < HallLength; i++) 
                    newHall[i] = hallBuf[i];
                
                var newRooms = new char[4][];
                for (var rr = 0; rr < 4; rr++)
                {
                    newRooms[rr] = new char[depth];
                    for (var d = 0; d < depth; d++) 
                        newRooms[rr][d] = roomsBuf[rr][d];
                }
                
                newHall[h] = room[top];
                newRooms[r][top] = '.';
                yield return (PackToUlong(newHall, newRooms, depth), cost);
            }
        }
    }
}