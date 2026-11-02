namespace WslcContainerAdmin.Models;

public sealed record PortMapping(int HostPort, int ContainerPort);
