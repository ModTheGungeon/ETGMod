-- this file is loaded by ETGMod
-- and it's used to setup a base environment for mods
-- editing this file has a high chance of breaking mods

print("A" .. tostring(unpack))

_ENV = { -- new environment for each mod
  import = import,
  luanet = luanet,
  unpack = _G.unpack,
  print = print,
  tostring = tostring
}
	
_ENV.Events = {}

print("B" .. tostring(unpack))

import 'Assembly-CSharp'
import 'ETGMod.Lua'
import 'ETGMod.Globals'
import 'ETGMod'

LuaTool = nil

local globals = luanet.namespace {"ETGMod.Globals"}

PrimaryPlayer = globals.PrimaryPlayer
SecondaryPlayer = globals.SecondaryPlayer

--[[
Loader.Logger:Info(getmetatable(_G).__index)

local g_mt = getmetatable(_G)
local old_g_mt_index = g_mt.__index
g_mt.__index = function(self, k)
    if k == "PrimaryPlayer" then
      return Globals.PrimaryPlayer
    elseif k == "SecondaryPlayer" then
      return Globals.SecondaryPlayer
    end
    return g_mt.__index(self, k)
end

Loader.Logger:Info("Hello")
]]
