namespace nickeltin.Core.Editor
{
    public interface ISearchWindowEntry
    {
        /// <summary>
        /// Data associates with entry.
        /// </summary>
        /// <returns></returns>
        object GetData();
        
        
        /// <summary>
        /// What path used for displaying names, can be repeated at the same levels.
        /// Should be the same length as <see cref="GetPath"/>
        /// </summary>
        /// <returns></returns>
        string[] GetPathAlias();
        
        /// <summary>
        /// Path used to build display hierarchy, should be unique.
        /// Should be the same length as <see cref="GetPathAlias"/>
        /// </summary>
        /// <returns></returns>
        string[] GetPath();
    }
}