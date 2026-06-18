// src/Dynamite.Modules.Economy/Helpers/SpecialPoolAutocomplete.cs
namespace Dynamite.Modules.Economy.Helpers;

using Discord;
using Discord.Interactions;
using Dynamite.Modules.Economy.Services;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Autocomplete handler cho các command cần chọn special pool.
/// Trả về danh sách pool đang active trong guild dưới dạng dropdown.
/// Value = Pool ID (Guid string), Label = tên pool.
/// </summary>
public class SpecialPoolAutocomplete : AutocompleteHandler
{
    public override async Task<AutocompletionResult> GenerateSuggestionsAsync(
        IInteractionContext          context,
        IAutocompleteInteraction     autocompleteInteraction,
        IParameterInfo               parameter,
        IServiceProvider             services)
    {
        var specialPoolService = services.GetRequiredService<SpecialPoolService>();
        var pools = await specialPoolService.GetActivePoolsAsync(context.Guild.Id);

        if (pools.Count == 0)
            return AutocompletionResult.FromSuccess(
                [new AutocompleteResult("(Không có pool nào đang hoạt động)", "none")]);

        var current = autocompleteInteraction.Data.Current.Value?.ToString() ?? "";

        var results = pools
            .Where(p => string.IsNullOrEmpty(current)
                     || p.PoolName.Contains(current, StringComparison.OrdinalIgnoreCase))
            .Select(p => new AutocompleteResult(
                $"{p.PoolName} (Lv.{p.MinLevel}+)",
                p.Id.ToString()))
            .Take(25);

        return AutocompletionResult.FromSuccess(results);
    }
}
