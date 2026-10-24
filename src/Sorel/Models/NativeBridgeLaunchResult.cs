using System.Diagnostics;

namespace Sorel.Models;

public sealed record NativeBridgeLaunchResult(Process Process, bool BridgeReady);
