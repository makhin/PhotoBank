using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace PhotoBank.Services.Enrichment;

internal static class EnricherDependencyResolver
{
    public static Type[] Sort(IReadOnlyList<Type> types, IServiceProvider provider)
    {
        var byName = types
            .GroupBy(t => t.Name)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        static IEnumerable<Type> GetPropertyDeps(Type t, object? instance, IReadOnlyDictionary<string, Type> map)
        {
            var prop = t.GetProperty("Dependencies", BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy | BindingFlags.Static);
            if (prop == null) return Array.Empty<Type>();

            object? value = prop.GetMethod?.IsStatic == true
                ? prop.GetValue(null)
                : instance != null
                    ? prop.GetValue(instance)
                    : null;

            if (value is IEnumerable<Type> typeEnum)
                return typeEnum;

            if (value is IEnumerable<string> nameEnum)
            {
                var result = new List<Type>();
                foreach (var name in nameEnum)
                {
                    if (!map.TryGetValue(name, out var depType))
                        throw new InvalidOperationException($"Enricher {t.Name} declares unknown dependency by name: '{name}'.");
                    result.Add(depType);
                }
                return result;
            }

            return Array.Empty<Type>();
        }

        var edges = new Dictionary<Type, HashSet<Type>>();
        var indegree = new Dictionary<Type, int>();

        foreach (var t in types)
        {
            edges[t] = new HashSet<Type>();
            indegree[t] = 0;
        }

        var tempInstances = new Dictionary<Type, object?>();
        foreach (var t in types)
        {
            object? instance = null;
            try { instance = ActivatorUtilities.CreateInstance(provider, t); } catch { }
            tempInstances[t] = instance;
        }

        foreach (var t in types)
        {
            var deps = new HashSet<Type>();
            foreach (var d in GetPropertyDeps(t, tempInstances[t], byName)) deps.Add(d);

            foreach (var d in deps)
            {
                if (!edges.ContainsKey(d))
                    throw new InvalidOperationException($"Enricher {t.Name} depends on {d.Name}, which is not registered.");
                if (d == t)
                    throw new InvalidOperationException($"Enricher {t.Name} cannot depend on itself.");
                if (edges[d].Add(t))
                    indegree[t]++;
            }
        }

        var queue = new Queue<Type>(indegree.Where(kv => kv.Value == 0).Select(kv => kv.Key));
        var ordered = new List<Type>(types.Count);

        while (queue.Count > 0)
        {
            var n = queue.Dequeue();
            ordered.Add(n);

            foreach (var m in edges[n])
            {
                indegree[m]--;
                if (indegree[m] == 0)
                    queue.Enqueue(m);
            }
        }

        if (ordered.Count != types.Count)
        {
            var cycle = FindCycle(edges);
            var path = string.Join(" -> ", cycle.Select(t => t.Name));
            throw new InvalidOperationException($"A dependency cycle was detected among enrichers: {path}");
        }

        return ordered.ToArray();
    }

    private static IReadOnlyList<Type> FindCycle(Dictionary<Type, HashSet<Type>> edges)
    {
        var color = new Dictionary<Type, int>();
        var stack = new Stack<Type>();
        var cycle = new List<Type>();

        bool Dfs(Type v)
        {
            color[v] = 1;
            stack.Push(v);

            foreach (var u in edges[v])
            {
                if (!color.TryGetValue(u, out var c)) c = 0;
                if (c == 0)
                {
                    if (Dfs(u)) return true;
                }
                else if (c == 1)
                {
                    var arr = stack.ToArray();
                    Array.Reverse(arr);
                    var idx = Array.IndexOf(arr, u);
                    cycle.AddRange(arr[idx..]);
                    cycle.Add(u);
                    return true;
                }
            }

            stack.Pop();
            color[v] = 2;
            return false;
        }

        foreach (var v in edges.Keys)
        {
            if (!color.TryGetValue(v, out var c) || c == 0)
            {
                if (Dfs(v)) break;
            }
        }
        return cycle;
    }
}

