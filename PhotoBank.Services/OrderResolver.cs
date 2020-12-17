using System;
using System.Collections.Generic;
using System.Linq;

namespace PhotoBank.Services
{
    public interface IOrderResolver<T> where T : IOrderDependant
    {
        IEnumerable<T> Resolve(IEnumerable<T> collection);
    }

    public class OrderResolver<T> : IOrderResolver<T> where T : IOrderDependant
    {
        private readonly Dictionary<T, List<T>> _elementDependents = new Dictionary<T, List<T>>();
        private readonly Stack<T> _elementStack = new Stack<T>();

        public IEnumerable<T> Resolve(IEnumerable<T> collection)
        {
            var orderDependents = collection.ToList();
            if (MutualDependenciesExists(orderDependents))
            {
                throw new InvalidOperationException("There must be no mutual dependencies between elements");
            }

            InitializeDependenciesOrder(orderDependents);
            return _elementStack;
        }

        private void InitializeDependenciesOrder(List<T> orderDependents)
        {
            InitializeDependenciesDictionary(orderDependents);

            while (_elementDependents.Any())
            {
                List<T> leafs = GetLeafs();
                leafs.ForEach(leaf =>
                {
                    _elementStack.Push(leaf);
                    CutLeaf(leaf);
                });
            }
        }

        private void CutLeaf(T leaf)
        {
            _elementDependents.Remove(leaf);
            foreach (var d in _elementDependents.Where(e=> e.Value.Contains(leaf)))
            {
                d.Value.Remove(leaf);
            }
        }

        private List<T> GetLeafs()
        {
            return _elementDependents.Where(e => !e.Value.Any()).Select(e => e.Key).ToList();
        }

        private void InitializeDependenciesDictionary(List<T> orderDependents)
        {
            orderDependents.ForEach(element => _elementDependents.Add(element, orderDependents.Where(e => e.Dependencies.Any(d => d == element.GetType())).ToList()));
        }

        private static bool MutualDependenciesExists(IList<T> orderDependents)
        {
            return ((from element1 in orderDependents
                from element2 in orderDependents
                where element1.Dependencies.Contains(element2.GetType()) &&
                      element2.Dependencies.Contains(element1.GetType())
                select element1)).Any();
        }
    }
}
