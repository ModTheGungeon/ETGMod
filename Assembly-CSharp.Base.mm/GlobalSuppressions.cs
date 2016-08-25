
// This file is used by Code Analysis to maintain SuppressMessage 
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given 
// a specific target and scoped to a namespace, type, member, etc.

[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Potential Code Quality Issues", "RECS0026:Possible unassigned object created by 'new'", Justification = "SGUI abuses this behaviour to add new children to the main root.")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Potential Code Quality Issues", "RECS0021:Warns about calls to virtual member functions occuring in the constructor", Justification = "Desired behaviour. Also, not always calls, but sometimes delegate +/-/= operations.")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Language Usage Opportunities", "RECS0091:Use 'var' keyword when possible", Justification = "This is not JavaScript.")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Redundancies in Symbol Declarations", "RECS0154:Parameter is never used", Justification = "When working with delegates, this is bound to happen.")]

