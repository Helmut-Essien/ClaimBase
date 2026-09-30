using Android.App;
using Android.Runtime;

namespace ClaimBase.MobileApp;

/// <summary>
/// Android application host for the lecturer app.
/// </summary>
[Application]
public class MainApplication : MauiApplication
{
    /// <summary>
    /// Creates the Android application from the JNI handle.
    /// </summary>
    /// <param name="handle">The native application handle.</param>
    /// <param name="ownership">Who owns the handle.</param>
    public MainApplication(IntPtr handle, JniHandleOwnership ownership)
        : base(handle, ownership)
    {
    }

    /// <summary>
    /// Builds the MAUI app for this process.
    /// </summary>
    /// <returns>The lecturer application.</returns>
    protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
}
