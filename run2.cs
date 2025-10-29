using System;
using System.Collections.Generic;
using System.Linq;

class Program
{
    private static Dictionary<char,int> BfsDistances(Dictionary<char, HashSet<char>> adj,
        Dictionary<(char,char), int> edgeIndex,
        ulong mask,
        char start)
    {
        var dist = new Dictionary<char,int>();
        var q = new Queue<char>();
        dist[start] = 0;
        q.Enqueue(start);

        while (q.Count > 0)
        {
            var u = q.Dequeue();
            if (!adj.TryGetValue(u, out var neighs)) 
                continue;
            
            foreach (var v in neighs)
            {
                if (!EdgePresent(edgeIndex, mask, u, v))
                    continue;

                if (dist.ContainsKey(v)) 
                    continue;
                
                dist[v] = dist[u] + 1;
                q.Enqueue(v);
            }
        }

        return dist;
    }

    private static bool EdgePresent(Dictionary<(char,char), int> edgeIndex, ulong mask, char u, char v)
    {
        if (edgeIndex.TryGetValue((u, v), out var idx) || edgeIndex.TryGetValue((v, u), out idx))
            return ((mask >> idx) & 1UL) == 1UL;

        return true;
    }

    private static bool Isolate(int virusIndex,
                         ulong mask,
                         Dictionary<char, HashSet<char>> adj,
                         Dictionary<(char,char), int> edgeIndex,
                         List<(char gateway, char node)> indexedEdges,
                         Dictionary<char, int> nodeToIndex,
                         List<char> indexToNode,
                         List<string> result,
                         HashSet<ulong> memo)
    {
        var key = ((ulong)virusIndex << indexedEdges.Count) | mask;
        if (!memo.Add(key)) 
            return false;

        var virus = indexToNode[virusIndex];
        var distFromVirus = BfsDistances(adj, edgeIndex, mask, virus);
        
        var reachableGateways = distFromVirus.Keys.Where(char.IsUpper).ToList();
        if (reachableGateways.Count == 0)
            return true;
        
        var candidates = new List<(char g, char n, int idx)>();
        for (var i = 0; i < indexedEdges.Count; i++)
        {
            if (((mask >> i) & 1UL) == 0UL) 
                continue;
            
            var (g, n) = indexedEdges[i];
            candidates.Add((g, n, i));
        }
        
        candidates.Sort((a,b) =>
        {
            var c = a.g.CompareTo(b.g);
            return c != 0 ? c : a.n.CompareTo(b.n);
        });
        
        foreach (var candidate in candidates)
        {
            var newMask = mask & ~(1UL << candidate.idx);
            
            var distAfter = BfsDistances(adj, edgeIndex, newMask, virus);
            var reachableAfter = distAfter.Keys.Where(char.IsUpper).ToList();
            if (reachableAfter.Count == 0)
            {
                result.Add($"{candidate.g}-{candidate.n}");
                return true;
            }
            
            var minDist = reachableAfter.Min(gw => distAfter[gw]);
            var targetGw = reachableAfter.Where(gw => distAfter[gw] == minDist).OrderBy(x => x).First();
            
            var distToTarget = BfsDistances(adj, edgeIndex, newMask, targetGw);
            if (!distToTarget.TryGetValue(virus, out var dVirus))
                continue;
            
            var nextCandidates = new List<char>();
            if (adj.TryGetValue(virus, out var neighs))
            {
                foreach (var nb in neighs)
                {
                    if (!EdgePresent(edgeIndex, newMask, virus, nb)) 
                        continue;
                    
                    if (distToTarget.TryGetValue(nb, out var value) && value == dVirus - 1)
                        nextCandidates.Add(nb);
                }
            }

            if (nextCandidates.Count == 0)
                continue;

            nextCandidates.Sort();
            var nextNode = nextCandidates[0];

            if (char.IsUpper(nextNode))
                continue;
            
            result.Add($"{candidate.g}-{candidate.n}");
            var nextIdx = nodeToIndex[nextNode];
            if (Isolate(nextIdx, newMask, adj, edgeIndex, indexedEdges, nodeToIndex, indexToNode, result, memo)) 
                return true;
            
            result.RemoveAt(result.Count - 1);
        }

        return false;
    }

    private static List<string> Solve(List<(string, string)> edgesInput)
    {
        var adj = new Dictionary<char, HashSet<char>>();
        var edgeIndex = new Dictionary<(char,char), int>();
        var indexedEdges = new List<(char gateway, char node)>();
        foreach (var (su, sv) in edgesInput)
        {
            var u = su[0];
            var v = sv[0];
            
            if (!adj.ContainsKey(u)) 
                adj[u] = [];
            
            if (!adj.ContainsKey(v)) 
                adj[v] = [];
            
            adj[u].Add(v);
            adj[v].Add(u);
            
            if (char.IsUpper(u) && !char.IsUpper(v))
            {
                edgeIndex[(u, v)] = indexedEdges.Count;
                indexedEdges.Add((u, v));
            }
            else if (!char.IsUpper(u) && char.IsUpper(v))
            {
                edgeIndex[(v, u)] = indexedEdges.Count;
                indexedEdges.Add((v, u));
            }
        }
        
        var fullMask = indexedEdges.Count >= 64 ? ulong.MaxValue : (1UL << indexedEdges.Count) - 1UL;
        
        var nodeToIndex = new Dictionary<char,int>();
        var indexToNode = new List<char>();
        var nodesSorted = adj.Keys.OrderBy(x => x).ToList();
        for (var i = 0; i < nodesSorted.Count; i++)
        {
            nodeToIndex[nodesSorted[i]] = i;
            indexToNode.Add(nodesSorted[i]);
        }
        
        var result = new List<string>();
        var memo = new HashSet<ulong>();
        Isolate(nodeToIndex['a'], fullMask, adj, edgeIndex, indexedEdges, nodeToIndex, indexToNode, result, memo);

        return result;
    }

    private static void Main()
    {
        var edges = new List<(string, string)>();
        while (Console.ReadLine() is { } line)
        {
            line = line.TrimStart('\uFEFF', '\uFFFE').Trim();
            if (line.Length == 0) 
                continue;
            
            var parts = line.Split('-');
            if (parts.Length == 2)
                edges.Add((parts[0], parts[1]));
        }

        var res = Solve(edges);
        foreach (var r in res) 
            Console.WriteLine(r);
    }
}
