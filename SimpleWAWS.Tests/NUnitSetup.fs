module NUnitSetup

open NUnit.Framework
open Serilog

[<SetUpFixture>]
type NUnitSetup() =

    [<SetUp>]
    member this.Setup() =
        let logger = (new LoggerConfiguration()).WriteTo.ColoredConsole().CreateLogger()
        SimpleWAWS.Trace.SimpleTrace.Analytics <- logger
        SimpleWAWS.Trace.SimpleTrace.Diagnostics <- logger