using System.Text.Json;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Together.Client.Storage;
using Together.Core;
using TripVariant = Together.Core.Variant;

namespace Together.Client.Pages;

public partial class Home
{
    [Inject] public BrowserStore Store { get; set; } = default!;
    [Inject] public IJSRuntime Js { get; set; } = default!;
    private Workspace data = new();
    private Guid? currentId;
    private Trip? Current => data.Trips.FirstOrDefault(t => t.Id == currentId);
    private bool HasExample => data.Trips.Any(t => t.Name == ExampleData.TripName);
    private bool loading = true, readFailed, busy, conflict;
    private string? editor, error, success;
    private Trip? editingTrip;
    private TripVariant? editingVariant;
    private int comparisonKey;
    private Func<Task>? retry;

    protected override Task OnInitializedAsync() => Load();
    private async Task Load()
    {
        loading = true;
        readFailed = false;
        error = null;
        success = null;
        try
        {
            var loaded = await Store.ReadAsync();
            if (loaded.Trips.Count == 0)
                loaded = await AddExampleData(loaded);
            data = loaded;
            if (!data.Trips.Any(t => t.Id == currentId))
                currentId = data.Trips.FirstOrDefault()?.Id;
        }
        catch (Exception ex) when (ex is JSException or JsonException or InvalidOperationException)
        {
            readFailed = true;
        }
        finally { loading = false; }
    }
    private async Task<Workspace> AddExampleData(Workspace empty)
    {
        var example = ExampleData.Create();
        var trip = example.Trips.Single();
        var result = await Store.WriteAsync(example, empty.Revision, trip.Id);
        if (result == "ok")
            return example;
        if (result == "conflict")
            return await Store.ReadAsync();
        throw new InvalidOperationException("Не удалось сохранить демонстрационные данные");
    }
    private void SwitchTrip(ChangeEventArgs e)
    {
        currentId = Guid.TryParse(e.Value?.ToString(), out var id) ? id : null;
        error = null;
        success = null;
        retry = null;
    }
    private void StartEditor(string type)
    {
        editor = type;
        error = null;
        success = null;
        retry = null;
        conflict = false;
    }
    private void NewTrip()
    {
        editingTrip = null;
        StartEditor("trip");
    }
    private void EditTrip()
    {
        editingTrip = Current;
        StartEditor("trip");
    }
    private async Task AddExample()
    {
        if (HasExample)
            return;
        var trip = ExampleData.Create().Trips.Single();
        trip.Revision = 0;
        retry = AddExample;
        if (await Commit(next => next.Trips.Add(trip), trip.Id))
        {
            currentId = trip.Id;
            success = "Пример добавлен";
        }
    }
    private void NewVariant()
    {
        editingVariant = null;
        StartEditor("variant");
    }
    private void EditVariant(Guid id)
    {
        editingVariant = Current!.Variants.Single(v => v.Id == id);
        StartEditor("variant");
    }
    private async Task CloseEditor()
    {
        await Js.InvokeVoidAsync("tripUi.close", "editor");
        editor = null;
        error = null;
        retry = null;
    }

