using System;
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

public abstract class JSONConfigMap<T> : JSONConfig<T> {

    public abstract Dictionary<MemberInfo, bool> Map {
        get;
    }

    public virtual bool Default {
        get {
            return true;
        }
    }

    private Dictionary<MemberInfo, bool> map;
    public override bool CanSerialize(object obj, MemberInfo info, bool isPrivate) {
        if (map == null) {
            map = Map;
        }

        bool serialize;
        if ((!map.TryGetValue(info, out serialize) && Default) || serialize) {
            return base.CanSerialize(obj, info, isPrivate);
        }
        return false;
    }

}
