using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

namespace nickeltin.Core.Editor
{
    public abstract class SearchWindowBase<T> : ScriptableObject, ISearchWindowProvider where T : SearchWindowBase<T>
    {
        public delegate Texture2D EntryIconGetter(ISearchWindowEntry entry);
        
        private Action<ISearchWindowEntry> _onObjectSelected;
        private IEnumerable<ISearchWindowEntry> _availableObjects;
        private bool _showNone;
        private bool _cleanUpTree;
        private string _topLabel;
        private EntryIconGetter _customIconGetter;

        private class EmptyEntry : ISearchWindowEntry
        {
            private readonly string[] path = Array.Empty<string>();
            public object GetData() => null;
            public string[] GetPathAlias() => path;
            public string[] GetPath() => path;
        }

        public List<SearchTreeEntry> CreateSearchTree(SearchWindowContext context)
        {
            var searchList = new List<SearchTreeEntry> { new SearchTreeGroupEntry(new GUIContent(_topLabel), 0) };
            var tree = GenerateNodeTree(_availableObjects);
            if (_cleanUpTree)
            {
                CleanUpTree(tree);
            }
            RecreateTree(searchList, tree);
            return searchList;
        }

        private static SearchWindowNode GenerateNodeTree(IEnumerable<ISearchWindowEntry> objects)
        {
            var root = SearchWindowNode.GroupNode("root", "");

            foreach (var obj in objects)
            {
                var path = obj.GetPath();
                var pathAlias = obj.GetPathAlias();

                var currentNode = root;
                for (var i = 0; i < path.Length - 1; i++)
                {
                    var pathElement = path[i];
                    if (currentNode.Contains(pathElement))
                    {
                        currentNode = currentNode.Get(pathElement);
                    }
                    else
                    {
                        var newNode = SearchWindowNode.GroupNode(pathElement, pathAlias[i]);
                        currentNode.Add(newNode);
                        currentNode = newNode;
                    }
                }
                
                currentNode.Add(SearchWindowNode.EndNode(obj));
            }

            return root;
        }

        private static void CleanUpTree(SearchWindowNode rootNode)
        {
            foreach (var node in rootNode.Traverse().Reverse())
            {
                if (node.DirectChildCount == 1 && !node.IsEndNode && node.Parent != null)
                {
                    var child = node.GetDirectChilds().First();
                    child.DisplayName = node.DisplayName + "." + child.DisplayName;
                    if (!node.Parent.Contains(child.Name))
                    {
                        node.Parent.Remove(node);
                        node.Parent.Add(child);
                    }
                }
            }
        }
        
        private void RecreateTree(ICollection<SearchTreeEntry> tree, SearchWindowNode rootNode)
        {
            if (_showNone)
            {
                tree.Add(new SearchTreeEntry(SearchWindowDefatuls.noneContnet)
                {
                    level = 1,
                    userData = new EmptyEntry()
                });
            }

            var typesCount = 0;
            foreach (var node in rootNode.Traverse())
            {
                if (node.IsEndNode)
                {
                    var icon = _customIconGetter != null ? _customIconGetter(node.Data) : GetIconForEntry(node.Data);
                    var content = new GUIContent(node.DisplayName, icon);
                    var entry = new SearchTreeEntry(content)
                    {
                        level = node.Depth,
                        userData = node.Data
                    };
                    tree.Add(entry);
                }
                else
                {
                    var content = new GUIContent(node.DisplayName);
                    var entry = new SearchTreeGroupEntry(content, node.Depth);
                    tree.Add(entry);
                }

                typesCount++;
            }

            if (typesCount == 0)
            {
                tree.Add(new SearchTreeEntry(SearchWindowDefatuls.noItemsFoundContent){level = 1});
            }
        }
        
        public bool OnSelectEntry(SearchTreeEntry SearchTreeEntry, SearchWindowContext context)
        {
            if (SearchTreeEntry.content == SearchWindowDefatuls.noItemsFoundContent)
            {
                return true;
            }
            _onObjectSelected?.Invoke((ISearchWindowEntry)SearchTreeEntry.userData);
            return true;
        }

        protected abstract Texture2D GetIconForEntry(ISearchWindowEntry entry);

        private static T _instance;


        /// <summary>
        /// Will show Search window with prowided options awaiting for user input.
        /// </summary>
        /// <param name="availableObjects">Shown entires</param>
        /// <param name="onObjectSelected">Action when users selects end entry</param>
        /// <param name="showNoneEntry">If true on the top of entries list will be displayed empty entry with null data</param>
        /// <param name="cleanUpTree">If true categories that has only one child will be shinked into one entry</param>
        /// <param name="position">Screen position of search window, if not defined current position of mouse will be used.
        ///     Even if called not from GUI frame (<see cref="IMGUIEventsCapturerWindow"/> will be used)</param>
        /// <param name="size"></param>
        /// <param name="customIconGetter"></param>
        /// <param name="topLabel">What text will be displayed on top of search window</param>
        public static void Open(IEnumerable<ISearchWindowEntry> availableObjects, Action<ISearchWindowEntry> onObjectSelected, 
            bool showNoneEntry = true, 
            bool cleanUpTree = true, 
            Vector2? position = null, 
            Vector2? size = null, 
            EntryIconGetter customIconGetter = null,
            string topLabel = "Select")
        {
            if (_instance == null)
            {
                _instance = CreateInstance<T>();
            }

            _instance._onObjectSelected = onObjectSelected;
            _instance._availableObjects = availableObjects;
            _instance._showNone = showNoneEntry;
            _instance._cleanUpTree = cleanUpTree;
            _instance._customIconGetter = customIconGetter;
            _instance._topLabel = topLabel;

            var e = Event.current;
            var hasEvent = e != null;
            var posDefined = position.HasValue;
            var pos = hasEvent ? GUIUtility.GUIToScreenPoint(e.mousePosition) : new Vector2(100, 100);
            var widthHeight = size ?? new Vector2(0, 0);
            if (posDefined)
            {
                pos = GUIUtility.GUIToScreenPoint(position.Value);
            }

            if (posDefined || hasEvent)
            {
                var context = new SearchWindowContext(pos, widthHeight.x, widthHeight.y);
                SearchWindow.Open(context, _instance);
            }
            else
            {
                IMGUIEventsCapturerWindow.CaptureEvent(evt =>
                {
                    _lastWidthHeight = widthHeight;
                    _lastPopupPos = GUIUtility.GUIToScreenPoint(evt.mousePosition);
                    EditorApplication.update += DelayedCall;
                });
            }
           
        }

        private static Vector2 _lastPopupPos;
        private static Vector2 _lastWidthHeight;
        
        private static void DelayedCall()
        {
            EditorApplication.update -= DelayedCall;
            var context = new SearchWindowContext(_lastPopupPos, _lastWidthHeight.x, _lastWidthHeight.y);
            SearchWindow.Open(context, _instance);
        }

        public static Rect CalcualteFieldRect(Rect fieldPos)
        {
            return new Rect(fieldPos.x + (fieldPos.width / 2f), fieldPos.y + (fieldPos.height * 2f), fieldPos.width, 0);
        }
    }
}