Main = {}

local function Start()
    gflog.log("TEST", gftime.getMillisecondsSinceStartup())
    --[#if UNITY_EDIT2OR || (CLIENT_DEBUG || UNITY_EDITOR)
    gflog.log("TEST", "nsaer")
    --]#endif
    
    --[#if GF_DEBUG && !UNITY_EDITOR
    gflog.error("TEST", "432qg")
    --]#endif

    --[#if GF_DEBUG && UNITY_EDITOR
    gflog.warning("TEST", "asdfe")
    --]#endif
    gflog.log("TEST", gftime.getMillisecondsSinceStartup())
end 

Main.Start = Start
return Main