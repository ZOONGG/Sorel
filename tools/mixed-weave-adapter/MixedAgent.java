package dev.sorel.mixed;

import java.lang.instrument.Instrumentation;
import java.net.URL;
import java.net.URLClassLoader;
import java.nio.file.Path;

/** Experimental agent; the old loader is never on a runtime classpath. */
public final class MixedAgent {
    public static void premain(String currentLoader, Instrumentation instrumentation) throws Exception {
        URL self = MixedAgent.class.getProtectionDomain().getCodeSource().getLocation();
        var isolated = new URLClassLoader("SorelMixedAdapter",
            new URL[]{self, Path.of(currentLoader).toUri().toURL()}, ClassLoader.getPlatformClassLoader());
        isolated.loadClass("dev.sorel.mixed.MixedTransformer")
            .getMethod("install", Instrumentation.class, URL.class).invoke(null, instrumentation, self);
    }
}
