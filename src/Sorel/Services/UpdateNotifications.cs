using Sorel.Models;

namespace Sorel.Services;

internal static class UpdateNotifications
{
    public static bool TryClaim(SorelSettings settings, string version, bool ready)
    {
        if ((ready ? settings.LastUpdateReadyNotification : settings.LastUpdateFoundNotification) == version) return false;
        if (ready) settings.LastUpdateReadyNotification = version;
        else settings.LastUpdateFoundNotification = version;
        return true;
    }
}
