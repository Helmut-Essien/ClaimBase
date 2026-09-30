namespace ClaimBase.MobileApp;

/// <summary>
/// Lecturer application shell. Sign-in is added with session logging.
/// </summary>
public partial class App : Application
{
    /// <summary>
    /// Initializes the application.
    /// </summary>
    public App()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Opens the single window on the shell.
    /// </summary>
    /// <param name="activationState">Platform activation state.</param>
    /// <returns>The application window.</returns>
    protected override Window CreateWindow(IActivationState? activationState)
    {
        return new Window(new AppShell());
    }
}
