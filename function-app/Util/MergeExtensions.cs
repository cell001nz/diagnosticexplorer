namespace DiagnosticExplorer.Util;

public static class MergeExtensions
{
    /// <summary>
    /// Merges a source collection into a target collection using a key selector.
    /// Adds new items (using default constructor + mergeAction), updates existing items, and removes items not in the source.
    /// </summary>
    /// <typeparam name="TTarget">The type of the target collection items (must have a parameterless constructor)</typeparam>
    /// <typeparam name="TSource">The type of the source collection items</typeparam>
    /// <typeparam name="TKey">The type of the key used for matching</typeparam>
    /// <param name="target">The target collection to merge into</param>
    /// <param name="source">The source collection to merge from</param>
    /// <param name="sourceKey">Function to extract the key from a source item</param>
    /// <param name="targetKey">Function to extract the key from a target item</param>
    /// <param name="mergeAction">Action to update an existing target item from a source item</param>
    public static void MergeFrom<TTarget, TSource, TKey>(
        this ICollection<TTarget> target,
        IReadOnlyCollection<TSource> source,
        Func<TSource, TKey> sourceKey,
        Func<TTarget, TKey> targetKey,
        Action<TTarget, TSource> mergeAction)
        where TTarget : new()
        where TKey : struct, IComparable<TKey>
    {
        target.MergeFrom(
            source,
            sourceKey,
            targetKey,
            mergeAction,
            createNew: sourceItem =>
            {
                var newItem = new TTarget();
                mergeAction(newItem, sourceItem);
                return newItem;
            });
    }
    
    /// <summary>
    /// Merges a source collection into a target collection using a key selector.
    /// Adds new items, updates existing items, and removes items not in the source.
    /// </summary>
    /// <typeparam name="TTarget">The type of the target collection items</typeparam>
    /// <typeparam name="TSource">The type of the source collection items</typeparam>
    /// <typeparam name="TKey">The type of the key used for matching</typeparam>
    /// <param name="target">The target collection to merge into</param>
    /// <param name="source">The source collection to merge from</param>
    /// <param name="sourceKey">Function to extract the key from a source item</param>
    /// <param name="targetKey">Function to extract the key from a target item</param>
    /// <param name="mergeAction">Action to update an existing target item from a source item</param>
    /// <param name="createNew">Function to create a new target item from a source item</param>
    public static void MergeFrom<TTarget, TSource, TKey>(
        this ICollection<TTarget> target,
        IReadOnlyCollection<TSource> source,
        Func<TSource, TKey> sourceKey,
        Func<TTarget, TKey> targetKey,
        Action<TTarget, TSource> mergeAction,
        Func<TSource, TTarget> createNew)
        where TKey : struct, IComparable<TKey>
    {
        // Get the incoming keys (only non-default values are considered existing)
        var defaultKey = default(TKey);
        var incomingKeys = source
            .Select(sourceKey)
            .Where(key => key.CompareTo(defaultKey) > 0)
            .ToHashSet();
        
        // Remove items that are not in the incoming list
        var itemsToRemove = target
            .Where(t => !incomingKeys.Contains(targetKey(t)))
            .ToList();
        
        foreach (var item in itemsToRemove)
        {
            target.Remove(item);
        }
        
        // Add or update items
        foreach (var sourceItem in source)
        {
            var key = sourceKey(sourceItem);
            
            if (key.CompareTo(defaultKey) > 0)
            {
                // Update existing item
                var existingItem = target.FirstOrDefault(t => 
                    targetKey(t).Equals(key));
                
                if (existingItem != null)
                {
                    mergeAction(existingItem, sourceItem);
                }
            }
            else
            {
                // Add new item
                target.Add(createNew(sourceItem));
            }
        }
    }
}


