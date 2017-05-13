using System;
using YamlDotNet.Serialization;

namespace TexMod {
    public class TexModExtra {
        [YamlMember(Alias = "dir")]
        public string Dir { get; set; } = "texmod";

        [YamlMember(Alias = "animations")]
        public string Animations { get; set; } = "animations";

        [YamlMember(Alias = "collections")]
        public string Collections { get; set; } = "collections";
    }
}
