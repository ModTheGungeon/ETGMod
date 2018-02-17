return function(mod, game, env)
  env.Key = game.UnityEngine.KeyCode;

  function env.AddDebugHotkey(key, func)
    mod.DebugHotkeys:AddFunction(key, func)
  end
end
