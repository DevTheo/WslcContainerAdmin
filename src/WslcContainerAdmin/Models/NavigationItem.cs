namespace WslcContainerAdmin.Models;

/// <summary>A sidebar entry. <see cref="ViewModelType"/> is resolved from DI when selected.</summary>
public sealed record NavigationItem(string Title, Type ViewModelType);
