---
name: blazor-components
description: Structure and conventions for Blazor Web App (Server) components in PCH.App. Load when creating or editing .razor pages/components, the typed API client, or PCH.App/Program.cs.
---

# Blazor Components — Personal Command Hub

PCH.App is a **Blazor Web App with Server interactivity**. It talks to PCH.Api over HTTP (it references only `PCH.Core`, not `PCH.Data`). Follow these patterns.

## Component structure
Order inside a `.razor` file:
1. `@page` / `@rendermode` / `@using` / `@inject` directives
2. Markup (with the state guards below)
3. `@code { }` — state fields, lifecycle, handlers, helpers, nested form-model classes

- **Interactive pages need `@rendermode InteractiveServer`** at the top, or buttons/forms won't respond.
- Reusable presentational components go in `Components/Shared/` (e.g. `ProgressBar.razor`) and take `[Parameter]` inputs. Clamp/guard inputs inside the component.

## Data access: `@inject`, never `new`
- Inject the typed `TaskApiClient` (registered via `AddHttpClient<TaskApiClient>` in `Program.cs`); never construct `HttpClient` directly.
- The API client wraps `System.Net.Http.Json` (`GetFromJsonAsync`, `PostAsJsonAsync`, ...) and returns DTOs from `PCH.Core.Dtos`. Treat 404 as a `null`/`false` result, not an exception, where it's expected.

## Loading / error / empty state pattern (required for any data-backed page)
Every page that loads data must handle all three states explicitly:

```razor
@if (_loading)        { <spinner/> }
else if (_error != null) { <alert + Retry button/> }
else
{
    @if (Visible.Count == 0) { <p>No tasks found.</p> }
    else { <table>...</table> }
}
```
- Load in `OnInitializedAsync`; wrap the call in try/catch/finally, set `_error` on failure, always clear `_loading` in `finally`.
- The server owns business rules (e.g. progress=100 ⇒ Done). **Display** that state; don't re-implement the rule client-side.

## Razor gotcha: no `@{ }` inside a code block's markup
Inside an `@if/else/foreach` body you're already in a code context — an explicit `@{ ... }` there fails with `RZ1010`. Compute derived values as a **property in `@code`** instead:

```razor
@* WRONG inside else { } *@
@{ var visible = _tasks.Where(...).ToList(); }

@* CORRECT *@
@code { private List<T> Visible => ActiveFilter is null ? _tasks : _tasks.Where(...).ToList(); }
```

## Namespaces
Add shared usings (`PCH.Core.Dtos`, `PCH.Core.Models`, `PCH.App.Services`, `PCH.App.Components.Shared`) to `Components/_Imports.razor` so pages don't repeat them.

## Verify
A Razor compile error only shows on build, and a rendered page only proves out at runtime. Build with the full `dotnet` path, then load the page (`Invoke-WebRequest http://localhost:5290/<route>`) with the API running to confirm the HTTP hop works end to end.
