return function(mod, game, env)
  function env.TestSpawnItem(id)
    game.ETGMod.API.CustomPickupObject.Test(id)
  end

  function env.CreateItem(name, id)
    assert(name)
    assert(id)

    local item = mod.ItemsDB:CreatePassive(mod, id)
    item.Name = name
    return item
  end
end
