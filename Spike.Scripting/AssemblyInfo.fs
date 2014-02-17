﻿namespace Spike.Scripting


open System.Reflection
open System.Runtime.CompilerServices
open System.Runtime.InteropServices

module Version =
  let [<Literal>] Major = 0
  let [<Literal>] Minor = 2
  let [<Literal>] Build = 1
  let [<Literal>] Revision = 0
  let [<Literal>] String = "0.2.1.0"
  let Tupled = Major, Minor, Build, Revision
  let FullName = sprintf "Spike.Scripting %s" String
 
[<assembly: AssemblyTitle("Spike.Scripting")>]
[<assembly: AssemblyDescription("Spike.Scripting - A JavaScript runtime for .NET")>]
[<assembly: AssemblyConfiguration("")>]
[<assembly: AssemblyCompany("Spike.Scripting")>]
[<assembly: AssemblyProduct("Spike.Scripting")>]
[<assembly: AssemblyCopyright("Copyright © Misakai Ltd. 2013")>]
[<assembly: AssemblyTrademark("")>]
[<assembly: AssemblyCulture("")>]
 
[<assembly: ComVisible(false)>]
[<assembly: Guid("93e32fd4-5c93-4d0b-91d5-0b4e54f0ce2d")>]

[<assembly: AssemblyVersion(Version.String)>]
[<assembly: AssemblyFileVersion(Version.String)>]

()