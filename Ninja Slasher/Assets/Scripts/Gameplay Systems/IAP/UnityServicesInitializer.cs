using System.Threading.Tasks;
using Unity.Services.Core;

public static class UnityServicesInitializer
{
    private static Task _initTask;
    private static readonly object _lock = new object();

    public static Task EnsureInitializedAsync()
    {
        lock (_lock)
        {
            if (_initTask == null || _initTask.IsFaulted)
                _initTask = InitializeInternal();
        }
        return _initTask;
    }

    private static async Task InitializeInternal()
    {
        if (UnityServices.State == ServicesInitializationState.Initialized) return;

        await UnityServices.InitializeAsync();
    }
}
