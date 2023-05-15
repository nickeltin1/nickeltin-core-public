using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Pool;

namespace nickeltin.Core.Editor
{
    internal class SearchWindowNode
    {
        public int depth
        {
            get
            {
                return parent != null ? parent.depth + 1 : 0;
            }
        }

        public string name { get; private set; }

        public string displayName { get; set; }
        public bool isEndNode { get; private set; }
        public ISearchWindowEntry data { get; private set; }
            
        public SearchWindowNode parent { get; private set; }

        public int directChildCount => _nestedNodes.Count;
            
        private readonly Dictionary<string, SearchWindowNode> _nestedNodes;
        

        private SearchWindowNode(string displayName, string name, bool isEndNode, ISearchWindowEntry data)
        {
            _nestedNodes = DictionaryPool<string, SearchWindowNode>.Get();
            this.displayName = displayName;
            this.name = name;
            this.isEndNode = isEndNode;
            this.data = data;
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
            if (isEndNode)
            {
                Debug.LogError("End node can't have children's");
                return;
            }
            
            node.parent = this;
            _nestedNodes.Add(node.name, node);
        }

        public void Remove(SearchWindowNode node)
        {
            _nestedNodes.Remove(node.name);
        }

        public SearchWindowNode Get(string nodeName) => _nestedNodes[nodeName];

        public bool Contains(string nodeName) => _nestedNodes.ContainsKey(nodeName);

        /// <summary>
        /// Returns all nested nodes, without self
        /// </summary>
        /// <returns></returns>
        public IEnumerable<SearchWindowNode> Traverse()
        {
            return _nestedNodes.Values.SelectMany<SearchWindowNode, SearchWindowNode>(node => node.TraverseWithSelf());
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