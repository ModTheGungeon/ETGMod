-- this file is loaded by ETGMod
-- and it's used to setup a base environment for mods
-- editing this file has a high chance of breaking mods

-- There's a Mod global available in here
-- with the current mod's ModInfo

local env = {
  Events = {}
}

luanet.load_assembly("Assembly-CSharp")

for k, v in pairs(luanet.namespace {'ETGMod.Lua'}) do
  env[k] = v
end

local globals = luanet.namespace {'ETGMod.Globals'}
env = setmetatable(env, {
  __index = function(self, k)
    if k == "PrimaryPlayer" then
      return globals.PrimaryPlayer
    elseif k == "SecondaryPlayer" then
      return globals.SecondaryPlayer
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
