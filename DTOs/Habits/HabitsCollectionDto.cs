using DevHabit.Api.DTOs.Common;
using Newtonsoft.Json;

namespace DevHabit.Api.DTOs.Habits;

public sealed record HabitsCollectionDto : ICollectionResponse<HabitDto>, ILinksResponse
{
    public List<HabitDto> Data { get; init; }

    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public List<LinkDto> Links { get; set; }
}