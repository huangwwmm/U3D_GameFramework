Main = {}

local function Start()
    gflog.log("TEST", gftime.getMillisecondsSinceStartup())
    --[#if UNITY_EDIT2OR || (CLIENT_DEBUG || UNITY_EDITOR)
    gflog.log("a", "nsaer")
    --]#endif
    
    --[#if gf_DEBUG && !UNITY_EDITOR
    gflog.error("c3wf", "432qg")
    --]#endif

    --[#if gf_DEBUG && UNITY_EDITOR
    gflog.warning("f23f", "asdfe")
    --]#endif
    gflog.log("TEST", gftime.getMillisecondsSinceStartup())
end

Main.Start = Start
return Main