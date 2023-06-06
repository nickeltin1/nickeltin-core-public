using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Pool;

namespace nickeltin.Core.Editor
{
    internal class SearchWindowNode
    {
        public int Depth
        {
            get
            {
                return Parent != null ? Parent.Depth + 1 : 0;
            }
        }

        public string Name { get; private set; }

        public string DisplayName { get; set; }
        public bool IsEndNode { get; private set; }
        public ISearchWindowEntry Data { get; private set; }
            
        public SearchWindowNode Parent { get; private set; }

        public int DirectChildCount => _nestedNodes.Count;
            
        private readonly Dictionary<string, SearchWindowNode> _nestedNodes;
        

        private SearchWindowNode(string displayName, string name, bool isEndNode, ISearchWindowEntry data)
        {
            _nestedNodes = DictionaryPool<string, SearchWindowNode>.Get();
            this.DisplayName = displayName;
            this.Name = name;
            this.IsEndNode = isEndNode;
            this.Data = data;
        }

        ~SearchWindowNode()
        {
            DictionaryPool<string, SearchWindowNode>.Release(_nestedNodes);
        }

        public static SearchWindowNode GroupNode(string name, string displayName)
        {
            return new SearchWindowNode(displayName, name, false, null);
        }

        public static SearchWindowNode EndNode(ISearchWindowEntry data)
        {
            if (data == null)
            {
                throw new Exception("Data is null");
            }

            return new SearchWindowNode(data.GetPathAlias().LastOrDefault(), 
                data.GetPath().LastOrDefault(), true, data);
        }
            
            
        public void Add(SearchWindowNode node)
        {
            if (IsEndNode)
            {
                Debug.LogError("End node can't have children's");
                return;
            }
            
            node.Parent = this;
            _nestedNodes.Add(node.Name, node);
        }

        public void Remove(SearchWindowNode node)
        {
            _nestedNodes.Remove(node.Name);
        }

        public SearchWindowNode Get(string nodeName) => _nestedNodes[nodeName];

        public bool Contains(string nodeName) => _nestedNodes.ContainsKey(nodeName);

        /// <summary>
        /// Returns all nested nodes, without self
        /// </summary>
        /// <returns></returns>
        public IEnumerable<SearchWindowNode> Traverse()
        {
            return _nestedNodes.Values.SelectMany(node => node.TraverseWithSelf());
        }

        private IEnumerable<SearchWindowNode> TraverseWithSelf()
        {
            yield return this;
            foreach (var n in _nestedNodes.Values.SelectMany(node => node.TraverseWithSelf())) yield return n;
        }

        /// <summary>
        /// Returns just direct nested nodes
        /// </summary>
        /// <returns></returns>
        public IEnumerable<SearchWindowNode> GetDirectChilds() => _nestedNodes.Values;
    }
}