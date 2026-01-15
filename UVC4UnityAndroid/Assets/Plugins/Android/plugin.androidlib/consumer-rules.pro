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
