-- this file is loaded by ETGMod
-- and it's used to setup a base environment for mods
-- editing this file has a high chance of breaking mods

-- There's a Mod global available in here
-- with the current mod's ModInfo

local env = {
  Events = {}
}

luanet.load_assembly("Assembly-CSharp")
luanet.load_assembly("UnityEngine")


local ns_etgmod = luanet.namespace {'ETGMod'}
local function namespace(name, tab)
  return setmetatable(tab, {
    __index = luanet.namespace {name}
  })
end

local _GAME = namespace("", {
  ETGMod = namespace("ETGMod", {
    GUI = luanet.namespace {'ETGMod.GUI'},
    Lua = luanet.namespace {'ETGMod.Lua'},
    Console = namespace("ETGMod.Console", {
      Parser = luanet.namespace {'ETGMod.Console'}
    }),
    TexMod = luanet.namespace {'ETGMod.TexMod'}
  })
})
env._GAME = _GAME

for k, v in pairs(luanet.namespace {'ETGMod.Lua'}) do
  env[k] = v
end

local lua = luanet.namespace {'ETGMod.Lua'}
env = setmetatable(env, {
  __index = function(self, k)
    if k == "PrimaryPlayer" then
      return lua.Globals.PrimaryPlayer
    elseif k == "SecondaryPlayer" then
      return lua.Globals.SecondaryPlayer
    end
  end
})

local gui = luanet.namespace {'ETGMod.GUI'}
local etgmod = luanet.namespace {'ETGMod'}

function env.Color(r, g, b, a)
  if a == nil then
    return etgmod.UnityUtil.NewColorRGB(r, g, b)
  else
    return etgmod.UnityUtil.NewColorRGBA(r, g, b, a)
  end
end

function env.Notify(data)
  if type(data) ~= "table" then
    error("Notification data must be a table")
  end

  if data.title == nil or data.content == nil then
    error("Notification must have a title and content")
  end
  local notif = gui.Notification(data.title, data.content)

  if data.image then
    notif.Image = data.image
  end
  if data.background_color then
    notif.BackgroundColor = data.background_color
  end
  if data.title_color then
    notif.TitleColor = data.title_color
  end
  if data.content_color then
    notif.ContentColor = data.content_color
  end

  gui.GUI.NotificationController:Notify(notif)
end

require("sandbox")(env)

return env;
