using System;
using YamlDotNet.Serialization;

namespace TexMod {
    public class TexModExtra {
        [YamlMember(Alias = "dir")]
        public string Dir { get; set; }
    }
}
