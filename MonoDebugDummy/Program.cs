using System;

namespace MonoDebugDummy {
    class MainClass {
        public static void Main(string[] args) {
            Console.WriteLine("You shouldn't see this.");
        }
    }
}

/*

MonoDebug instructions:

* Set environment variable MONODEVELOP_SDB_TEST=1
* Hit run button in MonoDevelop
* Set path to Gungeon executable path
* Set arguments to --debugger-client --no-steam
* Set IP to 127.0.0.1
* Set port to 10000
* Hit Listen

*/
