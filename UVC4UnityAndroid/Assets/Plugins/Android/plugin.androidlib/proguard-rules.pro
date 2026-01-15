# Add project specific ProGuard rules here.
# You can control the set of applied configuration files using the
# proguardFiles setting in build.gradle.
#
# For more details, see
#   http://developer.android.com/guide/developing/tools/proguard.html

# If your project uses WebView with JS, uncomment the following
# and specify the fully qualified class name to the JavaScript interface
# class:
#-keepclassmembers class fqcn.of.javascript.interface.for.webview {
#   public *;
#}

# Uncomment this to preserve the line number information for
# debugging stack traces.
#-keepattributes SourceFile,LineNumberTable

# If you keep the line number information, uncomment this to
# hide the original source file name.
#-renamesourcefileattribute SourceFile

# aandusb::usb
-keep public class com.serenegiant.usb.DeviceDetector {
    native <methods>;
	public *;
}

-keep public class com.serenegiant.usb.DeviceDetectorFragment {
    native <methods>;
	public *;
}

# unity
-keep public class com.serenegiant.unity.utils.AndroidUtils {
    *;
}

# uvcplugin
-keep public class com.serenegiant.unity.uvcplugin.UsbPermissionActivity {
    *;
}
