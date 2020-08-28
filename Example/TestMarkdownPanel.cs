using System.Collections.Generic;
using markdown;

using Unity.UIWidgets.animation;
using Unity.UIWidgets.engine;
using Unity.UIWidgets.foundation;
using Unity.UIWidgets.material;
using Unity.UIWidgets.painting;
using Unity.UIWidgets.ui;
using Unity.UIWidgets.widgets;
using UnityEngine;
using FontStyle = Unity.UIWidgets.ui.FontStyle;

namespace TestMarkdown
{
    namespace TestMarkdown
    {
        public class TestMarkdownPanel : UIWidgetsPanel
        {
            protected override void OnEnable()
            {
                // if you want to use your own font or font icons.   
                // FontManager.instance.addFont(Resources.Load<Font>(path: "path to your font"), "font family name");

                // load custom font with weight & style. The font weight & style corresponds to fontWeight, fontStyle of 
                // a TextStyle object
                // FontManager.instance.addFont(Resources.Load<Font>(path: "path to your font"), "Roboto", FontWeight.w500, 
                //    FontStyle.italic);

                // add material icons, familyName must be "Material Icons"
                // FontManager.instance.addFont(Resources.Load<Font>(path: "path to material icons"), "Material Icons");

                base.OnEnable();
            }

            private string markdownData1 = @"

#### When No usb cabble connecting to your android phone,but you need to check your app's log information. 

You can use [Android Console Server](https://github.com/suntabu/AndroidConsoleServer)


 
 
 
 ![](https://coding.net/u/suntabu/p/AndroidConsoleServer/git/raw/master/help.png)
 
 Using a NanoHTTPD Server on app side for serving the console command and request.
 
 - TODO:
    - list external app directory infos
    - clear external app directory files
    - format activity info string

HOW TO USE
-----

1. Start Server On Phone
    -    start code  
    
    ``` 
    // init with application context
    ACS.init(context);
    
    // start listen on port 8443
    ACS.startAndroidWebServer();
    ```
        
    -    Then you can use open console web page at your phone's ip:port with any browsers you like.

2. Start Server On Lan Device For Manager Working Devices  
	- Compile hostapp module to get app zip:
		-	Windows User  
		```
		gradlew assemblDist
		```  
		-	Linux/Mac User  
		```  
		./gradlew assemblDist  
		```  
	- Start HostApp  
		-	Linux/Mac User
		```
		./hostapp 
		```
		-	Windows User
		```
		./hostapp.bat
		```

3. Open the server address by browser
    ![](https://dn-coding-net-production-pp.qbox.me/ecf8a2dd-faf0-40c1-a5a7-4bba420b2e01.png)





TIPS: your phone's ip must be reachable！
===

LICENSE
-----
This is using the [NanoHTTPD](https://github.com/NanoHttpd/nanohttpd) library.
**AndroidConsoleServer** based on [Lopez Mikhael](http://mikhaellopez.com/)'s  [AndroidWebServer](https://github.com/lopspower/AndroidWebServer) and is licensed under a [Apache License 2.0](http://www.apache.org/licenses/LICENSE-2.0).  
";

            protected override Widget createWidget()
            {
                return new MaterialApp(
                    title: "Markdown Demo",
                    showPerformanceOverlay: false,
                    home: new Scaffold(
                        body: new Markdown(null, markdownData1,
                            syntaxHighlighter: new DartSyntaxHighlighter(SyntaxHighlighterStyle.lightThemeStyle()),
                            onTapLink:
                            url => { Application.OpenURL(url); })
                    )
                );
            }
        }
    }
}