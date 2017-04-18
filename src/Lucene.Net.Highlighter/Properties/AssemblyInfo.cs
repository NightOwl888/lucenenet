using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

[assembly: CLSCompliant(true)]

// Setting ComVisible to false makes the types in this assembly not visible 
// to COM components.  If you need to access a type in this assembly from 
// COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible(false)]

// The following GUID is for the ID of the typelib if this project is exposed to COM
[assembly: Guid("e9e769ea-8504-44bc-8dc9-ccf958765f8f")]

[assembly: InternalsVisibleTo("Lucene.Net.Icu")]
// for testing
[assembly: InternalsVisibleTo("Lucene.Net.Tests.Highlighter")]
[assembly: InternalsVisibleTo("Lucene.Net.Tests.Icu")]
