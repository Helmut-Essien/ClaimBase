# MobileApp

Read from [SKILL.md](SKILL.md) when changing lecturer flows. Portal rules do not apply to MAUI layouts. Product invariants do. Screens use MVVM.

## Who it is for

Lecturers only. Login, log a session, see whether that session is on a claim, raise an exception on a wrong log. Admin setup stays in the Portal.

## MVVM

Use `CommunityToolkit.Mvvm`. Each screen has a XAML `ContentPage` and a `*ViewModel`. The page does not call the API, SQLite, or secure storage.

| Piece | Responsibility |
|-------|----------------|
| View (XAML + code-behind) | Layout and bindings. Code-behind only calls `InitializeComponent` and sets `BindingContext` from DI. |
| ViewModel | State, validation messages, and `[RelayCommand]` actions. Inherits `ObservableObject`. |
| Service | HTTP, SQLite, and secure storage. ViewModels depend on interfaces, not on `HttpClient` created in the page. |

- Bind controls to `[ObservableProperty]` fields. Commands are `[RelayCommand]` methods. Async commands await I/O and leave the UI thread free ([performance.md](performance.md)).
- Every screen view model inherits one `BaseViewModel` (`ObservableObject`) that owns `IsBusy`. A command returns immediately when `IsBusy` is already true, and clears it in `finally`.
- Set `x:DataType` on each page so bindings compile. Do not leave the page on reflection bindings.
- Register each ViewModel and page in `MauiProgram` and resolve the page from DI so the ViewModel is constructed with its services.
- Tab pages (home, session list) and their view models are singletons. A form page (log session, exception) and its view model are transient. `OnAppearing` on a form must not rebuild the form after a child page returns.
- A service that uses the SQLite `DbContext` is scoped. Do not inject that context into a singleton. A singleton that must write opens a scope, uses the context, and disposes the scope.
- A new screen ships as `Features/{Name}/{Name}Page.xaml` plus `{Name}ViewModel.cs` in the same slice.
- Do not put click handlers, sync loops, or token reads in the code-behind. Do not use a static `BindingContext` new-ed inside the page.

## Offline log

1. The lecturer picks a course code and enters start and end. The device writes a local row immediately with a new `clientId` from `Ulid.NewUlid().ToString()` (26 characters, NUlid) and `RecordedAt` = device time. Do not use `Guid`.
2. `StartsAt` and `EndsAt` are the values entered. Sync must not replace them with the upload time.
3. When the network is available, POST `/api/sessions` with that `clientId`.
4. The same `clientId` posted twice returns the original server session. The app then marks the local row synced.
5. Server overlap or semester rejection stays on the device as a failed row with the API message. The app does not delete it silently.
6. Course codes come from a synced catalog. An unknown code cannot be queued as a payable session. The lecturer sees that the code is not in the catalog.

## Screens

| Screen | Behavior |
|--------|----------|
| Login | Email + password. Store the JWT in secure storage. Forgot password calls `POST /api/auth/forgot-password`. The link opens the Portal reset page. The lecturer then signs in here with the new password. |
| Home | Open semester name, pending sync count, recent sessions. |
| Log session | Course code, start, end. Save works offline. |
| Session detail | Status: pending sync, submitted, on a draft claim, approved, or exception. |
| Exception | Short note when a submitted session was entered wrongly. Disabled once the claim is approved. |

## Sync rules

- The outbox is the source until the server accepts the row. Push the pending rows in one batch and save the local database once per batch.
- Pull claim status for the signed-in lecturer when online. Do not pull other staff.
- A pull writes server state only. Disable change tracking for that save and re-enable it in `finally`, so the pull does not queue the same rows again.
- Clock skew: send the entered local date-time with an offset. The server converts to UTC and applies the tenant time zone for the semester-day check.

## Session and local data

The phone has one access token in `SecureStorage`. There is no refresh token in the current slices.

- Offline, timeout, HTTP 5xx, and HTTP 429 are transient. Keep the token and keep the local database. Do not navigate to login.
- HTTP 401 means the session is dead. Go to login. Leave unsynced session rows in SQLite so the next successful login can send them.
- Sign out is different. It deletes the local database only after that delete succeeds. If the delete fails, stay signed in and show the error. Do not clear the token first and then fail the wipe.
- Do not send the same `HttpRequestMessage` twice. Clone it, including a buffered body, before any retry.

## Project shape

```
MobileApp/
  ClaimBase.MobileApp.csproj
  Features/Auth/
    LoginPage.xaml
    LoginViewModel.cs
  Features/Sessions/
    LogSessionPage.xaml
    LogSessionViewModel.cs
  Data/                 # SQLite, outbox
  Services/Api/
```

Share HTTP contracts with `ClaimBase.Shared` by project reference, or duplicate the request records in the mobile project if a MAUI reference to Shared pulls unwanted ASP.NET packages. Prefer a reference to Shared when the package graph stays clean. Do not reference Domain, Infrastructure, or Api.

## Documentation

MAUI uses the same CS1591 rule as the backend. Document public types while writing them, per [documentation.md](documentation.md).

Required inline comments:

- `clientId` is the idempotency key so a retry does not create a second session. It is a ULID string created on the device before the row is queued.
- `StartsAt` / `EndsAt` are the times the lecturer entered. Sync must not replace them with the upload time.
- `RecordedAt` is when the device saved the row, which can be long after the lecture if the phone was offline.
