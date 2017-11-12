local sep = package.config:sub(1,1)
local function searchpath (mod,path)
    mod = mod:gsub('%.',sep)
    for m in path:gmatch('[^;]+') do
        local nm = m:gsub('?',mod)
        local f = io.open(nm,'r')
        if f then f:close(); return nm end
    end
end
-- https://github.com/stevedonovan/Penlight/blob/master/lua/pl/compat.lua
-- penlight is licensed under the MIT license
-- https://github.com/stevedonovan/Penlight/blob/master/LICENSE.md

return function(env)
  local function include(name)
    env[name] = _G[name]
  end

  env._G = env

  include "assert"
  include "error"
  include "pairs"
  include "ipairs"
  include "next"
  include "pcall"

  env.print = function(...)
    local args = {...}
    local len = select('#', ...)
    local s = ""

    for x = 1, len do
      s = s .. tostring(args[x])
      if x ~= len then
        s = s .. "\t"
      end
    end

    env.Logger:Info(s)
  end

  include "select"
  include "tonumber"
  include "tostring"
  include "type"
  include "unpack"
  include "_VERSION"
  env._ETGMOD = true
  include "xpcall"

  env.coroutine = {
    create = coroutine.create,
    resume = coroutine.resume,
    running = coroutine.running,
    status = coroutine.status,
    wrap = coroutine.wrap,
    yield = coroutine.yield
  }

  env.string = {
    byte = string.byte,
    char = string.char,
    dump = string.dump,
    find = string.find,
    format = string.format,
    gmatch = string.gmatch,
    gsub = string.gsub,
    len = string.len,
    lower = string.lower,
    match = string.match,
    rep = string.rep,
    reverse = string.reverse,
    sub = string.sub,
    upper = string.upper
  }

  env.table = {
    insert = table.insert,
    maxn = table.maxn,
    remove = table.remove,
    sort = table.sort
  }

  local system = clr.namespace(clr.assembly('mscorlib'), 'System')
  local rng = system.Random()

  env.math = {
    abs = math.abs,
    acos = math.acos,
    asin = math.asin,
    atan = math.atan,
    atan2 = math.atan2,
    ceil = math.ceil,
    cos = math.cos,
    cosh = math.cosh,
    deg = math.deg,
    exp = math.exp,
    floor = math.floor,
    fmod = math.fmod,
    frexp = math.frexp,
    huge = math.huge,
    ldexp = math.ldexp,
    log = math.log,
    log10 = math.log10,
    max = math.max,
    min = math.min,
    modf = math.modf,
    pi = math.pi,
    pow = math.pow,
    rad = math.rad,
    sin = math.sin,
    sinh = math.sinh,
    sqrt = math.sqrt,
    tan = math.tan,
    tanh = math.tanh,

    random = function(a, b)
      if a == nil and b == nil then
        return rng:NextDouble()
      elseif b == nil then
        return rng:Next(1, a + 1)
      else
        return rng:Next(a, b + 1)
      end
    end,
    randomseed = function(seed)
      rng = system.Random(seed)
    end
  }

  env.os = {
    clock = os.clock,
    date = os.date,
    difftime = os.difftime,
    time = os.time,
  }

  local _require = require
  

  function env.require(name)
  --   local formatted_name = name:gsub("[%./\\]+", "/") -- replace all series of dots or slashes with a single slash
  --   if #formatted_name > 0 then
  --     if formatted_name[1] == "/" then
  --       formatted_name = formatted_name:sub(2)
  --     end
  --     if formatted_name[#formatted_name] == "/" then
  --       formatted_name = formatted_name:sub(1, #formatted_name - 1)
  --     end
  --   end
    local path, err = searchpath(name, env.package.path)
    if err then error(err) end

    if env.package.loaded[path] then return env.package.loaded[path] end

    local f = loadfile(path, "t", env)

    env.package.loaded[path] = f()
    return env.package.loaded[path]
  end
end
