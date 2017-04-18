using NUnit.Framework;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

// Setting ComVisible to false makes the types in this assembly not visible 
// to COM components.  If you need to access a type in this assembly from 
// COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible(false)]

// The following GUID is for the ID of the typelib if this project is exposed to COM
[assembly: Guid("39e5e0c8-c1d3-4583-b9f7-8fbd695e5601")]


// LUCENENET specific - only allow tests in this assembly to run one at a time
// to prevent polluting shared state.
[assembly: LevelOfParallelism(1)]