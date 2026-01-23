

namespace DataView2.Options;

public class DataView2Options
{
    public string DeviceLogFolder { get; set; } = string.Empty;
    public ServiceOption[] ServiceOptions { get; set; } = Array.Empty<ServiceOption>();
    //public Language Language { get; set; }
}