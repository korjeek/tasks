using System;
using System.Collections.Generic;
using System.Linq;

class Program
{
    private static readonly List<string> Result = [];
    
    private static List<string> Solve(List<(char, char)> edges)
    {
        var graph = new Dictionary<char, HashSet<char>>();
        var gateways = new HashSet<char>();
        foreach (var (u, v) in edges)
        {
            if (char.IsUpper(u))
                gateways.Add(u);
            if (char.IsUpper(v))
                gateways.Add(v);
            
            graph.TryAdd(u, []);
            graph.TryAdd(v, []);
            graph[u].Add(v);
            graph[v].Add(u);
        }
        
        Isolate(graph, gateways, 'a');
        
        var result = Result;
        return result;
    }

    private static void Main()
    {
        var edges = new List<(char, char)>();

        while (Console.ReadLine() is { } line)
        {
            line = line.TrimStart('\uFEFF', '\uFFFE').Trim();
            if (!string.IsNullOrEmpty(line))
            {
                var parts = line.Split('-');
                if (parts.Length == 2)
                {
                    edges.Add((parts[0][0], parts[1][0]));
                }
            }
        }

        var result = Solve(edges);
        foreach (var edge in result)
            Console.WriteLine(edge);
    }
    
    private static void Isolate(
        Dictionary<char, HashSet<char>> graph,
        HashSet<char> gateways,
        char virusPos)
    {
        if (gateways.Count == 0)
            return;
        
        var dist = BfsDistances(graph, virusPos);
        var nearestGateway = NearestVertex(gateways, dist);
        
        var nbToDelete = NearestVertex(graph[nearestGateway], dist);
        graph[nearestGateway].Remove(nbToDelete);
        graph[nbToDelete].Remove(nearestGateway);
        Result.Add($"{nearestGateway}-{nbToDelete}");
        
        if (graph[nearestGateway].Count == 0)
        {
            gateways.Remove(nearestGateway);
            graph.Remove(nearestGateway);
        }
        
        if (gateways.Count == 0)
            return;
        
        dist = BfsDistances(graph, virusPos);
        nearestGateway = NearestVertex(gateways, dist);
        var distToTarget = BfsDistances(graph, nearestGateway);
        
        var next = graph[virusPos].First(nb => distToTarget[nb] == distToTarget[virusPos] - 1);
        
        Isolate(graph, gateways, next);
    }
    
    private static Dictionary<char, int> BfsDistances(Dictionary<char, HashSet<char>> graph, char start)
    {
        var dist = new Dictionary<char, int>();
        var queue = new Queue<char>();
        dist[start] = 0;
        queue.Enqueue(start);
        
        while (queue.Count > 0)
        {
            var u = queue.Dequeue();
            foreach (var w in graph[u].Where(w => !dist.ContainsKey(w)))
            {
                dist[w] = dist[u] + 1;
                queue.Enqueue(w);
            }
        }

        return dist;
    }
    
    private static char NearestVertex(HashSet<char> vertices, Dictionary<char, int> dist)
    {
        var best = int.MaxValue;
        var nearestVertex = '~';
        foreach (var v in vertices)
        {
            if (dist[v] < best || dist[v] == best && v < nearestVertex)
            {
                best = dist[v];
                nearestVertex = v;
            }
        }

        return nearestVertex;
    }
}