    private async Task<bool> Commit(Action<Workspace> change, Guid tripId, bool comparisonOnly = false)
    {
        if (busy)
            return false;
        busy = true;
        success = null;
        error = null;
        conflict = false;
        try
        {
            var next = new Workspace { SchemaVersion = data.SchemaVersion, Revision = data.Revision, Trips = [.. data.Trips] };
            var index = next.Trips.FindIndex(t => t.Id == tripId);
            if (index >= 0)
                next.Trips[index] = next.Trips[index].Copy(!comparisonOnly);
            change(next);
            next.Revision++;
            var trip = next.Trips.FirstOrDefault(t => t.Id == tripId);
            if (trip is not null)
            {
                trip.Revision++;
                trip.UpdatedAt = DateTimeOffset.UtcNow;
            }
            var result = await Store.WriteAsync(next, data.Revision, tripId, comparisonOnly);
            if (result != "ok")
            {
                conflict = result == "conflict";
                error = conflict ? "Данные изменились в другой вкладке. Черновик сохранён в форме: скопируйте нужные поля перед загрузкой актуальных данных." : "Не удалось сохранить данные в этом браузере";
                return false;
            }
            data = next;
            success = "Сохранено";
            retry = null;
            return true;
        }
        catch (Exception ex) when (ex is JSException or JsonException or InvalidOperationException)
        {
            error = "Не удалось сохранить данные в этом браузере";
            return false;
        }
        finally { busy = false; }
    }
    private async Task SaveTrip(TripDraft draft)
    {
        if (draft.Validate().Count > 0)
            return;
        var id = editingTrip?.Id ?? Guid.NewGuid();
        retry = () => SaveTrip(draft);
        if (await Commit(next =>
        {
            var trip = next.Trips.FirstOrDefault(t => t.Id == id);
            if (trip is null)
            {
                trip = new Trip { Id = id };
                next.Trips.Add(trip);
            }
            draft.Apply(trip);
        }, id))
        {
            currentId = id;
            await CloseEditor();
        }
    }
    private async Task SaveVariant(VariantDraft draft)
    {
        if (draft.Validate().Count > 0)
            return;
        var tripId = Current!.Id;
        var variantId = editingVariant?.Id ?? Guid.NewGuid();
        retry = () => SaveVariant(draft);
        if (await Commit(next =>
        {
            var trip = next.Trips.Single(t => t.Id == tripId);
            var variant = trip.Variants.FirstOrDefault(v => v.Id == variantId);
            if (variant is null)
            {
                variant = new TripVariant { Id = variantId, TripId = tripId };
                trip.Variants.Add(variant);
            }
            draft.Apply(variant);
        }, tripId))
            await CloseEditor();
    }
    private async Task DeleteVariant(Guid id)
    {
        var name = Current!.Variants.Single(v => v.Id == id).Name;
        if (!await Js.InvokeAsync<bool>("confirm", $"Удалить вариант «{name}»?"))
            return;
        await ConfirmedDelete(id, Current.Id);
    }
    private async Task ConfirmedDelete(Guid id, Guid tripId)
    {
        retry = () => ConfirmedDelete(id, tripId);
        await Commit(next =>
        {
            var trip = next.Trips.Single(t => t.Id == tripId);
            trip.Variants.RemoveAll(v => v.Id == id);
            trip.SelectedVariantIds.Remove(id);
        }, tripId);
    }
    private async Task ToggleVariant(Guid id)
    {
        var tripId = Current!.Id;
        retry = () => ToggleVariant(id);
        if (!await Commit(next =>
        {
            var selected = next.Trips.Single(t => t.Id == tripId).SelectedVariantIds;
            if (!selected.Remove(id))
                selected.Add(id);
        }, tripId, comparisonOnly: true))
            comparisonKey++; // Recreate checkbox DOM after a rejected write.
    }
    private async Task ToggleCriterion(string key)
    {
        var tripId = Current!.Id;
        retry = () => ToggleCriterion(key);
        if (!await Commit(next =>
        {
            var selected = next.Trips.Single(t => t.Id == tripId).SelectedCriteria;
            if (!selected.Remove(key))
                selected.Add(key);
        }, tripId, comparisonOnly: true))
            comparisonKey++;
    }
    private Task Retry() => retry?.Invoke() ?? Task.CompletedTask;
    private async Task ReloadCurrent()
    {
        if (editor is not null && !await Js.InvokeAsync<bool>("confirm", "Загрузить актуальные данные и отбросить несохранённые правки?"))
            return;
        // Keep the draft visible if reading still fails.
        try
        {
            var loaded = await Store.ReadAsync();
            if (editor is not null)
                await CloseEditor();
            data = loaded;
            error = null;
            success = null;
            conflict = false;
            retry = null;
            if (!data.Trips.Any(t => t.Id == currentId))
                currentId = data.Trips.FirstOrDefault()?.Id;
        }
        catch (Exception ex) when (ex is JSException or JsonException or InvalidOperationException)
        {
            error = "Не удалось прочитать актуальные данные. Правки остаются в форме.";
        }
    }
}
