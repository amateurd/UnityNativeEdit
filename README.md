## UnityNativeEdit v1.5
Unity Native Input Plugin for both iOS and Android (UGUI InputField compatible).

This means you don't need a separate 'Unity' Input box and you can use all native text functions such as `Select`, `Copy` and `Paste`.

### Usage
1. Simply copy the files in `release/NativeEditPlugin` into your existing unity project asset folder.
2. If using Unity 5.6.0 or 5.6.1, make sure that your Plugins/Android/AndroidManifest.xml defines 
    ```
    <activity android:name="com.bkmin.android.UnityPlayerNotOnTopActivity"
       android:label="@string/app_name">
    ```
    instead of
    ```
    <activity android:name="com.unity3d.player.UnityPlayerNativeActivity"
        android:label="@string/app_name">
    ```
    Note that there can be multiple Android manifests in a Unity project (if you have multiple Android plugins) and Unity merges them to a single manifest when building. The `activity` on the manifest closest to the root level of `Plugins/Android` directory seems to override definitions in other manifests so make sure to modify that manifest

    If another plugin you're using is overriding the `UnityPlayerActivity` and the input field appears invisible you need to modify the overriding `UnityPlayerActivity` so that it doesn't appear on top of native views, see https://github.com/YousicianGit/UnityNativeEdit/issues/34.
    
    You can refer to sample `AndroidManifest.xml` in `/Plugings/Android` folder.
 
3. Attach ```NativeEditBox``` script to your UnityUI ```InputField```object.
4. Build and run on your android or ios device!

### Etc
1. NativeEditBox will work with delegate defined in your Unity UI InputField, `On Value Change` and `End Edit`
2. It's open source and free to use/redistribute!
3. Please refer to `demo` Unity project.

- - -
## UnityNativeEdit v1.5 中文说明
UnityNativeEdit是适用于Unity 5版本、支持iOS和Android的原生输入框插件，免去直接使用UGUI的InputField产生的键盘方面的不便，并且可以和原生应用一样方便地对输入文本进行选择、复制和粘贴等操作。

本repo的1.5版本针对原版进行了各种优化和bug修复，无需像原版那样要事先挂载`PluginMsgHandler`脚本，并且已被某国产知名手机游戏采用。

Unity 2017版本尚未测试。

### 使用方法
1. 直接拷贝`release/NativeEditPlugin`目录下的文件到你的项目中；
2. 如果你使用的是Unity 5.6.0或者5.6.1版本，请确认你的项目的`Plugins/Android/AndroidManifest.xml`文件中：
    ```
    <activity android:name="com.unity3d.player.UnityPlayerNativeActivity"
        android:label="@string/app_name">
    ```
    改为：
    ```
    <activity android:name="com.bkmin.android.UnityPlayerNotOnTopActivity"
       android:label="@string/app_name">
    ```
    注：如果你使用多个Android插件的话，在一个项目中可能会有多个AndroidManifest.xml文件，Unity会在构建的时候将它们合并为一个文件。在文件结构中离`Plugins/Android`这一层最近的AndroidManifest中的`activity`定义看起来会覆盖其他AndroidManifest中的对应定义，所以请确保你修改的是正确的文件。可以参考demo`/Plugings/Android`文件夹中的`AndroidManifest.xml`。

    如果其他Android插件需要修改该`activity`定义同时修改后输入框不可用的话，参见https://github.com/YousicianGit/UnityNativeEdit/issues/34 。
3. 在你的InputField对象上添加`NativeEditBox`脚本组件。

    添加后，如果要通过代码修改输入框的文本的话，请务必通过`NativeEditBox`脚本的`text`属性进行操作，否则将不会看到修改后的文本。
4. 发布到真机上试试吧！

### Etc
1. 本插件可以响应同一GameObject上的InputField的`OnValueChanged`和`OnEndEdit`事件。
2. 本插件开源且可免费使用、分发。
