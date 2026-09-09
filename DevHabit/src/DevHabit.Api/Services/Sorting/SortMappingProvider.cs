namespace DevHabit.Api.Services.Sorting;

public sealed class SortMappingProvider(IEnumerable<ISortMappingDefinition> sortMappingDefinitions)
{
    public SortMapping[] GetMappings<TSource, TDestination>()
    {
        SortMappingDefinition<TSource, TDestination>? sortMappingDefinition = sortMappingDefinitions
         .OfType<SortMappingDefinition<TSource, TDestination>>()
         .FirstOrDefault();

        if (sortMappingDefinition is null)
        {
            throw new InvalidOperationException($"The mapping from '{typeof(TSource).Name}' to '{typeof(TDestination).Name}' is not defined.");
        }


        return sortMappingDefinition.Mappings;
    }

    // This will validate that all sort fields provided are correct. 
    public bool ValidateMappings<TSource, TDestination>(string? sort)
    {
        if (string.IsNullOrWhiteSpace(sort))
        {
            return true;
        }

        var sortFields = sort
            .Split(',')
            .Select(f => f.Trim().Split(' ')[0])
            .Where(f => !string.IsNullOrWhiteSpace(f))
            .ToList();

        SortMapping[] mapping = GetMappings<TSource, TDestination>();

        return sortFields.All(f => mapping.Any(m => m.SortField.Equals(f, StringComparison.OrdinalIgnoreCase)));
    }
};

