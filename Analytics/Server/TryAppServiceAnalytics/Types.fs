module TryAppServiceAnalytics.Types

open System
open System.Runtime.Serialization

type TimeRange = (DateTime * DateTime)

type AggregatePeriod =
    | Day
    | Week
    | Month