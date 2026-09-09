namespace DevHabit.Api.Services.Sorting;

/* 
What it does is:
Want to define mappings from 'SortField' -> 'PropertyName' on my Entity
So these definitions can be used at runtime. To Properly generate order by statements
And also perform input validation on the sort parameters
 */
public sealed record SortMapping(string SortField, string PropertyName, bool Reverse = false);

