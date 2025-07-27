using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace OpenIdProvider.Blazor.Services;

public static class EntityFrameworkExtensions
{
    /// <summary>
    /// Synchronizes a child collection of a tracked entity with a new set of items.
    /// This method efficiently adds new items, removes old ones, and leaves unchanged items alone.
    /// </summary>
    /// <typeparam name="TEntity">The type of the entity in the collection.</typeparam>
    /// <typeparam name="TViewModel">The type of the view model or DTO representing the new state.</typeparam>
    /// <typeparam name="TKey">The type of the key used for matching.</typeparam>
    /// <param name="dbCollection">The tracked collection from the database entity (e.g., client.AllowedScopes).</param>
    /// <param name="viewModelItems">The list of items from the view model representing the desired state.</param>
    /// <param name="entityKeySelector">A function to select the key from the database entity.</param>
    /// <param name="viewModelKeySelector">A function to select the key from the view model item.</param>
    /// <param name="creator">A function that creates a new entity from a view model item.</param>
    public static void SyncCollection<TEntity, TViewModel, TKey>(
        this ICollection<TEntity> dbCollection,
        IEnumerable<TViewModel> viewModelItems,
        Func<TEntity, TKey> entityKeySelector,
        Func<TViewModel, TKey> viewModelKeySelector,
        Func<TViewModel, TEntity> creator)
        where TEntity : class
        where TKey : notnull
    {
        var dbKeys = dbCollection.Select(entityKeySelector).ToHashSet();
        var vmKeys = viewModelItems.Select(viewModelKeySelector).ToHashSet();

        // Remove items that are in the DB but not in the new view model list
        var itemsToRemove = dbCollection.Where(e => !vmKeys.Contains(entityKeySelector(e))).ToList();
        foreach (var item in itemsToRemove)
        {
            dbCollection.Remove(item);
        }

        // Add items that are in the new view model list but not in the DB
        var itemsToAdd = viewModelItems.Where(vm => !dbKeys.Contains(viewModelKeySelector(vm)));
        foreach (var item in itemsToAdd)
        {
            dbCollection.Add(creator(item));
        }
    }
}
