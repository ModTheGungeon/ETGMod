using System;
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections;

public static partial class JSONHelper {

    public static class META {

        /// <summary>
        /// The metadata object marker. Use it inside metadata objects to specify which type of metadata it is.
        /// </summary>
        public const string MARKER = ".";
        /// <summary>
        /// The property "marker". Use it in normal objects as property name and the metadata object as value.
        /// </summary>
        public const string PROP = ":";
        /// <summary>
        /// The ValueType / struct "marker". Use it as value to PROP in value types.
        /// </summary>
        public const string VALUETYPE = "~";

        public const string REF                 = "ref";
        public const int REF_NONE               = -1;
        public const string REF_ID              = "#";
        public const string REF_TYPE            = "=";
        public const int REF_TYPE_EQUAL         = 0;
        public const int REF_TYPE_SAMEREF       = 1;

        public const string TYPE            = "type";
        public const string TYPE_FULLNAME   = "name";
        // public const string TYPE_SPLIT      = "split";
        public const string TYPE_GENPARAMS  = "params";

        public const string OBJTYPE         = "objtype";

        public const string ARRAYTYPE       = "arraytype";
        public const string ARRAYTYPE_ARRAY = "array";
        public const string ARRAYTYPE_LIST  = "list";
        public const string ARRAYTYPE_MAP   = "map";

        public const string ARRAYTYPE_ARRAY_SIZE = "size";

        public const string COMPONENTTYPE_DEFINITION    = "=";
        public const string COMPONENTTYPE_REFERENCE     = "~";

        public const string EXTERNAL                = "external";
        public const string EXTERNAL_PATH           = "path";
        public const string EXTERNAL_IN             = "in";
        public const string EXTERNAL_IN_RESOURCES   = "Resources.Load";
        public const string EXTERNAL_IN_RELATIVE    = "relative";
        public const string EXTERNAL_IN_SHARED      = "shared";

        public const string ARRAYAT         = "at";
        public const string ARRAYAT_INDEX   = "index";
        public const string ARRAYAT_VALUE   = "value";

        public const string UNSUPPORTED = "UNSUPPORTED";
        public const string UNSUPPORTED_USE_EXTERNAL = "REPLACE THIS WITH EXTERNAL";

    }

}
