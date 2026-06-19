// src/Dynamite.Modules.Economy/Helpers/RodAutocomplete.cs
namespace Dynamite.Modules.Economy.Helpers;

using Discord;
using Discord.Interactions;
using Dynamite.Modules.Economy.Services;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Autocomplete handler cho /shop repair-rod.
/// Trả về danh sách cần câu user đang sở hữu, ưu tiên cần gãy/mòn lên trên.
/// Value = tên cần câu (string), Label = "{emoji} {tên} — độ bền x/max".
/// </summary>
public class RodAutocomplete : AutocompleteHandler
{
    public override async Task<AutocompletionResult> GenerateSuggestionsAsync(
        IInteractionContext      context,
        IAutocompleteInteraction autocompleteInteraction,
        IParameterInfo           parameter,
        IServiceProvider         services)
    {
        var shop = services.GetRequiredService<ShopService>();

        var rods = await shop.GetUserRodsForAutocompleteAsync(
            context.Guild.Id, context.User.Id);

        if (rods.Count == 0)
            return AutocompletionResult.FromSuccess(
                [new AutocompleteResult("(Bạn chưa có cần câu nào)", "none")]);

        var current = autocompleteInteraction.Data.Current.Value?.ToString() ?? "";

        var results = rods
            .Where(r => string.IsNullOrEmpty(current)
                     || r.Name.Contains(current, StringComparison.OrdinalIgnoreCase))
            .Select(r => new AutocompleteResult(r.Label, r.Name))
            .Take(25);

        return AutocompletionResult.FromSuccess(results);
    }
}
