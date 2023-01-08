using GameWarriors.DependencyInjection.Abstraction;
using System;
using System.Collections.Generic;

namespace GameWarriors.DependencyInjection.Core
{
    public class DependencyHistory : IDependencyHistory
    {
        private Type _firstType;
        private Dictionary<Type, int> _dependencyTable;

        public DependencyHistory()
        {
            _dependencyTable = new();
        }

        public void AddDependency(Type injectType)
        {
            if (_firstType == null)
            {
                _firstType = injectType;
            }
            _dependencyTable.TryAdd(injectType, 0);
        }

        public void RemoveDependency(Type injectType)
        {
            _dependencyTable.Remove(injectType);
            if (_dependencyTable.Count == 1)
            {
                _dependencyTable.Clear();
                _firstType = null;
            }
        }

        public bool CheckDependencyHistory(Type argType)
        {
            bool hasHistory = _dependencyTable.ContainsKey(argType);
            if (hasHistory)
                throw new CircleDependencyException(_firstType, argType);
            return hasHistory;
        }
    }
}